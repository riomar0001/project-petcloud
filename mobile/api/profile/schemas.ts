import { z } from 'zod';

// Get Profile
export const profileResponseSchema = z.object({
  userId: z.number(),
  ownerId: z.number(),
  firstName: z.string(),
  lastName: z.string(),
  email: z.string(),
  phone: z.string().nullable(),
  profileImageUrl: z.string().nullable(),
  createdAt: z.string(),
});

export type ProfileResponse = z.infer<typeof profileResponseSchema>;

// Update Profile
export const updateProfileSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(50),
  lastName: z.string().min(1, 'Last name is required').max(50),
  phone: z.string().regex(/^\d{11}$/, 'Phone number must be exactly 11 digits'),
});

export type UpdateProfileRequest = z.infer<typeof updateProfileSchema>;

// Change Password
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
    newPassword: z.string().min(8, 'Password must be at least 8 characters'),
    confirmPassword: z.string().min(1, 'Please confirm your password'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

export type ChangePasswordRequest = z.infer<typeof changePasswordSchema>;
