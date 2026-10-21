import io
from pathlib import Path
import socket
import subprocess
import tempfile
import time
import unittest
from unittest.mock import Mock, patch

from scripts import dev


class DevTests(unittest.TestCase):
    def test_adapter_default_follows_the_selected_host_port(self):
        for hostname in ("127.0.0.1", "0.0.0.0"):
            with self.subTest(hostname=hostname):
                config = dev.configuration({
                    "KOFTE_HOST_URL": f"http://{hostname}:5081",
                    "KOFTE_ALLOW_UNSAFE_EXPOSURE": "true",
                })
                self.assertEqual("http://127.0.0.1:5081", config.backend)

    def test_dotenv_is_data_and_explicit_environment_wins(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / ".env"
            path.write_text("KOFTE_DATA_ROOT='$(touch sentinel)'\nKOFTE_PROFILE=local-sqlite\n")
            values = dev.load_environment(path, {"KOFTE_DATA_ROOT": "/owned"})
            self.assertEqual("/owned", values["KOFTE_DATA_ROOT"])
            self.assertFalse((Path(directory) / "sentinel").exists())
            self.assertEqual("local-sqlite", values["KOFTE_PROFILE"])
            path.write_text("export KOFTE_PROFILE=local-sqlite\n")
            with self.assertRaises(dev.DevError):
                dev.load_environment(path, {})

    def test_configuration_rejects_unknown_credentials_and_unsafe_destinations(self):
        for env in (
            {"KOFTE_UNKNOWN": "canary"},
            {"KOFTE_PROFILE": "test-postgres"},
            {"KOFTE_HOST_URL": "http://user:canary@127.0.0.1:5080"},
            {"KOFTE_BACKEND_ORIGIN": "http://example.com"},
            {"KOFTE_BACKEND_ORIGIN": "http://127.0.0.1:5081"},
            {"KOFTE_BACKEND_ORIGIN": "http://127.0.0.2:5080"},
            {"KOFTE_HOST_URL": "http://0.0.0.0:5080"},
            {"KOFTE_HOST_URL": "http://@127.0.0.1:5080"},
            {"KOFTE_HOST_URL": "http://127.0.0.1:5080\n"},
            {"KOFTE_DATA_ROOT": "../data"},
            {"KOFTE_ALLOW_UNSAFE_EXPOSURE": "yes"},
            {"KOFTE_WEB_URL": "http://127.0.0.1:5080"},
        ):
            with self.subTest(keys=tuple(env)):
                with self.assertRaises(dev.DevError):
                    values = dev.load_environment(Path("/nonexistent/kofte-env"), env)
                    dev.configuration(values)

    def test_occupied_port_is_not_taken_over(self):
        with socket.socket() as occupied:
            occupied.bind(("127.0.0.1", 0))
            occupied.listen()
            port = occupied.getsockname()[1]
            config = dev.configuration({"KOFTE_HOST_URL": f"http://127.0.0.1:{port}"})
            with self.assertRaises(dev.DevError) as caught:
                dev.check_ports(config)
            self.assertEqual("port_unavailable", caught.exception.code)
            self.assertEqual(port, occupied.getsockname()[1])

    def test_a_recently_closed_connection_does_not_block_restart(self):
        with socket.socket() as listener, socket.socket() as client, socket.socket() as web:
            listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            listener.bind(("127.0.0.1", 0))
            listener.listen()
            port = listener.getsockname()[1]
            client.settimeout(1)
            client.connect(("127.0.0.1", port))
            peer, _ = listener.accept()
            with peer:
                peer.shutdown(socket.SHUT_WR)
                self.assertEqual(b"", client.recv(1))
            client.close()
            listener.close()
            web.bind(("127.0.0.1", 0))
            web_port = web.getsockname()[1]
            web.close()
            config = dev.configuration({
                "KOFTE_HOST_URL": f"http://127.0.0.1:{port}",
                "KOFTE_WEB_URL": f"http://127.0.0.1:{web_port}",
            })
            dev.check_ports(config)

    @patch.object(dev.platform, "system", return_value="Darwin")
    @patch.object(dev.platform, "machine", return_value="arm64")
    @patch.object(dev.platform, "mac_ver", return_value=("15.0", (), ""))
    @patch.object(dev.shutil, "which", return_value="/test/tool")
    @patch.object(dev.subprocess, "run")
    def test_preflight_checks_every_exact_tool_version(self, run, *_):
        run.side_effect = [
            subprocess.CompletedProcess([], 0, version.encode(), b"")
            for version in dev.VERSIONS.values()
        ]
        dev.check_tools({}, time.monotonic() + 10)
        self.assertEqual(3, run.call_count)
        run.side_effect = None
        run.return_value = subprocess.CompletedProcess([], 0, b"wrong-canary", b"")
        with self.assertRaises(dev.DevError) as caught:
            dev.check_tools({}, time.monotonic() + 10)
        self.assertNotIn("canary", str(caught.exception))

    def test_build_failure_is_explicit_and_does_not_expose_child_output(self):
        with patch.object(dev, "Children") as children, patch.object(dev, "emit"):
            children.return_value.start.return_value.wait.return_value = 1
            with self.assertRaises(dev.DevError) as caught:
                dev.run_step(["tool"], "host", {}, time.monotonic() + 10)
            children.return_value.stop.assert_called_once()
        self.assertEqual("build_failed", caught.exception.code)
        self.assertNotIn("canary", str(caught.exception))

    def test_child_exit_and_deadline_fail_by_component(self):
        child = Mock()
        child.poll.return_value = 7
        children = dev.Children()
        children.processes = [(child, "host")]
        with self.assertRaises(dev.DevError) as caught:
            children.check()
        self.assertEqual("host", caught.exception.component)
        with self.assertRaises(dev.DevError) as caught:
            dev.wait_ready("http://127.0.0.1", dev.Children(), 0, "web")
        self.assertEqual("startup_timeout", caught.exception.code)

    def test_cleanup_targets_only_owned_process_groups(self):
        child = Mock()
        child.pid = 123456789
        child.returncode = None
        child.poll.return_value = None
        child.stdout, child.stderr = io.BytesIO(), io.BytesIO()
        children = dev.Children()
        children.processes = [(child, "host")]
        with patch.object(dev.os, "killpg") as kill:
            children.stop()
        self.assertTrue(all(call.args[0] == child.pid for call in kill.call_args_list))
        self.assertTrue(all(0 < call.kwargs["timeout"] <= 5 for call in child.wait.call_args_list))

    def test_partial_launch_is_cleaned_when_second_child_fails(self):
        config = dev.configuration({})
        with patch.object(dev, "check_tools"), patch.object(dev, "check_ports"), \
                patch.object(dev, "run_step"), patch.object(dev, "wait_ready"), \
                patch.object(dev, "Children") as children:
            children.return_value.start.side_effect = [None, OSError("credential-canary")]
            with self.assertRaises(OSError):
                dev.supervise(config)
            children.return_value.stop.assert_called_once()

    def test_interrupt_during_startup_stops_owned_children(self):
        config = dev.configuration({})
        with patch.object(dev, "check_tools"), patch.object(dev, "check_ports"), \
                patch.object(dev, "run_step"), patch.object(dev, "wait_ready", side_effect=KeyboardInterrupt), \
                patch.object(dev, "Children") as children:
            with self.assertRaises(KeyboardInterrupt):
                dev.supervise(config)
            children.return_value.stop.assert_called_once()


if __name__ == "__main__":
    unittest.main()
