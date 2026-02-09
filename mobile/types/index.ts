// types.ts - Shared TypeScript interfaces and types

import { ReactNode } from 'react';
import { TextInputProps, ViewStyle } from 'react-native';

// Component Props Types
export interface AppInputProps extends TextInputProps {
  label: string;
  error?: string;
  icon?: ReactNode;
  rightIcon?: ReactNode;
  containerStyle?: ViewStyle;
}

export interface AppButtonProps {
  title: string;
  onPress: () => void;
  variant?: 'primary' | 'outline' | 'ghost' | 'secondary';
  loading?: boolean;
  disabled?: boolean;
  icon?: ReactNode;
  containerStyle?: ViewStyle;
  size?: 'sm' | 'md' | 'lg';
}

// Form Data Types
export interface LoginFormData {
  email: string;
  password: string;
}

export interface RegisterFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

export interface ForgotPasswordFormData {
  email: string;
}

export interface TwoFactorFormData {
  code: string;
}

// Validation Error Types
export interface ValidationErrors {
  [key: string]: string | undefined;
}

// Navigation Types (for React Navigation if used)
export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
  ForgotPassword: undefined;
  TwoFactor: { email: string };
};
