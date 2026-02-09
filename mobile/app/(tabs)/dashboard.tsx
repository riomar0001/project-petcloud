import { View, Text, ScrollView } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';

export default function DashboardScreen() {
  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View className="bg-white px-6 pb-5 pt-4">
          <Text className="text-sm text-gray-400">Welcome back</Text>
          <Text className="mt-0.5 text-2xl font-bold text-gray-900">Dashboard</Text>
        </View>

        <View className="px-6 pt-5 pb-8">
          {/* Quick Stats */}
          <View className="flex-row gap-3">
            {[
              { icon: 'paw' as const, label: 'My Pets', value: '--', color: '#059666', bg: 'bg-mountain-meadow-100' },
              { icon: 'calendar' as const, label: 'Upcoming', value: '--', color: '#3B82F6', bg: 'bg-blue-100' },
            ].map((stat, i) => (
              <View key={i} className="flex-1 rounded-2xl border border-gray-100 bg-white p-4">
                <View className={`mb-3 h-10 w-10 items-center justify-center rounded-xl ${stat.bg}`}>
                  <Ionicons name={stat.icon} size={20} color={stat.color} />
                </View>
                <Text className="text-2xl font-bold text-gray-900">{stat.value}</Text>
                <Text className="mt-0.5 text-xs text-gray-400">{stat.label}</Text>
              </View>
            ))}
          </View>

          {/* Upcoming Appointments */}
          <View className="mt-6">
            <Text className="mb-3 text-base font-semibold text-gray-900">Upcoming Appointments</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-10">
              <View className="mb-3 h-14 w-14 items-center justify-center rounded-full bg-gray-100">
                <Ionicons name="calendar-outline" size={28} color="#9CA3AF" />
              </View>
              <Text className="text-sm font-medium text-gray-500">No upcoming appointments</Text>
              <Text className="mt-1 text-xs text-gray-400">Your scheduled visits will appear here</Text>
            </View>
          </View>

          {/* Recent Activity */}
          <View className="mt-6">
            <Text className="mb-3 text-base font-semibold text-gray-900">Recent Activity</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-10">
              <View className="mb-3 h-14 w-14 items-center justify-center rounded-full bg-gray-100">
                <Ionicons name="time-outline" size={28} color="#9CA3AF" />
              </View>
              <Text className="text-sm font-medium text-gray-500">No recent activity</Text>
              <Text className="mt-1 text-xs text-gray-400">Your activity feed will appear here</Text>
            </View>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
