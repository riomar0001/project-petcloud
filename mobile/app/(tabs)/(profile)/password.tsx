import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { ProfileService } from '@/api';
import { AppInput } from '@/components/ui/input';
import { AppButton } from '@/components/ui/button';

type ValidationErrors = Record<string, string | undefined>;

export default function ChangePasswordScreen() {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [saving, setSaving] = useState(false);
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3000);
  }, []);

  const handleSave = async () => {
    const errs: ValidationErrors = {};
    if (!currentPassword) errs.currentPassword = 'Current password is required';
    if (!newPassword) errs.newPassword = 'New password is required';
    else if (newPassword.length < 8) errs.newPassword = 'Must be at least 8 characters';
    if (!confirmPassword) errs.confirmPassword = 'Please confirm your password';
    else if (newPassword !== confirmPassword) errs.confirmPassword = 'Passwords do not match';

    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setSaving(true);
    try {
      const message = await ProfileService.changePassword({
        currentPassword,
        newPassword,
        confirmPassword,
      });
      showToast(message, true);
      setTimeout(() => router.back(), 1000);
    } catch (error: any) {
      showToast(error.message || 'Failed to change password', false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        className="flex-1"
      >
        {/* Header */}
        <View className="flex-row items-center bg-white px-4 pb-4 pt-4">
          <TouchableOpacity
            onPress={() => router.back()}
            className="mr-3 h-10 w-10 items-center justify-center rounded-xl bg-gray-100"
            activeOpacity={0.7}
          >
            <Ionicons name="arrow-back" size={20} color="#374151" />
          </TouchableOpacity>
          <View>
            <Text className="text-xl font-bold text-gray-900">Change Password</Text>
            <Text className="text-xs text-gray-400">Keep your account secure</Text>
          </View>
        </View>

        {/* Toast */}
        {toast && (
          <View className="px-6 pt-3">
            <View
              className={`flex-row items-center rounded-xl px-4 py-3 ${
                toast.success
                  ? 'border border-green-200 bg-green-50'
                  : 'border border-red-200 bg-red-50'
              }`}
            >
              <Ionicons
                name={toast.success ? 'checkmark-circle' : 'alert-circle'}
                size={20}
                color={toast.success ? '#16A34A' : '#EF4444'}
              />
              <Text
                className={`ml-2 flex-1 text-sm font-medium ${
                  toast.success ? 'text-green-700' : 'text-red-600'
                }`}
              >
                {toast.message}
              </Text>
            </View>
          </View>
        )}

        <ScrollView className="flex-1 px-6 pt-5" showsVerticalScrollIndicator={false}>
          <View className="rounded-2xl border border-gray-100 bg-white p-5">
            <AppInput
              label="Current Password"
              placeholder="Enter current password"
              value={currentPassword}
              onChangeText={(text) => {
                setCurrentPassword(text);
                setErrors({ ...errors, currentPassword: undefined });
              }}
              error={errors.currentPassword}
              secureTextEntry={!showCurrent}
              autoCapitalize="none"
              icon={<Ionicons name="lock-closed-outline" size={18} color="#9CA3AF" />}
              rightIcon={
                <TouchableOpacity onPress={() => setShowCurrent(!showCurrent)}>
                  <Ionicons
                    name={showCurrent ? 'eye-off-outline' : 'eye-outline'}
                    size={18}
                    color="#9CA3AF"
                  />
                </TouchableOpacity>
              }
            />

            <AppInput
              label="New Password"
              placeholder="At least 8 characters"
              value={newPassword}
              onChangeText={(text) => {
                setNewPassword(text);
                setErrors({ ...errors, newPassword: undefined });
              }}
              error={errors.newPassword}
              secureTextEntry={!showNew}
              autoCapitalize="none"
              icon={<Ionicons name="key-outline" size={18} color="#9CA3AF" />}
              rightIcon={
                <TouchableOpacity onPress={() => setShowNew(!showNew)}>
                  <Ionicons
                    name={showNew ? 'eye-off-outline' : 'eye-outline'}
                    size={18}
                    color="#9CA3AF"
                  />
                </TouchableOpacity>
              }
            />

            <AppInput
              label="Confirm Password"
              placeholder="Re-enter new password"
              value={confirmPassword}
              onChangeText={(text) => {
                setConfirmPassword(text);
                setErrors({ ...errors, confirmPassword: undefined });
              }}
              error={errors.confirmPassword}
              secureTextEntry={!showConfirm}
              autoCapitalize="none"
              icon={<Ionicons name="key-outline" size={18} color="#9CA3AF" />}
              rightIcon={
                <TouchableOpacity onPress={() => setShowConfirm(!showConfirm)}>
                  <Ionicons
                    name={showConfirm ? 'eye-off-outline' : 'eye-outline'}
                    size={18}
                    color="#9CA3AF"
                  />
                </TouchableOpacity>
              }
            />

            <AppButton
              title="Change Password"
              onPress={handleSave}
              loading={saving}
              variant="primary"
              icon={
                !saving ? (
                  <Ionicons name="shield-checkmark" size={16} color="#FFFFFF" />
                ) : undefined
              }
            />
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
