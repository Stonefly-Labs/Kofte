import { isIP } from "node:net";

export function backendReadinessUrl(
  environment: Readonly<Record<string, string | undefined>> = process.env,
): URL {
  const hostOrigin = environment.KOFTE_HOST_URL ?? "http://127.0.0.1:5080";
  if (
    !/^http:\/\/[^/?#@\s]+\/?$/.test(hostOrigin) ||
    hostOrigin.trim() !== hostOrigin
  )
    throw new TypeError("backend_configuration_invalid");
  const host = new URL(hostOrigin);
  const hostName = host.hostname.replace(/^\[|\]$/g, "");
  const unsafe = environment.KOFTE_ALLOW_UNSAFE_EXPOSURE ?? "false";
  if (
    !["true", "false"].includes(unsafe) ||
    host.port === "0" ||
    (!isLoopback(hostName) && unsafe !== "true")
  )
    throw new TypeError("backend_configuration_invalid");
  const origin =
    environment.KOFTE_BACKEND_ORIGIN ??
    (isLoopback(hostName)
      ? host.origin
      : `http://127.0.0.1:${host.port || "80"}`);
  if (!/^http:\/\/[^/?#@\s]+\/?$/.test(origin) || origin.trim() !== origin)
    throw new TypeError("backend_configuration_invalid");
  const url = new URL(origin);
  const hostname = url.hostname.replace(/^\[|\]$/g, "");
  if (
    url.username ||
    url.password ||
    !isLoopback(hostname) ||
    url.port === "0" ||
    url.port !== host.port ||
    (isLoopback(hostName) &&
      hostname !== hostName &&
      !(hostName === "localhost" && ["127.0.0.1", "::1"].includes(hostname))) ||
    (hostName === "0.0.0.0" && isIP(hostname) !== 4)
  )
    throw new TypeError("backend_configuration_invalid");
  return new URL("/health/ready", url);
}

export function isLoopback(hostname: string): boolean {
  return (
    hostname === "localhost" ||
    hostname === "::1" ||
    (isIP(hostname) === 4 && hostname.startsWith("127."))
  );
}
