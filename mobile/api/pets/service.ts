import { z } from 'zod';
import { apiClient } from '../client';
import { handleValidationError, ApiResponse } from '../types';
import {
  petListItemSchema,
  petDetailSchema,
  createPetSchema,
  updatePetSchema,
  petCardResponseSchema,
  type PetListItem,
  type PetDetail,
  type CreatePetRequest,
  type UpdatePetRequest,
  type PetCardResponse,
} from './schemas';

export class PetsService {
  /**
   * List all pets for the authenticated owner
   */
  static async listPets(): Promise<PetListItem[]> {
    try {
      const response = await apiClient.get<ApiResponse<PetListItem[]>>(
        '/api/v1/pets'
      );
      const data = response.data.data ?? [];
      return z.array(petListItemSchema).parse(data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Get pet details with paginated appointment history
   */
  static async getPetDetail(
    id: number,
    params?: { page?: number; pageSize?: number; search?: string; categoryFilter?: number }
  ): Promise<PetDetail> {
    try {
      const response = await apiClient.get<ApiResponse<PetDetail>>(
        `/api/v1/pets/${id}`,
        { params }
      );
      return petDetailSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Create a new pet (with optional photo)
   */
  static async createPet(
    data: CreatePetRequest,
    photo?: { uri: string; name: string; type: string }
  ): Promise<PetListItem> {
    try {
      const validated = createPetSchema.parse(data);

      const formData = new FormData();
      formData.append('Name', validated.Name);
      formData.append('Type', validated.Type);
      if (validated.Breed) formData.append('Breed', validated.Breed);
      formData.append('Birthdate', validated.Birthdate);

      if (photo) {
        formData.append('Photo', {
          uri: photo.uri,
          name: photo.name,
          type: photo.type,
        } as any);
      }

      const response = await apiClient.upload<ApiResponse<PetListItem>>(
        '/api/v1/pets',
        formData
      );
      return petListItemSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Update a pet (with optional photo)
   */
  static async updatePet(
    id: number,
    data: UpdatePetRequest,
    photo?: { uri: string; name: string; type: string }
  ): Promise<PetListItem> {
    try {
      const validated = updatePetSchema.parse(data);

      const formData = new FormData();
      if (validated.Name) formData.append('Name', validated.Name);
      if (validated.Type) formData.append('Type', validated.Type);
      if (validated.Breed) formData.append('Breed', validated.Breed);
      if (validated.Birthdate) formData.append('Birthdate', validated.Birthdate);

      if (photo) {
        formData.append('Photo', {
          uri: photo.uri,
          name: photo.name,
          type: photo.type,
        } as any);
      }

      const response = await apiClient.put<ApiResponse<PetListItem>>(
        `/api/v1/pets/${id}`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      );
      return petListItemSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Get breed list for a pet type (dog or cat)
   */
  static async getBreeds(type: string): Promise<string[]> {
    const response = await apiClient.get<ApiResponse<string[]>>(
      '/api/v1/pets/breeds',
      { params: { type } }
    );
    return response.data.data ?? [];
  }

  /**
   * Get pet health card with paginated records
   */
  static async getPetCard(
    id: number,
    params?: { page?: number; pageSize?: number }
  ): Promise<PetCardResponse> {
    try {
      const response = await apiClient.get<ApiResponse<PetCardResponse>>(
        `/api/v1/pets/${id}/card`,
        { params }
      );
      return petCardResponseSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Download pet card PDF (returns blob)
   */
  static async downloadPetCardPdf(id: number): Promise<Blob> {
    const response = await apiClient.get<Blob>(
      `/api/v1/pets/${id}/card/pdf`,
      { responseType: 'blob' }
    );
    return response.data;
  }
}
