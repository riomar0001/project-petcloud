import { z } from 'zod';
import { apiClient } from '../client';
import { handleValidationError, ApiResponse } from '../types';
import {
  appointmentListItemSchema,
  createAppointmentSchema,
  createBulkAppointmentSchema,
  bulkAppointmentResponseSchema,
  timeSlotsResponseSchema,
  serviceCategorySchema,
  type AppointmentListItem,
  type CreateAppointmentRequest,
  type CreateBulkAppointmentRequest,
  type BulkAppointmentResponse,
  type TimeSlotsResponse,
  type ServiceCategory,
} from './schemas';

export class AppointmentsService {
  /**
   * List all appointments across the clinic
   */
  static async listAppointments(): Promise<AppointmentListItem[]> {
    try {
      const response = await apiClient.get<ApiResponse<AppointmentListItem[]>>(
        '/api/v1/appointments'
      );
      const data = response.data.data ?? [];
      return z.array(appointmentListItemSchema).parse(data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Create a single appointment
   */
  static async createAppointment(data: CreateAppointmentRequest): Promise<string> {
    try {
      const validated = createAppointmentSchema.parse(data);
      const response = await apiClient.post<ApiResponse<null>>(
        '/api/v1/appointments',
        validated
      );
      return response.data.message || 'Appointment created successfully!';
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Create grouped appointments (multiple services at same time slot)
   */
  static async createBulkAppointments(
    data: CreateBulkAppointmentRequest
  ): Promise<BulkAppointmentResponse> {
    try {
      const validated = createBulkAppointmentSchema.parse(data);
      const response = await apiClient.post<ApiResponse<BulkAppointmentResponse>>(
        '/api/v1/appointments/bulk',
        validated
      );
      return bulkAppointmentResponseSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Request cancellation for all appointments in a group
   */
  static async cancelAppointment(id: number): Promise<string> {
    const response = await apiClient.post<ApiResponse<null>>(
      `/api/v1/appointments/${id}/cancel`
    );
    return response.data.message || 'Cancellation requested successfully!';
  }

  /**
   * Get available time slots for a given date
   */
  static async getTimeSlots(date: string): Promise<TimeSlotsResponse> {
    try {
      const response = await apiClient.get<ApiResponse<TimeSlotsResponse>>(
        '/api/v1/appointments/time-slots',
        { params: { date } }
      );
      return timeSlotsResponseSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * List all service categories with subtypes
   */
  static async getServices(): Promise<ServiceCategory[]> {
    try {
      const response = await apiClient.get<ApiResponse<ServiceCategory[]>>(
        '/api/v1/appointments/services'
      );
      const data = response.data.data ?? [];
      return z.array(serviceCategorySchema).parse(data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }
}
