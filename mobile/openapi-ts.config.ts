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
      runtimeConfigPath: "../client-config",
    },
    {
      name: "@hey-api/sdk",
      validator: "zod",
      asClass: true, // Use class-based SDK
    },
  ],
});
