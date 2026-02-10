import { z } from 'zod';
import { apiClient } from '../client';
import { handleValidationError, ApiResponse } from '../types';
import {
  profileResponseSchema,
  updateProfileSchema,
  changePasswordSchema,
  updatePhotoResponseSchema,
  type ProfileResponse,
  type UpdateProfileRequest,
  type ChangePasswordRequest,
  type UpdatePhotoResponse,
} from './schemas';

export class ProfileService {
  static async getProfile(): Promise<ProfileResponse> {
    try {
      const response = await apiClient.get<ApiResponse<ProfileResponse>>(
        '/api/v1/profile'
      );
      return profileResponseSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  static async updateProfile(data: UpdateProfileRequest): Promise<string> {
    try {
      const validated = updateProfileSchema.parse(data);
      const response = await apiClient.put<ApiResponse<null>>(
        '/api/v1/profile',
        validated
      );
      return response.data.message || 'Profile updated successfully!';
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  static async changePassword(data: ChangePasswordRequest): Promise<string> {
    try {
      const validated = changePasswordSchema.parse(data);
      const response = await apiClient.put<ApiResponse<null>>(
        '/api/v1/profile/password',
        validated
      );
      return response.data.message || 'Password changed successfully!';
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  static async updatePhoto(
    photo: { uri: string; name: string; type: string }
  ): Promise<UpdatePhotoResponse> {
    try {
      const formData = new FormData();
      formData.append('photo', {
        uri: photo.uri,
        name: photo.name,
        type: photo.type,
      } as any);

      const response = await apiClient.upload<ApiResponse<UpdatePhotoResponse>>(
        '/api/v1/profile/photo',
        formData
      );
      return updatePhotoResponseSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }
}
