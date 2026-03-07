import { useEffect, useRef } from 'react';
import { Stack } from 'expo-router';
import '../global.css';
import { useAuthStore } from '@/store/authStore';
import { useProfileStore } from '@/store/useProfileStore';
import { useNotificationStore } from '@/store/useNotificationStore';
import { View, ActivityIndicator } from 'react-native';

export default function RootLayout() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isHydrated = useAuthStore((state) => state.isHydrated);
  const hydrate = useAuthStore((state) => state.hydrate);
  const clearProfile = useProfileStore((state) => state.clearProfile);
  const resetNotifications = useNotificationStore((state) => state.reset);

  // Track previous auth state to detect logout
  const wasAuthenticated = useRef(isAuthenticated);

  useEffect(() => {
    hydrate();
  }, []);

  // Clear derived stores when the user logs out
  useEffect(() => {
    if (wasAuthenticated.current && !isAuthenticated) {
      clearProfile();
      resetNotifications();
    }
    wasAuthenticated.current = isAuthenticated;
  }, [isAuthenticated]);

  if (!isHydrated) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#FFFFFF' }}>
        <ActivityIndicator size="large" color="#059666" />
      </View>
    );
  }

  return (
    <Stack>
      <Stack.Protected guard={isAuthenticated}>
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
      </Stack.Protected>
      <Stack.Protected guard={!isAuthenticated}>
        <Stack.Screen name="index" options={{ headerShown: false }} />
        <Stack.Screen name="registration" options={{ headerShown: false }} />
        <Stack.Screen name="forgot-password" options={{ headerShown: false }} />
        <Stack.Screen name="two-factor" options={{ headerShown: false }} />
      </Stack.Protected>
    </Stack>
  );
}
