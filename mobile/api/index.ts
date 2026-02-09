/**
 * API Services
 * Pure Axios + Zod implementation
 */

// Core
export { apiClient, ApiError } from './client';
export type { ApiResponse } from './types';

// Auth
export { AuthService } from './auth/service';
export type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  Verify2FARequest,
  TokenResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from './auth/schemas';
