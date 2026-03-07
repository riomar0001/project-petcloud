import React, { useCallback, useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Switch,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { Image } from 'expo-image';
import { router, useFocusEffect } from 'expo-router';
import { useAuthStore } from '@/store/authStore';
import { useProfileStore } from '@/store/useProfileStore';
import { resolveImageUrl } from '@/utils/imageUrl';
import { Alert } from 'react-native';

export default function ProfileViewScreen() {
  const { logout } = useAuthStore();
  const { profile, isLoading, fetchProfile } = useProfileStore();
  const [notificationsEnabled, setNotificationsEnabled] = useState(true);

  useFocusEffect(
    useCallback(() => {
      fetchProfile();
    }, [fetchProfile])
  );

  const handleLogout = () => {
    Alert.alert('Sign Out', 'Are you sure you want to sign out?', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Sign Out', style: 'destructive', onPress: logout },
    ]);
  };

  const photoUrl = resolveImageUrl(profile?.profileImageUrl);

  if (isLoading && !profile) {
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
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View className="bg-white px-6 pb-5 pt-4">
          <Text className="text-2xl font-bold text-gray-900">My Profile</Text>
          <Text className="mt-0.5 text-sm text-gray-400">Manage your account details</Text>
        </View>

        <View className="px-6 pt-5 pb-8">
          {/* Profile Avatar Card */}
          <View className="items-center rounded-2xl border border-gray-100 bg-white p-6">
            <TouchableOpacity
              onPress={() => router.push('/(tabs)/(profile)/photo')}
              activeOpacity={0.7}
            >
              {photoUrl ? (
                <Image
                  source={{ uri: photoUrl }}
                  style={{ width: 80, height: 80, borderRadius: 40 }}
                  contentFit="cover"
                  transition={200}
                />
              ) : (
                <View className="h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-100">
                  <Ionicons name="person" size={36} color="#059666" />
                </View>
              )}
              <View className="absolute -bottom-0.5 -right-0.5 h-7 w-7 items-center justify-center rounded-full border-2 border-white bg-mountain-meadow-600">
                <Ionicons name="camera" size={13} color="#FFFFFF" />
              </View>
            </TouchableOpacity>
            <Text className="mt-2 text-lg font-bold text-gray-900">
              {profile ? `${profile.firstName} ${profile.lastName}` : '—'}
            </Text>
            <Text className="mt-0.5 text-sm text-gray-400">{profile?.email}</Text>
          </View>

          {/* Info Card */}
          <View className="mt-5 rounded-2xl border border-gray-100 bg-white p-5">
            <View className="mb-4 flex-row items-center justify-between">
              <View className="flex-row items-center">
                <View className="mr-3 h-10 w-10 items-center justify-center rounded-xl bg-mountain-meadow-100">
                  <Ionicons name="person-circle-outline" size={20} color="#059666" />
                </View>
                <Text className="text-base font-bold text-gray-900">Personal Information</Text>
              </View>
              <TouchableOpacity
                onPress={() => router.push('/(tabs)/(profile)/edit')}
                className="flex-row items-center rounded-lg bg-mountain-meadow-50 px-3 py-1.5"
                activeOpacity={0.7}
              >
                <Ionicons name="pencil" size={14} color="#059666" />
                <Text className="ml-1 text-xs font-semibold text-mountain-meadow-700">Edit</Text>
              </TouchableOpacity>
            </View>

            <InfoRow icon="person-outline" label="First Name" value={profile?.firstName} />
            <InfoRow icon="person-outline" label="Last Name" value={profile?.lastName} />
            <InfoRow icon="mail-outline" label="Email" value={profile?.email} />
            <InfoRow icon="call-outline" label="Phone" value={profile?.phone || 'Not set'} last />
          </View>

          {/* Settings */}
          <View className="mt-5 rounded-2xl border border-gray-100 bg-white">
            <SettingsToggleRow
              icon="notifications-outline"
              iconBg="bg-purple-100"
              iconColor="#A855F7"
              label="Notifications"
              value={notificationsEnabled}
              onValueChange={setNotificationsEnabled}
            />
            <View className="mx-5 h-px bg-gray-100" />
            <View className="mx-5 h-px bg-gray-100" />
            <SettingsRow
              icon="key-outline"
              iconBg="bg-amber-100"
              iconColor="#D97706"
              label="Change Password"
              onPress={() => router.push('/(tabs)/(profile)/password')}
            />
            <View className="mx-5 h-px bg-gray-100" />
            <SettingsRow
              icon="camera-outline"
              iconBg="bg-blue-100"
              iconColor="#3B82F6"
              label="Update Photo"
              onPress={() => router.push('/(tabs)/(profile)/photo')}
            />
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
    </SafeAreaView>
  );
}

function InfoRow({
  icon,
  label,
  value,
  last,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  value?: string | null;
  last?: boolean;
}) {
  return (
    <View className={`flex-row items-center py-3 ${last ? '' : 'border-b border-gray-50'}`}>
      <Ionicons name={icon} size={18} color="#9CA3AF" />
      <Text className="ml-3 w-24 text-sm text-gray-400">{label}</Text>
      <Text className="flex-1 text-sm font-medium text-gray-900">{value || '—'}</Text>
    </View>
  );
}

function SettingsRow({
  icon,
  iconBg,
  iconColor,
  label,
  onPress,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  iconBg: string;
  iconColor: string;
  label: string;
  onPress: () => void;
}) {
  return (
    <TouchableOpacity
      onPress={onPress}
      className="flex-row items-center px-5 py-4"
      activeOpacity={0.6}
    >
      <View className={`mr-3 h-10 w-10 items-center justify-center rounded-xl ${iconBg}`}>
        <Ionicons name={icon} size={18} color={iconColor} />
      </View>
      <Text className="flex-1 text-sm font-semibold text-gray-900">{label}</Text>
      <Ionicons name="chevron-forward" size={18} color="#9CA3AF" />
    </TouchableOpacity>
  );
}

function SettingsToggleRow({
  icon,
  iconBg,
  iconColor,
  label,
  value,
  onValueChange,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  iconBg: string;
  iconColor: string;
  label: string;
  value: boolean;
  onValueChange: (val: boolean) => void;
}) {
  return (
    <View className="flex-row items-center px-5 py-4">
      <View className={`mr-3 h-10 w-10 items-center justify-center rounded-xl ${iconBg}`}>
        <Ionicons name={icon} size={18} color={iconColor} />
      </View>
      <Text className="flex-1 text-sm font-semibold text-gray-900">{label}</Text>
      <Switch
        value={value}
        onValueChange={onValueChange}
        trackColor={{ false: '#E5E7EB', true: '#34D399' }}
        thumbColor={value ? '#059666' : '#f4f3f4'}
      />
    </View>
  );
}
