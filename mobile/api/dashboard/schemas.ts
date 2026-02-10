import { z } from 'zod';

// Dashboard Pet
export const dashboardPetSchema = z.object({
  petId: z.number(),
  name: z.string(),
  breed: z.string(),
  photoUrl: z.string().nullable().optional(),
  birthdate: z.string(),
});

export type DashboardPet = z.infer<typeof dashboardPetSchema>;

// Dashboard Appointment
export const dashboardAppointmentSchema = z.object({
  appointmentId: z.number(),
  appointmentDate: z.string(),
  status: z.string(),
  petId: z.number(),
  petName: z.string(),
  serviceType: z.string().nullable().optional(),
});

export type DashboardAppointment = z.infer<typeof dashboardAppointmentSchema>;

// Dashboard Vaccine Due
export const dashboardVaccineDueSchema = z.object({
  appointmentId: z.number(),
  dueDate: z.string().nullable().optional(),
  petId: z.number(),
  petName: z.string(),
  serviceType: z.string().nullable().optional(),
});

export type DashboardVaccineDue = z.infer<typeof dashboardVaccineDueSchema>;

// Dashboard Deworm Due
export const dashboardDewormDueSchema = z.object({
  appointmentId: z.number(),
  dueDate: z.string().nullable().optional(),
  petId: z.number(),
  petName: z.string(),
  serviceType: z.string().nullable().optional(),
});

export type DashboardDewormDue = z.infer<typeof dashboardDewormDueSchema>;

// Dashboard Response
export const dashboardResponseSchema = z.object({
  userName: z.string(),
  pets: z.array(dashboardPetSchema),
  upcomingAppointments: z.array(dashboardAppointmentSchema),
  vaccineDue: z.array(dashboardVaccineDueSchema),
  dewormDue: z.array(dashboardDewormDueSchema),
});

export type DashboardResponse = z.infer<typeof dashboardResponseSchema>;
