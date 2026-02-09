import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useAuthStore } from '@/store/authStore';

export default function ProfileScreen() {
  const { logout } = useAuthStore();

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View className="bg-white px-6 pb-6 pt-4">
          <Text className="text-2xl font-bold text-gray-900">Profile</Text>
        </View>

        <View className="px-6 pt-5 pb-8">
          {/* Avatar Card */}
          <View className="items-center rounded-2xl border border-gray-100 bg-white p-6">
            <View className="mb-3 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-100">
              <Ionicons name="person" size={36} color="#059666" />
            </View>
            <Text className="text-lg font-bold text-gray-900">Pet Owner</Text>
            <Text className="mt-0.5 text-sm text-gray-400">owner@example.com</Text>
          </View>

          {/* Settings Sections */}
          <View className="mt-5">
            <Text className="mb-2 px-1 text-xs font-semibold uppercase text-gray-400">Account</Text>
            <View className="rounded-2xl border border-gray-100 bg-white">
              {[
                { icon: 'person-outline' as const, label: 'Edit Profile' },
                { icon: 'lock-closed-outline' as const, label: 'Change Password' },
                { icon: 'notifications-outline' as const, label: 'Notifications' },
              ].map((item, i) => (
                <TouchableOpacity
                  key={i}
                  className={`flex-row items-center px-4 py-3.5 ${i > 0 ? 'border-t border-gray-100' : ''}`}
                  activeOpacity={0.6}
                >
                  <View className="mr-3 h-9 w-9 items-center justify-center rounded-xl bg-gray-100">
                    <Ionicons name={item.icon} size={18} color="#6B7280" />
                  </View>
                  <Text className="flex-1 text-sm font-medium text-gray-700">{item.label}</Text>
                  <Ionicons name="chevron-forward" size={16} color="#D1D5DB" />
                </TouchableOpacity>
              ))}
            </View>
          </View>

          <View className="mt-5">
            <Text className="mb-2 px-1 text-xs font-semibold uppercase text-gray-400">Support</Text>
            <View className="rounded-2xl border border-gray-100 bg-white">
              {[
                { icon: 'help-circle-outline' as const, label: 'Help & Support' },
                { icon: 'document-text-outline' as const, label: 'Terms of Service' },
                { icon: 'shield-checkmark-outline' as const, label: 'Privacy Policy' },
              ].map((item, i) => (
                <TouchableOpacity
                  key={i}
                  className={`flex-row items-center px-4 py-3.5 ${i > 0 ? 'border-t border-gray-100' : ''}`}
                  activeOpacity={0.6}
                >
                  <View className="mr-3 h-9 w-9 items-center justify-center rounded-xl bg-gray-100">
                    <Ionicons name={item.icon} size={18} color="#6B7280" />
                  </View>
                  <Text className="flex-1 text-sm font-medium text-gray-700">{item.label}</Text>
                  <Ionicons name="chevron-forward" size={16} color="#D1D5DB" />
                </TouchableOpacity>
              ))}
            </View>
          </View>

          {/* Logout */}
          <TouchableOpacity
            onPress={logout}
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
