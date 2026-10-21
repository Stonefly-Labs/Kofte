import type { NextConfig } from "next";

const config: NextConfig = {
  poweredByHeader: false,
  transpilePackages: ["@kofte/bootstrap-contract"],
};

export default config;
