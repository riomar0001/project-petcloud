import { create } from 'zustand';

export type Pet = {
    id: string;
    name: string;
    species: string;
    breed: string;
    weight: number;
    gender: string;
    birthday: string;
    photoUri?: string;
};

export type Appointment = {
    id: string;
    petId: string;
    serviceType: string;
    date: string;
    time: string;
    notes?: string;
};

interface AppState {
    pets: Pet[];
    appointments: Appointment[];
    addPet: (pet: Omit<Pet, 'id'>) => void;
    addAppointment: (appointment: Omit<Appointment, 'id'>) => void;
}

// Mock Initial Data
const MOCK_PETS: Pet[] = [
    {
        id: '1',
        name: 'Bella',
        species: 'Dog',
        breed: 'Golden Retriever',
        weight: 25,
        gender: 'Female',
        birthday: '2021-05-12',
    },
    {
        id: '2',
        name: 'Luna',
        species: 'Cat',
        breed: 'Siamese',
        weight: 4.5,
        gender: 'Female',
        birthday: '2022-08-20',
    },
];

const MOCK_APPOINTMENTS: Appointment[] = [
    {
        id: '1',
        petId: '1',
        serviceType: 'Checkup',
        date: '2026-03-10',
        time: '10:00 AM',
        notes: 'Annual wellness check',
    },
];

export const useAppStore = create<AppState>()((set) => ({
    pets: MOCK_PETS,
    appointments: MOCK_APPOINTMENTS,

    addPet: (petData) =>
        set((state) => ({
            pets: [
                ...state.pets,
                {
                    ...petData,
                    id: Math.random().toString(36).substr(2, 9),
                },
            ],
        })),

    addAppointment: (appointmentData) =>
        set((state) => ({
            appointments: [
                ...state.appointments,
                {
                    ...appointmentData,
                    id: Math.random().toString(36).substr(2, 9),
                },
            ],
        })),
}));
