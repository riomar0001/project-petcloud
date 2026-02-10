import { z } from 'zod';

// Appointment List Item
export const appointmentListItemSchema = z.object({
  appointmentId: z.number(),
  appointmentDate: z.string(),
  status: z.string(),
  groupId: z.number().nullable().optional(),
  notes: z.string().nullable().optional(),
  petId: z.number(),
  petName: z.string(),
  serviceType: z.string().nullable().optional(),
  serviceSubtype: z.string().nullable().optional(),
  isOwnAppointment: z.boolean(),
});

export type AppointmentListItem = z.infer<typeof appointmentListItemSchema>;

// Create Appointment
export const createAppointmentSchema = z.object({
  petId: z.number(),
  categoryId: z.number(),
  subtypeId: z.number().nullable().optional(),
  appointmentDate: z.string().min(1, 'Appointment date is required'),
  appointmentTime: z.string().min(1, 'Appointment time is required'),
  notes: z.string().nullable().optional(),
});

export type CreateAppointmentRequest = z.infer<typeof createAppointmentSchema>;

// Bulk Appointment Item
export const bulkAppointmentItemSchema = z.object({
  petId: z.number(),
  categoryId: z.number(),
  subtypeId: z.number().nullable().optional(),
  appointmentDate: z.string().min(1, 'Appointment date is required'),
  appointmentTime: z.string().min(1, 'Appointment time is required'),
  notes: z.string().nullable().optional(),
});

export type BulkAppointmentItem = z.infer<typeof bulkAppointmentItemSchema>;

// Create Bulk Appointment
export const createBulkAppointmentSchema = z.object({
  appointments: z.array(bulkAppointmentItemSchema).min(1, 'At least one appointment is required'),
});

export type CreateBulkAppointmentRequest = z.infer<typeof createBulkAppointmentSchema>;

// Bulk Appointment Response
export const bulkAppointmentResponseSchema = z.object({
  groupId: z.number(),
  count: z.number(),
});

export type BulkAppointmentResponse = z.infer<typeof bulkAppointmentResponseSchema>;

// Time Slot
export const timeSlotSchema = z.object({
  time: z.string(),
  available: z.boolean(),
});

export type TimeSlot = z.infer<typeof timeSlotSchema>;

// Time Slots Response
export const timeSlotsResponseSchema = z.object({
  date: z.string(),
  slots: z.array(timeSlotSchema),
});

export type TimeSlotsResponse = z.infer<typeof timeSlotsResponseSchema>;

// Service Subtype
export const serviceSubtypeSchema = z.object({
  subtypeId: z.number(),
  serviceSubType: z.string(),
});

export type ServiceSubtype = z.infer<typeof serviceSubtypeSchema>;

// Service Category
export const serviceCategorySchema = z.object({
  categoryId: z.number(),
  serviceType: z.string(),
  subtypes: z.array(serviceSubtypeSchema),
});

export type ServiceCategory = z.infer<typeof serviceCategorySchema>;
