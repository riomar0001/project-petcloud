import type { CreateClientConfig } from './client/client.gen';

/**
 * Runtime configuration for the Hey API client.
 * This function is called on client initialization and configures the client
 * to use our custom axios instance with interceptors for auth and token refresh.
 *
 * @see https://heyapi.dev/openapi-ts/clients/axios#custom-instance
 */
export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  // Use our custom axios instance with auth interceptors
});