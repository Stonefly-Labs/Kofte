import { request } from "node:http";

export function requestWithBody(url: URL, body: string) {
  return new Promise<{
    status: number | undefined;
    body: string;
    cacheControl: string | undefined;
  }>((resolve, reject) => {
    const outgoing = request(
      url,
      {
        method: "GET",
        headers: { "Content-Length": Buffer.byteLength(body) },
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
