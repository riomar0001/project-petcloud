import { defineConfig } from "@hey-api/openapi-ts";

export default defineConfig({
  input: {
    path: "./api/openapi.json",
    watch: false,
  },
  output: "api/client",
  plugins: [
    {
      name: "@hey-api/client-axios",
      // Note: Ensure this path is relative to the OUTPUT directory
      // or an absolute path to your config file
      runtimeConfigPath: "../client-config",
    },
    "@hey-api/client-axios",
    {
      name: "@hey-api/sdk",
      validator: "zod",
      operations: { strategy: "byTags" },
    },
  ],
});
