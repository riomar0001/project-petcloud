import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

export default function CreateAppointmentScreen() {
  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center bg-white px-5 pb-4 pt-3">
        <TouchableOpacity
          onPress={() => router.back()}
          className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-gray-100"
          activeOpacity={0.7}
        >
          <Ionicons name="arrow-back" size={20} color="#374151" />
        </TouchableOpacity>
        <View className="flex-1">
          <Text className="text-xl font-bold text-gray-900">Book Appointment</Text>
          <Text className="text-xs text-gray-400">Schedule a new visit</Text>
        </View>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-5 pb-8">
          {/* Select Pet */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Select Pet</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-8">
              <Ionicons name="paw-outline" size={28} color="#9CA3AF" />
              <Text className="mt-2 text-sm text-gray-400">Pet selector will go here</Text>
            </View>
          </View>

          {/* Select Service */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Service Type</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-8">
              <Ionicons name="medkit-outline" size={28} color="#9CA3AF" />
              <Text className="mt-2 text-sm text-gray-400">Service picker will go here</Text>
            </View>
          </View>

          {/* Date & Time */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Date & Time</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-8">
              <Ionicons name="calendar-outline" size={28} color="#9CA3AF" />
              <Text className="mt-2 text-sm text-gray-400">Date/time picker will go here</Text>
            </View>
          </View>

          {/* Notes */}
          <View className="mb-6">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Notes</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-8">
              <Ionicons name="create-outline" size={28} color="#9CA3AF" />
              <Text className="mt-2 text-sm text-gray-400">Notes input will go here</Text>
            </View>
          </View>

          {/* Submit Button Placeholder */}
          <View className="rounded-2xl bg-gray-200 py-4">
            <Text className="text-center text-sm font-semibold text-gray-400">Confirm Booking</Text>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
