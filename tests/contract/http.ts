import { request as httpRequest } from "node:http";
import { request as httpsRequest } from "node:https";

export function requestWithBody(
  url: URL,
  body: string,
  headers: Record<string, string> = {},
) {
  return new Promise<{
    status: number | undefined;
    body: string;
    cacheControl: string | undefined;
  }>((resolve, reject) => {
    const request = url.protocol === "https:" ? httpsRequest : httpRequest;
    const outgoing = request(
      url,
      {
        method: "GET",
        headers: { ...headers, "Content-Length": Buffer.byteLength(body) },
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
