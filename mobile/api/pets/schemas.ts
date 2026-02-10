import { z } from 'zod';

// Pet List Item
export const petListItemSchema = z.object({
  petId: z.number(),
  name: z.string(),
  type: z.string(),
  breed: z.string(),
  birthdate: z.string(),
  photoUrl: z.string().nullable().optional(),
  age: z.string(),
  createdAt: z.string(),
});

export type PetListItem = z.infer<typeof petListItemSchema>;

// Pet Detail
export const petDetailSchema = z.object({
  petId: z.number(),
  name: z.string(),
  type: z.string(),
  breed: z.string(),
  birthdate: z.string(),
  photoUrl: z.string().nullable().optional(),
  age: z.string(),
  ownerName: z.string(),
});

export type PetDetail = z.infer<typeof petDetailSchema>;

// Create Pet
export const createPetSchema = z.object({
  Name: z.string().min(1, 'Pet name is required').max(100),
  Type: z.string().min(1, 'Pet type is required').max(100),
  Breed: z.string().max(100).optional(),
  Birthdate: z.string().min(1, 'Birthdate is required'),
});

export type CreatePetRequest = z.infer<typeof createPetSchema>;

// Update Pet
export const updatePetSchema = z.object({
  Name: z.string().max(100).optional(),
  Type: z.string().max(100).optional(),
  Breed: z.string().max(100).optional(),
  Birthdate: z.string().optional(),
});

export type UpdatePetRequest = z.infer<typeof updatePetSchema>;

// Pet Card Record
export const petCardRecordSchema = z.object({
  appointmentId: z.number(),
  appointmentDate: z.string(),
  notes: z.string().nullable().optional(),
  serviceType: z.string().nullable().optional(),
  serviceSubtype: z.string().nullable().optional(),
  administeredBy: z.string().nullable().optional(),
  dueDate: z.string().nullable().optional(),
});

export type PetCardRecord = z.infer<typeof petCardRecordSchema>;

// Pet Card Response
export const petCardResponseSchema = z.object({
  pet: petListItemSchema,
  ownerName: z.string(),
  ownerPhone: z.string(),
  ownerEmail: z.string(),
  ageInMonths: z.number(),
  records: z.array(petCardRecordSchema),
  currentPage: z.number(),
  totalPages: z.number(),
  totalRecords: z.number(),
});

export type PetCardResponse = z.infer<typeof petCardResponseSchema>;
