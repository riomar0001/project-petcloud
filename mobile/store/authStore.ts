import { create } from 'zustand';
import { apiClient, AuthService, ApiError } from '@/api';

interface AuthStore {
  isAuthenticated: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  requires2FA: boolean;
  userID: number | null;

  // Actions
  login: (email: string, password: string) => Promise<{ requires2FA: boolean; userId?: number | null }>;
  logout: () => Promise<void>;
  setTokens: (accessToken: string, refreshToken: string) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthStore>((set) => ({
  // State
  isAuthenticated: false,
  accessToken: null,
  refreshToken: null,
  requires2FA: false,
  userID: null,

  // Actions
  login: async (email: string, password: string) => {
    try {
      const result = await AuthService.login({ email, password });

      console.log('✅ Login successful:', result);

      if (result.requires2FA) {
        // 2FA required - store userId and return flag
        set({
          requires2FA: true,
          userID: result.twoFactorUserId ?? null,
        });

        return {
          requires2FA: true,
          userId: result.twoFactorUserId ?? null,
        };
      } else {
        // Direct login - store tokens
        const accessToken = result.accessToken ?? null;
        const refreshToken = result.refreshToken ?? null;

        if (accessToken) {
          apiClient.setToken(accessToken);
        }

        set({
          isAuthenticated: true,
          accessToken,
          refreshToken,
          requires2FA: false,
        });

        return {
          requires2FA: false,
        };
      }
    } catch (error: any) {
      if (error instanceof ApiError) {
        console.error('❌ Login failed:', error.message);
        console.error('Status:', error.status);
        console.error('Errors:', error.errors);
      }
      throw error;
    }
  },

  logout: async () => {
    try {
      await AuthService.logout();
    } catch (error: any) {
      console.warn('Logout API call failed:', error);
    } finally {
      apiClient.setToken(null);
      set({
        isAuthenticated: false,
        accessToken: null,
        refreshToken: null,
        requires2FA: false,
        userID: null,
      });
    }
  },

  setTokens: (accessToken: string, refreshToken: string) => {
    apiClient.setToken(accessToken);
    set({
      isAuthenticated: true,
      accessToken,
      refreshToken,
    });
  },

  clearAuth: () => {
    apiClient.setToken(null);
    set({
      isAuthenticated: false,
      accessToken: null,
      refreshToken: null,
      requires2FA: false,
      userID: null,
    });
  },
}));
