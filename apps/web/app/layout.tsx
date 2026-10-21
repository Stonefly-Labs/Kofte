import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Köfte · Your platform, your ingredients",
  description:
    "A composable, self-hosted developer platform. Local bootstrap preview.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
