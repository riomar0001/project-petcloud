import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { ProfileService, ApiError } from '@/api';
import { AppInput } from '@/components/ui/input';
import { AppButton } from '@/components/ui/button';

type ValidationErrors = Record<string, string | undefined>;

export default function EditProfileScreen() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3000);
  }, []);

  useEffect(() => {
    (async () => {
      try {
        const data = await ProfileService.getProfile();
        setFirstName(data.firstName);
        setLastName(data.lastName);
        setPhone(data.phone || '');
        setEmail(data.email);
      } catch (error) {
        if (error instanceof ApiError) {
          showToast(error.message, false);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [showToast]);

  const handleSave = async () => {
    const errs: ValidationErrors = {};
    if (!firstName.trim()) errs.firstName = 'First name is required';
    else if (!/^[a-zA-Z\s\-]+$/.test(firstName)) errs.firstName = 'Letters, spaces, and hyphens only';
    if (!lastName.trim()) errs.lastName = 'Last name is required';
    else if (!/^[a-zA-Z\s\-]+$/.test(lastName)) errs.lastName = 'Letters, spaces, and hyphens only';
    if (!phone.trim()) errs.phone = 'Phone is required';
    else if (!/^\d{11}$/.test(phone)) errs.phone = 'Must be exactly 11 digits';

    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setSaving(true);
    try {
      const message = await ProfileService.updateProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: phone.trim(),
      });
      showToast(message, true);
      setTimeout(() => router.back(), 1000);
    } catch (error: any) {
      showToast(error.message || 'Failed to update profile', false);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
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
            <Text className="text-xl font-bold text-gray-900">Edit Profile</Text>
            <Text className="text-xs text-gray-400">Update your personal details</Text>
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
            {/* Email (read-only) */}
            <AppInput
              label="Email"
              value={email}
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
                    setErrors({ ...errors, firstName: undefined });
                  }}
                  error={errors.firstName}
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
                    setErrors({ ...errors, lastName: undefined });
                  }}
                  error={errors.lastName}
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
                setErrors({ ...errors, phone: undefined });
              }}
              error={errors.phone}
              keyboardType="number-pad"
              maxLength={11}
              icon={<Ionicons name="call-outline" size={18} color="#9CA3AF" />}
            />

            <AppButton
              title="Save Changes"
              onPress={handleSave}
              loading={saving}
              variant="primary"
              icon={
                !saving ? (
                  <Ionicons name="checkmark-circle" size={16} color="#FFFFFF" />
                ) : undefined
              }
            />
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
