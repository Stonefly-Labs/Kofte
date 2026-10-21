import contextlib
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch
import uuid

from scripts import dev


class SupervisorOutputTests(unittest.TestCase):
    def assert_safe_events(self, output, forbidden):
        for value in forbidden:
            self.assertNotIn(value, output)
        events = [json.loads(line) for line in output.splitlines()]
        self.assertTrue(events)
        for event in events:
            self.assertEqual({"eventCode", "component", "correlationId"}, set(event))
            self.assertRegex(event["correlationId"], r"^[0-9a-f]{32}$")
        return events

    def test_actual_main_configuration_failure_never_echoes_credentials_or_root(self):
        canary = "credential-canary-" + uuid.uuid4().hex
        output = io.StringIO()
        with tempfile.TemporaryDirectory() as directory, \
                patch.object(dev, "ROOT", Path(directory)), \
                patch.dict(os.environ, {
                    "KOFTE_HOST_URL": f"http://operator:{canary}@127.0.0.1:5080",
                    "KOFTE_DATA_ROOT": directory,
                }, clear=True), patch.object(sys, "argv", ["dev.py"]), \
                contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = dev.main()
        self.assertEqual(1, code)
        events = self.assert_safe_events(output.getvalue(), (canary, directory, "Traceback"))
        self.assertEqual("configuration_invalid", events[-1]["eventCode"])

    def test_real_failed_child_streams_are_drained_but_never_emitted(self):
        canary = "credential-canary-" + uuid.uuid4().hex
        environment = {**os.environ, "BOOTSTRAP_PRIVACY_CANARY": canary}
        script = (
            "import os,sys; value=os.environ['BOOTSTRAP_PRIVACY_CANARY'];"
            "sys.stdout.write('Password='+value+'\\n');"
            "sys.stderr.write('System.IO.IOException: '+value+"
            "' at /private/private-layout/host.cs:42\\n');sys.exit(7)"
        )
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            with self.assertRaises(dev.DevError) as caught:
                dev.run_step([sys.executable, "-c", script], "host", environment,
                             time.monotonic() + 10)
            dev.emit(caught.exception.code, caught.exception.component, error=True)
        events = self.assert_safe_events(output.getvalue(), (
            canary, "Password=", "System.IO.", "/private/private-layout/", "Traceback",
        ))
        self.assertEqual("build_failed", events[-1]["eventCode"])
        self.assertEqual("host", events[-1]["component"])


if __name__ == "__main__":
    unittest.main()
