import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

export default function UpdatePetScreen() {
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
          <Text className="text-xl font-bold text-gray-900">Add Pet</Text>
          <Text className="text-xs text-gray-400">Register a new companion</Text>
        </View>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-5 pb-8">
          {/* Photo Upload */}
          <View className="mb-5 items-center">
            <View className="h-24 w-24 items-center justify-center rounded-full border-2 border-dashed border-gray-300 bg-gray-50">
              <Ionicons name="camera-outline" size={32} color="#9CA3AF" />
            </View>
            <Text className="mt-2 text-xs text-gray-400">Tap to add photo</Text>
          </View>

          {/* Pet Name */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Pet Name</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-6">
              <Text className="text-sm text-gray-400">Name input will go here</Text>
            </View>
          </View>

          {/* Species & Breed */}
          <View className="mb-4 flex-row gap-3">
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Species</Text>
              <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-6">
                <Text className="text-sm text-gray-400">Species picker</Text>
              </View>
            </View>
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Breed</Text>
              <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-6">
                <Text className="text-sm text-gray-400">Breed picker</Text>
              </View>
            </View>
          </View>

          {/* Gender & Birthday */}
          <View className="mb-4 flex-row gap-3">
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Gender</Text>
              <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-6">
                <Text className="text-sm text-gray-400">Gender select</Text>
              </View>
            </View>
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Birthday</Text>
              <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-6">
                <Text className="text-sm text-gray-400">Date picker</Text>
              </View>
            </View>
          </View>

          {/* Weight */}
          <View className="mb-6">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Weight (kg)</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-6">
              <Text className="text-sm text-gray-400">Weight input will go here</Text>
            </View>
          </View>

          {/* Submit Button Placeholder */}
          <View className="rounded-2xl bg-gray-200 py-4">
            <Text className="text-center text-sm font-semibold text-gray-400">Add Pet</Text>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
