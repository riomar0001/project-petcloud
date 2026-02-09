import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router, useLocalSearchParams } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

export default function AppointmentDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();

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
          <Text className="text-xl font-bold text-gray-900">Appointment Details</Text>
          <Text className="text-xs text-gray-400">ID: {id}</Text>
        </View>
        <TouchableOpacity
          className="h-10 w-10 items-center justify-center rounded-full bg-gray-100"
          activeOpacity={0.7}
        >
          <Ionicons name="ellipsis-vertical" size={18} color="#374151" />
        </TouchableOpacity>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-5 pb-8">
          {/* Status Badge */}
          <View className="mb-5 items-center rounded-2xl border border-gray-100 bg-white p-5">
            <View className="mb-3 h-14 w-14 items-center justify-center rounded-full bg-mountain-meadow-100">
              <Ionicons name="calendar" size={28} color="#059666" />
            </View>
            <View className="rounded-full bg-yellow-100 px-3 py-1">
              <Text className="text-xs font-semibold text-yellow-700">Placeholder Status</Text>
            </View>
          </View>

          {/* Details Sections */}
          {[
            { icon: 'paw' as const, label: 'Pet', value: 'Pet details will load here' },
            { icon: 'medkit' as const, label: 'Service', value: 'Service type will load here' },
            { icon: 'calendar' as const, label: 'Date & Time', value: 'Scheduled date will load here' },
            { icon: 'person' as const, label: 'Veterinarian', value: 'Assigned vet will load here' },
            { icon: 'chatbubble' as const, label: 'Notes', value: 'Appointment notes will load here' },
          ].map((item, i) => (
            <View key={i} className={`flex-row items-start py-4 ${i > 0 ? 'border-t border-gray-100' : ''}`}>
              <View className="mr-3 h-9 w-9 items-center justify-center rounded-xl bg-gray-100">
                <Ionicons name={item.icon} size={18} color="#6B7280" />
              </View>
              <View className="flex-1">
                <Text className="text-xs text-gray-400">{item.label}</Text>
                <Text className="mt-0.5 text-sm font-medium text-gray-600">{item.value}</Text>
              </View>
            </View>
          ))}

          {/* Action Buttons */}
          <View className="mt-4 gap-3">
            <View className="rounded-2xl bg-gray-200 py-4">
              <Text className="text-center text-sm font-semibold text-gray-400">Update Appointment</Text>
            </View>
            <View className="rounded-2xl border border-red-200 bg-red-50 py-4">
              <Text className="text-center text-sm font-semibold text-red-400">Cancel Appointment</Text>
            </View>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
