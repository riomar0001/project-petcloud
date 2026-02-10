import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useAuthStore } from '@/store/authStore';
import { ProfileService, ApiError } from '@/api';
import { AppInput } from '@/components/ui/input';
import { AppButton } from '@/components/ui/button';
import type { ProfileResponse } from '@/api';

type ValidationErrors = Record<string, string | undefined>;

export default function ProfileScreen() {
  const { logout } = useAuthStore();

  // Profile state
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [loadingProfile, setLoadingProfile] = useState(true);

  // Profile form
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [profileErrors, setProfileErrors] = useState<ValidationErrors>({});
  const [savingProfile, setSavingProfile] = useState(false);

  // Password form
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordErrors, setPasswordErrors] = useState<ValidationErrors>({});
  const [savingPassword, setSavingPassword] = useState(false);
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // Toast
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3000);
  }, []);

  const fetchProfile = useCallback(async () => {
    try {
      setLoadingProfile(true);
      const data = await ProfileService.getProfile();
      setProfile(data);
      setFirstName(data.firstName);
      setLastName(data.lastName);
      setPhone(data.phone || '');
    } catch (error) {
      if (error instanceof ApiError) {
        showToast(error.message, false);
      }
    } finally {
      setLoadingProfile(false);
    }
  }, [showToast]);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  const handleUpdateProfile = async () => {
    const errors: ValidationErrors = {};
    if (!firstName.trim()) errors.firstName = 'First name is required';
    else if (!/^[a-zA-Z\s\-]+$/.test(firstName)) errors.firstName = 'Letters, spaces, and hyphens only';
    if (!lastName.trim()) errors.lastName = 'Last name is required';
    else if (!/^[a-zA-Z\s\-]+$/.test(lastName)) errors.lastName = 'Letters, spaces, and hyphens only';
    if (!phone.trim()) errors.phone = 'Phone is required';
    else if (!/^\d{11}$/.test(phone)) errors.phone = 'Must be exactly 11 digits';

    setProfileErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSavingProfile(true);
    try {
      const message = await ProfileService.updateProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: phone.trim(),
      });
      showToast(message, true);
      await fetchProfile();
    } catch (error: any) {
      showToast(error.message || 'Failed to update profile', false);
    } finally {
      setSavingProfile(false);
    }
  };

  const handleChangePassword = async () => {
    const errors: ValidationErrors = {};
    if (!currentPassword) errors.currentPassword = 'Current password is required';
    if (!newPassword) errors.newPassword = 'New password is required';
    else if (newPassword.length < 8) errors.newPassword = 'Must be at least 8 characters';
    if (!confirmPassword) errors.confirmPassword = 'Please confirm your password';
    else if (newPassword !== confirmPassword) errors.confirmPassword = 'Passwords do not match';

    setPasswordErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSavingPassword(true);
    try {
      const message = await ProfileService.changePassword({
        currentPassword,
        newPassword,
        confirmPassword,
      });
      showToast(message, true);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setPasswordErrors({});
    } catch (error: any) {
      showToast(error.message || 'Failed to change password', false);
    } finally {
      setSavingPassword(false);
    }
  };

  const handleLogout = () => {
    Alert.alert('Sign Out', 'Are you sure you want to sign out?', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Sign Out', style: 'destructive', onPress: logout },
    ]);
  };

  if (loadingProfile) {
    return (
      <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        className="flex-1"
      >
        <ScrollView showsVerticalScrollIndicator={false}>
          {/* Header */}
          <View className="bg-white px-6 pb-5 pt-4">
            <Text className="text-2xl font-bold text-gray-900">My Profile</Text>
            <Text className="mt-0.5 text-sm text-gray-400">Manage your account details</Text>
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
                <TouchableOpacity onPress={() => setToast(null)}>
                  <Ionicons name="close" size={18} color={toast.success ? '#16A34A' : '#EF4444'} />
                </TouchableOpacity>
              </View>
            </View>
          )}

          <View className="px-6 pt-5 pb-8">
            {/* Profile Avatar Card */}
            <View className="items-center rounded-2xl border border-gray-100 bg-white p-6">
              <View className="mb-3 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-100">
                <Ionicons name="person" size={36} color="#059666" />
              </View>
              <Text className="text-lg font-bold text-gray-900">
                {profile ? `${profile.firstName} ${profile.lastName}` : 'Loading...'}
              </Text>
              <Text className="mt-0.5 text-sm text-gray-400">{profile?.email}</Text>
            </View>

            {/* Basic Information Section */}
            <View className="mt-5">
              <View className="rounded-2xl border border-gray-100 bg-white p-5">
                <View className="mb-4 flex-row items-center">
                  <View className="mr-3 h-10 w-10 items-center justify-center rounded-xl bg-mountain-meadow-100">
                    <Ionicons name="pencil" size={18} color="#059666" />
                  </View>
                  <View>
                    <Text className="text-base font-bold text-gray-900">Basic Information</Text>
                    <Text className="text-xs text-gray-400">Update your personal details</Text>
                  </View>
                </View>

                {/* Email (read-only) */}
                <AppInput
                  label="Email"
                  value={profile?.email || ''}
                  editable={false}
                  icon={<Ionicons name="mail-outline" size={18} color="#9CA3AF" />}
                />

                <View className="flex-row gap-3">
                  <View className="flex-1">
                    <AppInput
                      label="First Name"
                      placeholder="First name"
                      value={firstName}
                      onChangeText={(text) => {
                        setFirstName(text);
                        setProfileErrors({ ...profileErrors, firstName: undefined });
                      }}
                      error={profileErrors.firstName}
                      icon={<Ionicons name="person-outline" size={18} color="#9CA3AF" />}
                    />
                  </View>
                  <View className="flex-1">
                    <AppInput
                      label="Last Name"
                      placeholder="Last name"
                      value={lastName}
                      onChangeText={(text) => {
                        setLastName(text);
                        setProfileErrors({ ...profileErrors, lastName: undefined });
                      }}
                      error={profileErrors.lastName}
                      icon={<Ionicons name="person-outline" size={18} color="#9CA3AF" />}
                    />
                  </View>
                </View>

                <AppInput
                  label="Phone"
                  placeholder="e.g. 09123456789"
                  value={phone}
                  onChangeText={(text) => {
                    const digits = text.replace(/\D/g, '').slice(0, 11);
                    setPhone(digits);
                    setProfileErrors({ ...profileErrors, phone: undefined });
                  }}
                  error={profileErrors.phone}
                  keyboardType="number-pad"
                  maxLength={11}
                  icon={<Ionicons name="call-outline" size={18} color="#9CA3AF" />}
                />

                <AppButton
                  title="Update Profile"
                  onPress={handleUpdateProfile}
                  loading={savingProfile}
                  variant="primary"
                  size="sm"
                  icon={!savingProfile ? <Ionicons name="checkmark-circle" size={16} color="#FFFFFF" /> : undefined}
                />
              </View>
            </View>

            {/* Change Password Section */}
            <View className="mt-5">
              <View className="rounded-2xl border border-gray-100 bg-white p-5">
                <View className="mb-4 flex-row items-center">
                  <View className="mr-3 h-10 w-10 items-center justify-center rounded-xl bg-amber-100">
                    <Ionicons name="shield-checkmark" size={18} color="#D97706" />
                  </View>
                  <View>
                    <Text className="text-base font-bold text-gray-900">Change Password</Text>
                    <Text className="text-xs text-gray-400">Keep your account secure</Text>
                  </View>
                </View>

                <AppInput
                  label="Current Password"
                  placeholder="Enter current password"
                  value={currentPassword}
                  onChangeText={(text) => {
                    setCurrentPassword(text);
                    setPasswordErrors({ ...passwordErrors, currentPassword: undefined });
                  }}
                  error={passwordErrors.currentPassword}
                  secureTextEntry={!showCurrentPassword}
                  autoCapitalize="none"
                  icon={<Ionicons name="lock-closed-outline" size={18} color="#9CA3AF" />}
                  rightIcon={
                    <TouchableOpacity onPress={() => setShowCurrentPassword(!showCurrentPassword)}>
                      <Ionicons name={showCurrentPassword ? 'eye-off-outline' : 'eye-outline'} size={18} color="#9CA3AF" />
                    </TouchableOpacity>
                  }
                />

                <AppInput
                  label="New Password"
                  placeholder="At least 8 characters"
                  value={newPassword}
                  onChangeText={(text) => {
                    setNewPassword(text);
                    setPasswordErrors({ ...passwordErrors, newPassword: undefined });
                  }}
                  error={passwordErrors.newPassword}
                  secureTextEntry={!showNewPassword}
                  autoCapitalize="none"
                  icon={<Ionicons name="key-outline" size={18} color="#9CA3AF" />}
                  rightIcon={
                    <TouchableOpacity onPress={() => setShowNewPassword(!showNewPassword)}>
                      <Ionicons name={showNewPassword ? 'eye-off-outline' : 'eye-outline'} size={18} color="#9CA3AF" />
                    </TouchableOpacity>
                  }
                />

                <AppInput
                  label="Confirm Password"
                  placeholder="Re-enter new password"
                  value={confirmPassword}
                  onChangeText={(text) => {
                    setConfirmPassword(text);
                    setPasswordErrors({ ...passwordErrors, confirmPassword: undefined });
                  }}
                  error={passwordErrors.confirmPassword}
                  secureTextEntry={!showConfirmPassword}
                  autoCapitalize="none"
                  icon={<Ionicons name="key-outline" size={18} color="#9CA3AF" />}
                  rightIcon={
                    <TouchableOpacity onPress={() => setShowConfirmPassword(!showConfirmPassword)}>
                      <Ionicons name={showConfirmPassword ? 'eye-off-outline' : 'eye-outline'} size={18} color="#9CA3AF" />
                    </TouchableOpacity>
                  }
                />

                <AppButton
                  title="Change Password"
                  onPress={handleChangePassword}
                  loading={savingPassword}
                  variant="primary"
                  size="sm"
                  icon={!savingPassword ? <Ionicons name="shield-checkmark" size={16} color="#FFFFFF" /> : undefined}
                />
              </View>
            </View>

            {/* Sign Out */}
            <TouchableOpacity
              onPress={handleLogout}
              className="mt-5 flex-row items-center justify-center rounded-2xl border border-red-100 bg-red-50 py-4"
              activeOpacity={0.7}
            >
              <Ionicons name="log-out-outline" size={18} color="#EF4444" />
              <Text className="ml-2 text-sm font-semibold text-red-500">Sign Out</Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
