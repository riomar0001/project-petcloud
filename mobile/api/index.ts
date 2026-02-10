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

// Profile
export { ProfileService } from './profile/service';
export type {
  ProfileResponse,
  UpdateProfileRequest,
  ChangePasswordRequest,
  UpdatePhotoResponse,
} from './profile/schemas';

// Pets
export { PetsService } from './pets/service';
export type {
  PetListItem,
  PetDetail,
  CreatePetRequest,
  UpdatePetRequest,
  PetCardRecord,
  PetCardResponse,
} from './pets/schemas';

// Appointments
export { AppointmentsService } from './appointments/service';
export type {
  AppointmentListItem,
  CreateAppointmentRequest,
  CreateBulkAppointmentRequest,
  BulkAppointmentItem,
  BulkAppointmentResponse,
  TimeSlot,
  TimeSlotsResponse,
  ServiceCategory,
  ServiceSubtype,
} from './appointments/schemas';

// Dashboard
export { DashboardService } from './dashboard/service';
export type {
  DashboardResponse,
  DashboardPet,
  DashboardAppointment,
  DashboardVaccineDue,
  DashboardDewormDue,
} from './dashboard/schemas';

// Notifications
export { NotificationsService } from './notifications/service';
export type {
  NotificationDto,
  NotificationListResponse,
  UnreadCountResponse,
} from './notifications/schemas';
