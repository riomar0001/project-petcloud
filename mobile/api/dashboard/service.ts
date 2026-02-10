import { z } from 'zod';
import { apiClient } from '../client';
import { handleValidationError, ApiResponse } from '../types';
import {
  dashboardResponseSchema,
  type DashboardResponse,
} from './schemas';

export class DashboardService {
  /**
   * Get owner dashboard data (pets, upcoming appointments, vaccine/deworm due)
   */
  static async getDashboard(): Promise<DashboardResponse> {
    try {
      const response = await apiClient.get<ApiResponse<DashboardResponse>>(
        '/api/v1/dashboard'
      );
      return dashboardResponseSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }
}
