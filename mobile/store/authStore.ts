import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import { apiClient, AuthService, ApiError } from '@/api';

const TOKEN_KEYS = {
  ACCESS: 'auth_access_token',
  REFRESH: 'auth_refresh_token',
} as const;

interface AuthStore {
  isAuthenticated: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  requires2FA: boolean;
  userID: number | null;
  isHydrated: boolean;

  // Actions
  login: (email: string, password: string) => Promise<{ requires2FA: boolean; userId?: number | null }>;
  logout: () => Promise<void>;
  setTokens: (accessToken: string, refreshToken: string) => void;
  clearAuth: () => void;
  hydrate: () => Promise<void>;
}

async function saveTokens(accessToken: string, refreshToken: string) {
  await SecureStore.setItemAsync(TOKEN_KEYS.ACCESS, accessToken);
  await SecureStore.setItemAsync(TOKEN_KEYS.REFRESH, refreshToken);
}

async function deleteTokens() {
  await SecureStore.deleteItemAsync(TOKEN_KEYS.ACCESS);
  await SecureStore.deleteItemAsync(TOKEN_KEYS.REFRESH);
}

export const useAuthStore = create<AuthStore>((set) => ({
  // State
  isAuthenticated: false,
  accessToken: null,
  refreshToken: null,
  requires2FA: false,
  userID: null,
  isHydrated: false,

  // Actions
  login: async (email: string, password: string) => {
    try {
      const result = await AuthService.login({ email, password });

      console.log('✅ Login successful:', result);

      if (result.requires2FA) {
        // 2FA required - store userId and return flag
        set({
          requires2FA: true,
          userID: result.twoFactorUserId ?? null
        });

        return {
          requires2FA: true,
          userId: result.twoFactorUserId ?? null
        };
      } else {
        // Direct login - store tokens
        const accessToken = result.accessToken ?? null;
        const refreshToken = result.refreshToken ?? null;

        if (accessToken) {
          apiClient.setToken(accessToken);
        }

        if (accessToken && refreshToken) {
          await saveTokens(accessToken, refreshToken);
        }

        set({
          isAuthenticated: true,
          accessToken,
          refreshToken,
          requires2FA: false
        });

        return {
          requires2FA: false
        };
      }
    } catch (error: any) {
      if (error instanceof ApiError) {
        console.log({
          message: error.message,
          status: error.status,
          errors: error.errors
        });
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
      await deleteTokens();
      set({
        isAuthenticated: false,
        accessToken: null,
        refreshToken: null,
        requires2FA: false,
        userID: null
      });
    }
  },

  setTokens: (accessToken: string, refreshToken: string) => {
    apiClient.setToken(accessToken);
    saveTokens(accessToken, refreshToken);
    set({
      isAuthenticated: true,
      accessToken,
      refreshToken
    });
  },

  clearAuth: () => {
    apiClient.setToken(null);
    deleteTokens();
    set({
      isAuthenticated: false,
      accessToken: null,
      refreshToken: null,
      requires2FA: false,
      userID: null
    });
  },

  hydrate: async () => {
    try {
      const accessToken = await SecureStore.getItemAsync(TOKEN_KEYS.ACCESS);
      const refreshToken = await SecureStore.getItemAsync(TOKEN_KEYS.REFRESH);

      if (accessToken && refreshToken) {
        apiClient.setToken(accessToken);
        set({
          isAuthenticated: true,
          accessToken,
          refreshToken,
          isHydrated: true
        });
      } else {
        set({ isHydrated: true });
      }
    } catch (error) {
      console.warn('Failed to hydrate auth state:', error);
      set({ isHydrated: true });
    }
  }
}));
