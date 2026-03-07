import { create } from 'zustand';
import { ProfileService } from '@/api';
import type { ProfileResponse } from '@/api';

interface ProfileStore {
  profile: ProfileResponse | null;
  isLoading: boolean;
  fetchProfile: () => Promise<void>;
  setProfile: (profile: ProfileResponse) => void;
  patchProfile: (fields: Partial<ProfileResponse>) => void;
  setPhotoUrl: (url: string) => void;
  clearProfile: () => void;
}

export const useProfileStore = create<ProfileStore>((set) => ({
  profile: null,
  isLoading: false,

  fetchProfile: async () => {
    set({ isLoading: true });
    try {
      const data = await ProfileService.getProfile();
      set({ profile: data });
    } catch {
      // silently fail — caller handles UI feedback
    } finally {
      set({ isLoading: false });
    }
  },

  setProfile: (profile) => set({ profile }),

  patchProfile: (fields) =>
    set((state) => ({
      profile: state.profile ? { ...state.profile, ...fields } : null,
    })),

  setPhotoUrl: (url) =>
    set((state) => ({
      profile: state.profile
        ? { ...state.profile, profileImageUrl: url }
        : null,
    })),

  clearProfile: () => set({ profile: null }),
}));
