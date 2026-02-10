import { z } from 'zod';
import { apiClient } from '../client';
import { handleValidationError, ApiResponse } from '../types';
import {
  notificationListResponseSchema,
  unreadCountResponseSchema,
  type NotificationListResponse,
  type UnreadCountResponse,
} from './schemas';

export class NotificationsService {
  /**
   * List paginated notifications with optional filters
   */
  static async listNotifications(params?: {
    typeFilter?: string;
    statusFilter?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<NotificationListResponse> {
    try {
      const response = await apiClient.get<NotificationListResponse>(
        '/api/v1/notifications',
        { params }
      );
      return notificationListResponseSchema.parse(response.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Get total unread notification count
   */
  static async getUnreadCount(): Promise<number> {
    try {
      const response = await apiClient.get<ApiResponse<UnreadCountResponse>>(
        '/api/v1/notifications/unread-count'
      );
      const data = unreadCountResponseSchema.parse(response.data.data);
      return data.unreadCount;
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }

  /**
   * Mark a single notification as read
   */
  static async markAsRead(id: number): Promise<string> {
    const response = await apiClient.put<ApiResponse<null>>(
      `/api/v1/notifications/${id}/read`
    );
    return response.data.message || 'Notification marked as read.';
  }

  /**
   * Mark all unread notifications as read
   */
  static async markAllAsRead(): Promise<string> {
    const response = await apiClient.put<ApiResponse<null>>(
      '/api/v1/notifications/read-all'
    );
    return response.data.message || 'All notifications marked as read.';
  }
}
