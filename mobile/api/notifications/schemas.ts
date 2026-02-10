import { z } from 'zod';

// Notification
export const notificationSchema = z.object({
  notificationId: z.number(),
  message: z.string(),
  type: z.string(),
  createdAt: z.string(),
  isRead: z.boolean(),
  redirectUrl: z.string().nullable().optional(),
});

export type NotificationDto = z.infer<typeof notificationSchema>;

// Notification List Response (paginated with unread count)
export const notificationListResponseSchema = z.object({
  unreadCount: z.number(),
  success: z.boolean(),
  items: z.array(notificationSchema),
  currentPage: z.number(),
  totalPages: z.number(),
  totalCount: z.number(),
  pageSize: z.number(),
  hasPrevious: z.boolean(),
  hasNext: z.boolean(),
});

export type NotificationListResponse = z.infer<typeof notificationListResponseSchema>;

// Unread Count Response
export const unreadCountResponseSchema = z.object({
  unreadCount: z.number(),
});

export type UnreadCountResponse = z.infer<typeof unreadCountResponseSchema>;
