import { View, Text, ScrollView } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';

export default function NotificationsScreen() {
  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View className="flex-row items-center justify-between bg-white px-6 pb-4 pt-4">
          <View>
            <Text className="text-2xl font-bold text-gray-900">Notifications</Text>
            <Text className="mt-0.5 text-sm text-gray-400">Stay updated with your pets</Text>
          </View>
        </View>

        <View className="px-6 pt-5 pb-8">
          {/* Empty State */}
          <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-16">
            <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-50">
              <Ionicons name="notifications-outline" size={40} color="#059666" />
            </View>
            <Text className="text-lg font-semibold text-gray-900">No notifications yet</Text>
            <Text className="mt-1 text-center text-sm text-gray-400">
              Appointment reminders and updates{'\n'}will appear here
            </Text>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
