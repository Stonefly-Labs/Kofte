import { request as httpRequest, type IncomingHttpHeaders } from "node:http";
import { request as httpsRequest } from "node:https";

export function requestWithHeaderBytes(url: URL, bytes: number) {
  const headers = {
    Host: url.host,
    "X-Forwarded-Host": "boundary.invalid",
    "X-Forwarded-Port": "443",
    "X-Forwarded-Proto": "https",
    "X-Forwarded-For": "127.0.0.1",
    "X-Identity-Padding": "",
  };
  const fixedBytes = Object.entries(headers).reduce(
    (total, [name, value]) =>
      total + Buffer.byteLength(`${name}: ${value}\r\n`),
    0,
  );
  return requestWithBody(url, "", {
    ...headers,
    "X-Identity-Padding": "a".repeat(bytes - fixedBytes),
  });
}

export function requestWithBody(
  url: URL,
  body: string,
  headers: Record<string, string | string[]> = {},
  method = "GET",
) {
  return new Promise<{
    status: number | undefined;
    body: string;
    cacheControl: string | undefined;
    headers: IncomingHttpHeaders;
  }>((resolve, reject) => {
    const request = url.protocol === "https:" ? httpsRequest : httpRequest;
    const outgoing = request(
      url,
      {
        method,
        setDefaultHeaders: false,
        headers: {
          Host: url.host,
          ...headers,
          ...(body.length ? { "Content-Length": Buffer.byteLength(body) } : {}),
        },
      },
      (response) => {
        let text = "";
        response.setEncoding("utf8");
        response.on("data", (chunk: string) => {
          text += chunk;
        });
        response.on("end", () =>
          resolve({
            status: response.statusCode,
            body: text,
            cacheControl: response.headers["cache-control"],
            headers: response.headers,
          }),
        );
        response.on("error", reject);
      },
    );
    outgoing.setTimeout(6_000, () =>
      outgoing.destroy(new Error("contract_http_deadline")),
    );
    outgoing.on("error", reject);
    outgoing.end(body);
  });
}
