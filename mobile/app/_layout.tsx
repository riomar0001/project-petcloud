import { useEffect } from 'react';
import { Stack } from 'expo-router';
import '../global.css';
import { useAuthStore } from '@/store/authStore';
import { View, ActivityIndicator } from 'react-native';

export default function RootLayout() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isHydrated = useAuthStore((state) => state.isHydrated);
  const hydrate = useAuthStore((state) => state.hydrate);

  useEffect(() => {
    hydrate();
  }, []);

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
