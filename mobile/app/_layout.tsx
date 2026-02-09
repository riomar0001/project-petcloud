import { Stack } from 'expo-router';
import '../global.css';

export default function RootLayout() {
  const isAuthenticated = true; // Replace with your authentication logic
  return (
    <Stack>
      <Stack.Protected guard={isAuthenticated}>
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
        <Stack.Screen name="index" options={{ headerShown: false }} />
        <Stack.Screen name="registration" options={{ headerShown: false }} />
        <Stack.Screen name="forgot-password" options={{ headerShown: false }} />
        <Stack.Screen name="two-factor" options={{ headerShown: false }} />

      </Stack.Protected>
    </Stack>
  );
}
