import React, { useState } from 'react';
import { router } from 'expo-router';
import {
  View,
  Text,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { AppButton } from '../components/button';
import { AppInput } from '../components/input';
import { ForgotPasswordFormData, ValidationErrors } from '../types';

export default function ForgotPasswordScreen() {
  const [formData, setFormData] = useState<ForgotPasswordFormData>({
    email: '',
  });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [loading, setLoading] = useState(false);
  const [emailSent, setEmailSent] = useState(false);
  const [isError, setIsError] = useState(true);

  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {};

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSendResetLink = async (): Promise<void> => {
    if (!validateForm()) return;

    setLoading(true);
    setTimeout(() => {
      setLoading(false);
      setEmailSent(true);
    }, 2000);
  };

  // Mask email for display
  const maskEmail = (e: string) => {
    const [user, domain] = e.split('@');
    if (!domain) return e;
    const masked =
      user.length > 2
        ? user[0] + '*'.repeat(user.length - 2) + user[user.length - 1]
        : user;
    return `${masked}@${domain}`;
  };

  if (emailSent) {
    return (
      <SafeAreaView className="flex-1 bg-white">
        <View className="flex-1 px-6 pb-8">
          {/* Top bar */}
          <View className="flex-row items-center pb-6 pt-3">
            <TouchableOpacity
              onPress={() => router.push('/')}
              className="h-10 w-10 items-center justify-center rounded-full bg-gray-100"
              activeOpacity={0.7}
            >
              <Ionicons name="arrow-back" size={20} color="#374151" />
            </TouchableOpacity>
          </View>

          <View className="flex-1 justify-center">
            {/* Success Illustration */}
            <View className="items-center pb-8">
              <View className="mb-5 h-24 w-24 items-center justify-center rounded-3xl bg-mountain-meadow-100">
                <Ionicons name="mail-open" size={44} color="#059666" />
              </View>
              <Text className="mb-2 text-center text-2xl font-bold text-gray-900">
                Check your inbox
              </Text>
              <Text className="text-center text-sm text-gray-500">
                We sent a reset link to
              </Text>
              <Text className="mt-0.5 text-center text-sm font-semibold text-mountain-meadow-600">
                {maskEmail(formData.email)}
              </Text>
            </View>

            {/* Instructions Card */}
            <View className="mb-6 rounded-xl border border-gray-100 bg-gray-50 p-5">
              {[
                {
                  icon: 'mail-outline' as const,
                  text: 'Open the email we sent you',
                },
                {
                  icon: 'link-outline' as const,
                  text: 'Click the password reset link',
                },
                {
                  icon: 'lock-closed-outline' as const,
                  text: 'Create your new password',
                },
              ].map((step, i) => (
                <View
                  key={i}
                  className={`flex-row items-center ${i > 0 ? 'mt-4' : ''}`}
                >
                  <View className="mr-3 h-8 w-8 items-center justify-center rounded-lg bg-mountain-meadow-100">
                    <Ionicons name={step.icon} size={16} color="#059666" />
                  </View>
                  <Text className="flex-1 text-sm text-gray-700">
                    {step.text}
                  </Text>
                </View>
              ))}
            </View>



            <AppButton
              title="Back to Sign In"
              onPress={() => router.push('/')}
              variant="ghost"
            />
          </View>

          {/* Help text */}
          <View className="mt-auto items-center pt-4">
            <Text className="text-center text-xs text-gray-400">
              The reset link expires in 24 hours.{'\n'}Check your spam folder if
              you don&apos;t see it.
            </Text>
          </View>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-white">
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        className="flex-1"
      >
        <ScrollView
          contentContainerStyle={{ flexGrow: 1 }}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View className="flex-1 px-6 pb-8">
            {/* Top bar */}
            <View className="flex-row items-center pb-6 pt-3">
              <TouchableOpacity
                onPress={() => router.push('/')}
                className="h-10 w-10 items-center justify-center rounded-full bg-gray-100"
                activeOpacity={0.7}
              >
                <Ionicons name="arrow-back" size={20} color="#374151" />
              </TouchableOpacity>
            </View>

            {/* Header */}
            <View className="items-center pb-8">
              <View className="mb-5 h-20 w-20 items-center justify-center rounded-3xl bg-mountain-meadow-100">
                <Ionicons name="key-outline" size={36} color="#059666" />
              </View>
              <Text className="mb-2 text-center text-2xl font-bold text-gray-900">
                Forgot password?
              </Text>
              <Text className="px-6 text-center text-sm leading-5 text-gray-500">
                No worries! Enter the email associated with your account and
                we&apos;ll send a reset link.
              </Text>
            </View>

            {/* Error Message */}
            {isError && (
              <View className="mb-4 flex-row items-center rounded-xl border border-red-200 bg-red-50 px-4 py-3">
                <Ionicons name="alert-circle" size={20} color="#EF4444" />
                <Text className="ml-2 flex-1 text-sm font-medium text-red-600">
                  No account found with this email address.
                </Text>
                <TouchableOpacity onPress={() => setIsError(false)} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
                  <Ionicons name="close" size={18} color="#EF4444" />
                </TouchableOpacity>
              </View>
            )}

            {/* Form */}
            <View className="mb-4 rounded-2xl border border-gray-100 bg-white p-5">
              <AppInput
                label="Email Address"
                placeholder="name@email.com"
                value={formData.email}
                onChangeText={(text: string) => {
                  setFormData({ email: text });
                  setErrors({ email: undefined });
                }}
                error={errors.email}
                keyboardType="email-address"
                autoCapitalize="none"
                autoComplete="email"
                icon={
                  <Ionicons name="mail-outline" size={18} color="#9CA3AF" />
                }
              />
            </View>

            <AppButton
              title="Send Reset Link"
              onPress={handleSendResetLink}
              loading={loading}
              variant="primary"
              icon={
                !loading ? (
                  <Ionicons name="send" size={16} color="#FFFFFF" />
                ) : undefined
              }
            />

            {/* Back to login */}
            <View className="mt-6 flex-row items-center justify-center">
              <TouchableOpacity
                onPress={() => router.push('/')}
                className="flex-row items-center"
              >
                <Ionicons name="arrow-back" size={14} color="#059666" />
                <Text className="ml-1 text-sm font-semibold text-mountain-meadow-600">
                  Back to Sign In
                </Text>
              </TouchableOpacity>
            </View>

            {/* Info card */}
            <View className="mt-auto pt-8">
              <View className="rounded-xl border border-gray-100 bg-gray-50 p-4">
                <View className="flex-row items-start">
                  <Ionicons
                    name="information-circle-outline"
                    size={18}
                    color="#9CA3AF"
                    style={{ marginTop: 1 }}
                  />
                  <View className="ml-2.5 flex-1">
                    <Text className="text-xs font-semibold text-gray-600">
                      Need help?
                    </Text>
                    <Text className="mt-0.5 text-xs text-gray-400">
                      If you&apos;re having trouble, contact our support team for
                      assistance.
                    </Text>
                  </View>
                </View>
              </View>
            </View>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
