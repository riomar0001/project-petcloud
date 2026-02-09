import { z } from 'zod';
import { apiClient } from '../client';
import {  handleValidationError, ApiResponse } from '../types';
import {
  loginSchema,
  loginResponseSchema,
  registerSchema,
  verify2FASchema,
  tokenResponseSchema,
  forgotPasswordSchema,
  resetPasswordSchema,
  type LoginRequest,
  type LoginResponse,
  type RegisterRequest,
  type Verify2FARequest,
  type TokenResponse,
  type ForgotPasswordRequest,
  type ResetPasswordRequest,
} from './schemas';

export class AuthService {
  /**
   * Login with email and password
   */
  static async login(data: LoginRequest): Promise<LoginResponse> {
    try {
      // Validate input
      const validated = loginSchema.parse(data);

      // Make API call
      const response = await apiClient.post<ApiResponse<LoginResponse>>(
        '/api/v1/auth/login',
        validated
      );

      // Validate and unwrap response
      const validatedResponse = loginResponseSchema.parse(response.data.data);
      return validatedResponse;
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Register new user
   */
  static async register(data: RegisterRequest): Promise<LoginResponse> {
    try {
      const validated = registerSchema.parse(data);

      const response = await apiClient.post<ApiResponse<LoginResponse>>(
        '/api/v1/auth/register',
        validated
      );

      const validatedResponse = loginResponseSchema.parse(response.data.data);
      return validatedResponse;
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Verify 2FA code
   */
  static async verify2FA(data: Verify2FARequest): Promise<TokenResponse> {
    try {
      const validated = verify2FASchema.parse(data);

      const response = await apiClient.post<ApiResponse<TokenResponse>>(
        '/api/v1/auth/verify-2fa',
        validated
      );

      const validatedResponse = tokenResponseSchema.parse(response.data.data);
      return validatedResponse;
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Request password reset
   */
  static async forgotPassword(data: ForgotPasswordRequest): Promise<void> {
    try {
      const validated = forgotPasswordSchema.parse(data);

      await apiClient.post('/api/v1/auth/forgot-password', validated);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Reset password with token
   */
  static async resetPassword(data: ResetPasswordRequest): Promise<void> {
    try {
      const validated = resetPasswordSchema.parse(data);

      await apiClient.post('/api/v1/auth/reset-password', validated);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Refresh access token
   */
  static async refreshToken(refreshToken: string): Promise<TokenResponse> {
    try {
      const response = await apiClient.post<ApiResponse<TokenResponse>>(
        '/api/v1/auth/refresh',
        { refreshToken }
      );

      const validatedResponse = tokenResponseSchema.parse(response.data.data);
      return validatedResponse;
    } catch (error) {
      throw error;
    }
  }

  /**
   * Logout
   */
  static async logout(): Promise<void> {
    try {
      await apiClient.post('/api/v1/auth/logout');
    } catch (error) {
      // Ignore logout errors
      console.warn('Logout failed:', error);
    }
  }
}

// Re-export types
export type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  Verify2FARequest,
  TokenResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
};
