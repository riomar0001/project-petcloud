import React, { useState, useRef, useEffect, useCallback } from 'react';
import { router } from 'expo-router';
import {
  View,
  Text,
  TextInput,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
  Animated,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { AppButton } from '../components/button';
import { useAuthStore } from '@/store/authStore';
import { AuthService, apiClient, ApiError } from '@/api';

const RESEND_COOLDOWN = 60;

export default function TwoFactorScreen() {
  const { userID, setTokens } = useAuthStore();
  const [code, setCode] = useState<string[]>(['', '', '', '', '', '']);
  const [error, setError] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(RESEND_COOLDOWN);
  const [canResend, setCanResend] = useState(false);
  const [isError, setIsError] = useState(false);

  // Redirect if no userID
  useEffect(() => {
    if (!userID) {
      router.replace('/');
    }
  }, [userID]);

  const inputRefs = useRef<(TextInput | null)[]>([]);
  const shakeAnim = useRef(new Animated.Value(0)).current;

  // Countdown timer
  useEffect(() => {
    if (resendCooldown <= 0) {
      setCanResend(true);
      return;
    }
    const timer = setTimeout(
      () => setResendCooldown((c) => c - 1),
      1000,
    );
    return () => clearTimeout(timer);
  }, [resendCooldown]);

  useEffect(() => {
    inputRefs.current[0]?.focus();
  }, []);

  const triggerShake = useCallback(() => {
    Animated.sequence([
      Animated.timing(shakeAnim, {
        toValue: 10,
        duration: 60,
        useNativeDriver: true,
      }),
      Animated.timing(shakeAnim, {
        toValue: -10,
        duration: 60,
        useNativeDriver: true,
      }),
      Animated.timing(shakeAnim, {
        toValue: 6,
        duration: 60,
        useNativeDriver: true,
      }),
      Animated.timing(shakeAnim, {
        toValue: -6,
        duration: 60,
        useNativeDriver: true,
      }),
      Animated.timing(shakeAnim, {
        toValue: 0,
        duration: 60,
        useNativeDriver: true,
      }),
    ]).start();
  }, [shakeAnim]);

  const handleCodeChange = (text: string, index: number): void => {
    if (text && !/^\d+$/.test(text)) return;

    const newCode = [...code];
    newCode[index] = text;
    setCode(newCode);
    setError('');

    if (text && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyPress = (key: string, index: number): void => {
    if (key === 'Backspace' && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handleVerify = async (): Promise<void> => {
    const fullCode = code.join('');

    if (fullCode.length !== 6) {
      setError('Please enter the complete 6-digit code');
      triggerShake();
      return;
    }

    if (!userID) {
      setError('Session expired. Please login again.');
      return;
    }

    setLoading(true);
    setError('');
    setIsError(false);

    try {
      const result = await AuthService.verify2FA({
        userId: userID,
        code: fullCode,
      });

      // Set tokens and navigate to home
      apiClient.setToken(result.accessToken);
      setTokens(result.accessToken, result.refreshToken);

      router.replace('/dashboard');
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message || 'Invalid verification code');
        setIsError(true);
      } else {
        setError('An error occurred. Please try again.');
        setIsError(true);
      }
      triggerShake();
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async (): Promise<void> => {
    if (!canResend) return;

    setCode(['', '', '', '', '', '']);
    setError('');
    setCanResend(false);
    setResendCooldown(RESEND_COOLDOWN);
    inputRefs.current[0]?.focus();
  };

  const isCodeComplete = code.every((digit) => digit !== '');

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
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

  return (
    <SafeAreaView className="flex-1 bg-white">
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        className="flex-1"
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
              <Ionicons name="shield-checkmark" size={36} color="#059666" />
            </View>
            <Text className="mb-2 text-center text-2xl font-bold text-gray-900">
              Verify your identity
            </Text>
            <Text className="text-center text-sm text-gray-500">
              We sent a 6-digit code to your email
            </Text>
            <Text className="mt-0.5 text-center text-sm font-medium text-mountain-meadow-600">
              Check your inbox to continue
            </Text>
          </View>

          {/* Error Message */}
          {isError && (
            <View className="mb-4 flex-row items-center rounded-xl border border-red-200 bg-red-50 px-4 py-3">
              <Ionicons name="alert-circle" size={20} color="#EF4444" />
              <Text className="ml-2 flex-1 text-sm font-medium text-red-600">
                Invalid verification code. Please try again.
              </Text>
              <TouchableOpacity onPress={() => setIsError(false)} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
                <Ionicons name="close" size={18} color="#EF4444" />
              </TouchableOpacity>
            </View>
          )}

          {/* Code Input */}
          <Animated.View
            style={{ transform: [{ translateX: shakeAnim }] }}
          >
            <View className="mb-2 flex-row justify-between gap-2.5">
              {code.map((digit, index) => {
                const isFilled = digit !== '';
                const isNext =
                  !isFilled && code.slice(0, index).every((d) => d !== '');

                return (
                  <TextInput
                    key={index}
                    ref={(ref) => {
                      inputRefs.current[index] = ref;
                    }}
                    value={digit}
                    onChangeText={(text) => handleCodeChange(text, index)}
                    onKeyPress={({ nativeEvent }) =>
                      handleKeyPress(nativeEvent.key, index)
                    }
                    keyboardType="number-pad"
                    maxLength={1}
                    selectTextOnFocus
                    className={`h-14 flex-1 rounded-xl border-2 bg-gray-50 text-center text-2xl font-bold ${
                      error
                        ? 'border-red-400'
                        : isFilled
                          ? 'border-mountain-meadow-500 bg-mountain-meadow-50'
                          : 'border-gray-200'
                    }`}
                    style={{ textAlign: 'center' }}
                  />
                );
              })}
            </View>

            {error && (
              <Text className="mt-2 text-center text-sm text-red-500">
                {error}
              </Text>
            )}
          </Animated.View>

          {/* Verify Button */}
          <View className="mt-6">
            <AppButton
              title="Verify Code"
              onPress={handleVerify}
              loading={loading}
              disabled={!isCodeComplete}
              variant="primary"
              icon={
                !loading && isCodeComplete ? (
                  <Ionicons name="checkmark" size={18} color="#FFFFFF" />
                ) : undefined
              }
            />
          </View>

          {/* Resend */}
          <View className="mt-6 items-center">
            {canResend ? (
              <TouchableOpacity onPress={handleResendCode}>
                <Text className="text-sm font-semibold text-mountain-meadow-600">
                  Resend Code
                </Text>
              </TouchableOpacity>
            ) : (
              <View className="items-center">
                <Text className="text-sm text-gray-400">
                  Resend code in
                </Text>
                <Text className="mt-1 text-lg font-bold text-gray-700">
                  {formatTime(resendCooldown)}
                </Text>
              </View>
            )}
          </View>

          {/* Help section */}
          <View className="mt-auto">
            <View className="rounded-xl border border-gray-100 bg-gray-50 p-4">
              <View className="flex-row items-start">
                <Ionicons
                  name="help-circle-outline"
                  size={18}
                  color="#9CA3AF"
                  style={{ marginTop: 1 }}
                />
                <View className="ml-2.5 flex-1">
                  <Text className="text-xs font-semibold text-gray-600">
                    Didn&apos;t receive the code?
                  </Text>
                  <Text className="mt-0.5 text-xs text-gray-400">
                    Check your spam folder or request a new code after the timer
                    expires.
                  </Text>
                </View>
              </View>
            </View>
          </View>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
