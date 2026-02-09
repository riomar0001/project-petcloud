import React, { useState, useRef } from 'react';
import {
  View,
  Text,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
  Animated,
  Dimensions,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { AppButton } from '../components/button';
import { AppInput } from '../components/input';
import { ProgressSteps } from '../components/progress-steps';
import { RegisterFormData, ValidationErrors } from '../types';

const STEP_LABELS = ['Personal', 'Security', 'Confirm'];
const TOTAL_STEPS = 3;
const { width: SCREEN_WIDTH } = Dimensions.get('window');

export default function RegisterScreen() {
  const [currentStep, setCurrentStep] = useState(1);
  const [formData, setFormData] = useState<RegisterFormData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false,
  });
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isError, setIsError] = useState(true);

  const slideAnim = useRef(new Animated.Value(0)).current;
  const fadeAnim = useRef(new Animated.Value(1)).current;

  const updateField = (
    field: keyof RegisterFormData,
    value: string | boolean,
  ): void => {
    setFormData({ ...formData, [field]: value });
    setErrors({ ...errors, [field]: undefined });
  };

  const animateTransition = (direction: 'forward' | 'back') => {
    const exitValue = direction === 'forward' ? -30 : 30;
    const enterValue = direction === 'forward' ? 30 : -30;

    Animated.parallel([
      Animated.timing(fadeAnim, {
        toValue: 0,
        duration: 120,
        useNativeDriver: true,
      }),
      Animated.timing(slideAnim, {
        toValue: exitValue,
        duration: 120,
        useNativeDriver: true,
      }),
    ]).start(() => {
      slideAnim.setValue(enterValue);
      Animated.parallel([
        Animated.timing(fadeAnim, {
          toValue: 1,
          duration: 180,
          useNativeDriver: true,
        }),
        Animated.timing(slideAnim, {
          toValue: 0,
          duration: 180,
          useNativeDriver: true,
        }),
      ]).start();
    });
  };

  const validateStep = (step: number): boolean => {
    const newErrors: ValidationErrors = {};

    if (step === 1) {
      if (!formData.firstName.trim())
        newErrors.firstName = 'First name is required';
      if (!formData.lastName.trim())
        newErrors.lastName = 'Last name is required';
      if (!formData.email.trim()) {
        newErrors.email = 'Email is required';
      } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
        newErrors.email = 'Please enter a valid email';
      }
      if (!formData.phone.trim()) {
        newErrors.phone = 'Phone number is required';
      } else if (!/^\+?[\d\s\-()]+$/.test(formData.phone)) {
        newErrors.phone = 'Please enter a valid phone number';
      }
    }

    if (step === 2) {
      if (!formData.password) {
        newErrors.password = 'Password is required';
      } else if (formData.password.length < 8) {
        newErrors.password = 'Minimum 8 characters';
      } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(formData.password)) {
        newErrors.password = 'Must include uppercase, lowercase & number';
      }
      if (!formData.confirmPassword) {
        newErrors.confirmPassword = 'Please confirm your password';
      } else if (formData.password !== formData.confirmPassword) {
        newErrors.confirmPassword = 'Passwords do not match';
      }
    }

    if (step === 3) {
      if (!formData.acceptTerms) {
        newErrors.acceptTerms = 'You must accept the terms to continue';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleNext = () => {
    if (!validateStep(currentStep)) return;

    if (currentStep < TOTAL_STEPS) {
      animateTransition('forward');
      setTimeout(() => setCurrentStep((s) => s + 1), 120);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      animateTransition('back');
      setTimeout(() => setCurrentStep((s) => s - 1), 120);
    } else {
      router.push('/');
    }
  };

  const handleRegister = async (): Promise<void> => {
    if (!validateStep(currentStep)) return;

    setLoading(true);
    setTimeout(() => {
      setLoading(false);
    }, 2000);
  };

  // Password strength indicator
  const getPasswordStrength = (): {
    level: number;
    label: string;
    color: string;
  } => {
    const p = formData.password;
    if (!p) return { level: 0, label: '', color: '' };
    let score = 0;
    if (p.length >= 8) score++;
    if (/[a-z]/.test(p) && /[A-Z]/.test(p)) score++;
    if (/\d/.test(p)) score++;
    if (/[^a-zA-Z\d]/.test(p)) score++;

    if (score <= 1)
      return { level: 1, label: 'Weak', color: 'bg-red-400' };
    if (score === 2)
      return { level: 2, label: 'Fair', color: 'bg-yellow-400' };
    if (score === 3)
      return { level: 3, label: 'Good', color: 'bg-mountain-meadow-400' };
    return { level: 4, label: 'Strong', color: 'bg-mountain-meadow-600' };
  };

  const passwordStrength = getPasswordStrength();

  const renderStep1 = () => (
    <View>
      <View className="mb-5">
        <Text className="text-xl font-bold text-gray-900">
          Personal Information
        </Text>
        <Text className="mt-1 text-sm text-gray-500">
          Tell us about yourself
        </Text>
      </View>

      <View className="flex-row gap-3">
        <View className="flex-1">
          <AppInput
            label="First Name"
            placeholder="John"
            value={formData.firstName}
            onChangeText={(text) => updateField('firstName', text)}
            error={errors.firstName}
            autoCapitalize="words"
            autoComplete="name-given"
            icon={<Ionicons name="person-outline" size={18} color="#9CA3AF" />}
          />
        </View>
        <View className="flex-1">
          <AppInput
            label="Last Name"
            placeholder="Doe"
            value={formData.lastName}
            onChangeText={(text) => updateField('lastName', text)}
            error={errors.lastName}
            autoCapitalize="words"
            autoComplete="name-family"
            icon={<Ionicons name="person-outline" size={18} color="#9CA3AF" />}
          />
        </View>
      </View>

      <AppInput
        label="Email Address"
        placeholder="john.doe@example.com"
        value={formData.email}
        onChangeText={(text) => updateField('email', text)}
        error={errors.email}
        keyboardType="email-address"
        autoCapitalize="none"
        autoComplete="email"
        icon={<Ionicons name="mail-outline" size={18} color="#9CA3AF" />}
      />

      <AppInput
        label="Phone Number"
        placeholder="+1 (555) 123-4567"
        value={formData.phone}
        onChangeText={(text) => updateField('phone', text)}
        error={errors.phone}
        keyboardType="phone-pad"
        autoComplete="tel"
        icon={<Ionicons name="call-outline" size={18} color="#9CA3AF" />}
      />
    </View>
  );

  const renderStep2 = () => (
    <View>
      <View className="mb-5">
        <Text className="text-xl font-bold text-gray-900">
          Create Password
        </Text>
        <Text className="mt-1 text-sm text-gray-500">
          Secure your account with a strong password
        </Text>
      </View>

      <AppInput
        label="Password"
        placeholder="Create a password"
        value={formData.password}
        onChangeText={(text) => updateField('password', text)}
        error={errors.password}
        secureTextEntry={!showPassword}
        autoCapitalize="none"
        autoComplete="password-new"
        icon={<Ionicons name="lock-closed-outline" size={18} color="#9CA3AF" />}
        rightIcon={
          <TouchableOpacity
            onPress={() => setShowPassword(!showPassword)}
            hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
          >
            <Ionicons
              name={showPassword ? 'eye-off-outline' : 'eye-outline'}
              size={18}
              color="#9CA3AF"
            />
          </TouchableOpacity>
        }
      />

      {/* Password strength bar */}
      {formData.password.length > 0 && (
        <View className="-mt-2 mb-4">
          <View className="mb-1.5 flex-row gap-1.5">
            {[1, 2, 3, 4].map((i) => (
              <View
                key={i}
                className={`h-1 flex-1 rounded-full ${
                  i <= passwordStrength.level
                    ? passwordStrength.color
                    : 'bg-gray-200'
                }`}
              />
            ))}
          </View>
          <Text className="text-xs text-gray-500">
            Password strength:{' '}
            <Text className="font-semibold">{passwordStrength.label}</Text>
          </Text>
        </View>
      )}

      <AppInput
        label="Confirm Password"
        placeholder="Re-enter your password"
        value={formData.confirmPassword}
        onChangeText={(text) => updateField('confirmPassword', text)}
        error={errors.confirmPassword}
        secureTextEntry={!showConfirmPassword}
        autoCapitalize="none"
        autoComplete="password-new"
        icon={<Ionicons name="lock-closed-outline" size={18} color="#9CA3AF" />}
        rightIcon={
          <TouchableOpacity
            onPress={() => setShowConfirmPassword(!showConfirmPassword)}
            hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
          >
            <Ionicons
              name={showConfirmPassword ? 'eye-off-outline' : 'eye-outline'}
              size={18}
              color="#9CA3AF"
            />
          </TouchableOpacity>
        }
      />

      {/* Requirements checklist */}
      <View className="rounded-xl border border-gray-100 bg-gray-50 p-4">
        <Text className="mb-2 text-xs font-semibold text-gray-500">
          PASSWORD REQUIREMENTS
        </Text>
        {[
          { met: formData.password.length >= 8, text: 'At least 8 characters' },
          {
            met: /[A-Z]/.test(formData.password),
            text: 'One uppercase letter',
          },
          {
            met: /[a-z]/.test(formData.password),
            text: 'One lowercase letter',
          },
          { met: /\d/.test(formData.password), text: 'One number' },
        ].map((req, i) => (
          <View key={i} className="mt-1.5 flex-row items-center">
            <Ionicons
              name={req.met ? 'checkmark-circle' : 'ellipse-outline'}
              size={16}
              color={req.met ? '#059666' : '#D1D5DB'}
            />
            <Text
              className={`ml-2 text-xs ${req.met ? 'text-mountain-meadow-700' : 'text-gray-400'}`}
            >
              {req.text}
            </Text>
          </View>
        ))}
      </View>
    </View>
  );

  const renderStep3 = () => (
    <View>
      <View className="mb-5">
        <Text className="text-xl font-bold text-gray-900">
          Review & Confirm
        </Text>
        <Text className="mt-1 text-sm text-gray-500">
          Almost done! Verify your details
        </Text>
      </View>

      {/* Summary Card */}
      <View className="mb-5 rounded-xl border border-gray-100 bg-gray-50 p-4">
        {[
          {
            icon: 'person-outline' as const,
            label: 'Name',
            value: `${formData.firstName} ${formData.lastName}`,
          },
          {
            icon: 'mail-outline' as const,
            label: 'Email',
            value: formData.email,
          },
          {
            icon: 'call-outline' as const,
            label: 'Phone',
            value: formData.phone,
          },
        ].map((item, i) => (
          <View
            key={i}
            className={`flex-row items-center py-3 ${i > 0 ? 'border-t border-gray-200' : ''}`}
          >
            <View className="mr-3 h-9 w-9 items-center justify-center rounded-lg bg-mountain-meadow-100">
              <Ionicons name={item.icon} size={18} color="#059666" />
            </View>
            <View className="flex-1">
              <Text className="text-xs text-gray-400">{item.label}</Text>
              <Text className="text-sm font-medium text-gray-900">
                {item.value}
              </Text>
            </View>
            <TouchableOpacity
              onPress={() => {
                animateTransition('back');
                setTimeout(() => setCurrentStep(1), 120);
              }}
            >
              <Ionicons name="create-outline" size={18} color="#9CA3AF" />
            </TouchableOpacity>
          </View>
        ))}
      </View>

      {/* Terms Checkbox */}
      <TouchableOpacity
        onPress={() => updateField('acceptTerms', !formData.acceptTerms)}
        className="mb-2 flex-row items-start rounded-xl border border-gray-100 bg-white p-4"
        activeOpacity={0.7}
      >
        <View
          className={`mr-3 mt-0.5 h-5 w-5 items-center justify-center rounded-md border-2 ${
            formData.acceptTerms
              ? 'border-mountain-meadow-600 bg-mountain-meadow-600'
              : errors.acceptTerms
                ? 'border-red-400'
                : 'border-gray-300'
          }`}
        >
          {formData.acceptTerms && (
            <Ionicons name="checkmark" size={14} color="#FFFFFF" />
          )}
        </View>
        <View className="flex-1">
          <Text className="text-sm leading-5 text-gray-700">
            I agree to the{' '}
            <Text className="font-semibold text-mountain-meadow-600">
              Terms of Service
            </Text>{' '}
            and{' '}
            <Text className="font-semibold text-mountain-meadow-600">
              Privacy Policy
            </Text>
          </Text>
          {errors.acceptTerms && (
            <Text className="mt-1 text-xs text-red-500">
              {errors.acceptTerms}
            </Text>
          )}
        </View>
      </TouchableOpacity>
    </View>
  );

  const renderCurrentStep = () => {
    switch (currentStep) {
      case 1:
        return renderStep1();
      case 2:
        return renderStep2();
      case 3:
        return renderStep3();
      default:
        return null;
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-white">
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        className="flex-1"
      >
        {/* Top bar */}
        <View className="flex-row items-center justify-between px-5 pb-2 pt-3">
          <TouchableOpacity
            onPress={handleBack}
            className="h-10 w-10 items-center justify-center rounded-full bg-gray-100"
            activeOpacity={0.7}
          >
            <Ionicons name="arrow-back" size={20} color="#374151" />
          </TouchableOpacity>
          <Text className="text-sm font-medium text-gray-400">
            Step {currentStep} of {TOTAL_STEPS}
          </Text>
          <View className="h-10 w-10" />
        </View>

        <ScrollView
          contentContainerStyle={{ flexGrow: 1 }}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View className="flex-1 px-6 pb-8 pt-2">
            <ProgressSteps
              currentStep={currentStep}
              totalSteps={TOTAL_STEPS}
              labels={STEP_LABELS}
            />

            {/* Error Message */}
            {isError && (
              <View className="mb-4 flex-row items-center rounded-xl border border-red-200 bg-red-50 px-4 py-3">
                <Ionicons name="alert-circle" size={20} color="#EF4444" />
                <Text className="ml-2 flex-1 text-sm font-medium text-red-600">
                  Registration failed. This email is already in use.
                </Text>
                <TouchableOpacity onPress={() => setIsError(false)} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
                  <Ionicons name="close" size={18} color="#EF4444" />
                </TouchableOpacity>
              </View>
            )}

            {/* Animated step content */}
            <Animated.View
              style={{
                opacity: fadeAnim,
                transform: [{ translateX: slideAnim }],
              }}
            >
              {renderCurrentStep()}
            </Animated.View>
          </View>
        </ScrollView>

        {/* Bottom Action Bar */}
        <View className="border-t border-gray-100 px-6 pb-4 pt-3">
          {currentStep < TOTAL_STEPS ? (
            <AppButton
              title="Continue"
              onPress={handleNext}
              variant="primary"
              icon={<Ionicons name="arrow-forward" size={18} color="#FFFFFF" />}
            />
          ) : (
            <AppButton
              title="Create Account"
              onPress={handleRegister}
              loading={loading}
              variant="primary"
              icon={
                !loading ? (
                  <Ionicons name="checkmark-circle" size={18} color="#FFFFFF" />
                ) : undefined
              }
            />
          )}

          {/* Sign In Link */}
          <View className="mt-3 flex-row items-center justify-center">
            <Text className="text-sm text-gray-500">
              Already have an account?{' '}
            </Text>
            <TouchableOpacity onPress={() => router.push('/')}>
              <Text className="text-sm font-semibold text-mountain-meadow-600">
                Sign In
              </Text>
            </TouchableOpacity>
          </View>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
