import React, { useState } from 'react';
import { router } from 'expo-router';
import { View, Text, ScrollView, KeyboardAvoidingView, Platform, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { AppButton } from '../components/button';
import { AppInput } from '../components/input';
import { LoginFormData, ValidationErrors } from '../types';
import { useAuthStore } from '@/store/authStore';

export default function LoginScreen() {
  const [formData, setFormData] = useState<LoginFormData>({
    email: '',
    password: ''
  });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isError, setIsError] = useState(false);
  const { login } = useAuthStore();

  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {};

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email';
    }

    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 6) {
      newErrors.password = 'Password must be at least 6 characters';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleLogin = async (): Promise<void> => {
    if (!validateForm()) {
      return;
    }

    setIsError(false);
    setLoading(true);

    try {
      const result = await login(formData.email, formData.password);

      if (result.requires2FA) {
        // Redirect to 2FA screen
        router.push({
          pathname: '/two-factor',
          params: { userId: result.userId?.toString() || '' }
        });
      } else {
        // Success - navigate to home/dashboard
        router.replace('/dashboard');
      }
    } catch (error: any) {
      if (error.message === 'This API is only available for pet owners.') {
        setIsError(true);
        setErrorMessage("You're trying to log in as Admin/Staff. Please use the web portal instead.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-white">
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'} className="flex-1">
        <ScrollView contentContainerStyle={{ flexGrow: 1 }} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
          <View className="flex-1 px-6 pb-8">
            {/* Brand Header */}
            <View className="items-center pb-6 pt-12">
              <View className="mb-4 h-20 w-20 items-center justify-center rounded-3xl bg-mountain-meadow-600">
                <Ionicons name="paw" size={40} color="#FFFFFF" />
              </View>
              <Text className="text-2xl font-bold tracking-tight text-gray-900">PetCloud</Text>
              <Text className="mt-1 text-sm text-gray-400">Your clinic, in the cloud</Text>
            </View>

            {/* Welcome Section */}
            <View className="mb-6 flex items-center">
              <Text className="text-3xl font-bold text-mountain-meadow-500">Welcome back</Text>
              <Text className="mt-1 text-center text-base text-gray-500">Sign in to manage your pet&apos;s health</Text>
            </View>

            {/* Error Message */}
            {isError && (
              <View className="mb-4 flex-row items-center rounded-xl border border-red-200 bg-red-50 px-4 py-3">
                <Ionicons name="alert-circle" size={20} color="#EF4444" />
                <Text className="ml-2 flex-1 text-sm font-medium text-red-600">{errorMessage}</Text>
                <TouchableOpacity onPress={() => setIsError(false)} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
                  <Ionicons name="close" size={18} color="#EF4444" />
                </TouchableOpacity>
              </View>
            )}

            {/* Form Card */}
            <View className="mb-4 rounded-2xl border border-gray-100 bg-white p-5">
              <AppInput
                label="Email"
                placeholder="name@email.com"
                value={formData.email}
                onChangeText={(text) => {
                  setFormData({ ...formData, email: text });
                  setErrors({ ...errors, email: undefined });
                }}
                error={errors.email}
                keyboardType="email-address"
                autoCapitalize="none"
                autoComplete="email"
                icon={<Ionicons name="mail-outline" size={18} color="#9CA3AF" />}
              />

              <AppInput
                label="Password"
                placeholder="Enter your password"
                value={formData.password}
                onChangeText={(text) => {
                  setFormData({ ...formData, password: text });
                  setErrors({ ...errors, password: undefined });
                }}
                error={errors.password}
                secureTextEntry={!showPassword}
                autoCapitalize="none"
                autoComplete="password"
                icon={<Ionicons name="lock-closed-outline" size={18} color="#9CA3AF" />}
                rightIcon={
                  <TouchableOpacity onPress={() => setShowPassword(!showPassword)} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
                    <Ionicons name={showPassword ? 'eye-off-outline' : 'eye-outline'} size={18} color="#9CA3AF" />
                  </TouchableOpacity>
                }
              />

              {/* Forgot Password */}
              <TouchableOpacity onPress={() => router.push('/forgot-password')} className="mb-2 self-end">
                <Text className="text-sm font-medium text-mountain-meadow-600">Forgot password?</Text>
              </TouchableOpacity>
            </View>

            {/* Sign In Button */}
            <AppButton
              title="Sign In"
              onPress={handleLogin}
              loading={loading}
              variant="primary"
              icon={!loading ? <Ionicons name="arrow-forward" size={18} color="#FFFFFF" /> : undefined}
            />

            {/* Divider */}
            <View className="my-6 flex-row items-center">
              <View className="h-px flex-1 bg-gray-200" />
              <Text className="mx-4 text-xs font-medium text-gray-400">OR</Text>
              <View className="h-px flex-1 bg-gray-200" />
            </View>

            {/* Sign Up CTA */}
            <AppButton title="Create an Account" onPress={() => router.push('/registration')} variant="outline" />

            {/* Footer */}
            <View className="mt-auto items-center pt-8">
              <Text className="text-xs text-gray-400">By signing in you agree to our Terms & Privacy Policy</Text>
            </View>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
