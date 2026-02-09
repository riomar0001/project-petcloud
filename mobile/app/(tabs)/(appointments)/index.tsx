import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

export default function AppointmentsScreen() {
  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center justify-between bg-white px-6 pb-4 pt-4">
        <View>
          <Text className="text-2xl font-bold text-gray-900">Appointments</Text>
          <Text className="mt-0.5 text-sm text-gray-400">Manage your vet visits</Text>
        </View>
        <TouchableOpacity
          onPress={() => router.push('/(tabs)/(appointments)/create')}
          className="h-10 w-10 items-center justify-center rounded-xl bg-mountain-meadow-600"
          activeOpacity={0.8}
        >
          <Ionicons name="add" size={22} color="#FFFFFF" />
        </TouchableOpacity>
      </View>

      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ flexGrow: 1 }}>
        <View className="flex-1 items-center justify-center px-6 pb-20">
          <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-50">
            <Ionicons name="calendar-outline" size={40} color="#059666" />
          </View>
          <Text className="text-lg font-semibold text-gray-900">No appointments yet</Text>
          <Text className="mt-1 text-center text-sm text-gray-400">
            Schedule your first appointment{'\n'}to get started
          </Text>
          <TouchableOpacity
            onPress={() => router.push('/(tabs)/(appointments)/create')}
            className="mt-5 flex-row items-center rounded-xl bg-mountain-meadow-600 px-6 py-3"
            activeOpacity={0.8}
          >
            <Ionicons name="add-circle-outline" size={18} color="#FFFFFF" />
            <Text className="ml-2 text-sm font-semibold text-white">Book Appointment</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
