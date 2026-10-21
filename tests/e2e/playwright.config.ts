import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: ".",
  timeout: 60000,
  workers: 1,
  forbidOnly: true,
  retries: 0,
  reporter: [["list"]],
  use: {
    baseURL: process.env.KOFTE_WEB_URL ?? "http://127.0.0.1:3000",
    headless: true,
  },
});
