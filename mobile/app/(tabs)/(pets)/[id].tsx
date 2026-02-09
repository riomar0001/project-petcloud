import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router, useLocalSearchParams } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

export default function PetDetailScreen() {
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
          <Text className="text-xl font-bold text-gray-900">Pet Profile</Text>
          <Text className="text-xs text-gray-400">ID: {id}</Text>
        </View>
        <TouchableOpacity
          className="h-10 w-10 items-center justify-center rounded-full bg-gray-100"
          activeOpacity={0.7}
        >
          <Ionicons name="create-outline" size={18} color="#374151" />
        </TouchableOpacity>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-5 pb-8">
          {/* Pet Avatar & Name */}
          <View className="mb-5 items-center rounded-2xl border border-gray-100 bg-white p-6">
            <View className="mb-3 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-100">
              <Ionicons name="paw" size={36} color="#059666" />
            </View>
            <Text className="text-xl font-bold text-gray-900">Pet Name</Text>
            <Text className="mt-0.5 text-sm text-gray-400">Pet details will load here</Text>
          </View>

          {/* Info Cards */}
          <View className="mb-5 flex-row gap-3">
            {[
              { icon: 'male-female' as const, label: 'Gender', value: '--' },
              { icon: 'fitness' as const, label: 'Weight', value: '-- kg' },
              { icon: 'gift' as const, label: 'Age', value: '--' },
            ].map((item, i) => (
              <View key={i} className="flex-1 items-center rounded-2xl border border-gray-100 bg-white p-3">
                <Ionicons name={item.icon} size={18} color="#059666" />
                <Text className="mt-1.5 text-base font-bold text-gray-900">{item.value}</Text>
                <Text className="text-xs text-gray-400">{item.label}</Text>
              </View>
            ))}
          </View>

          {/* Details */}
          <View className="rounded-2xl border border-gray-100 bg-white">
            {[
              { icon: 'paw' as const, label: 'Species', value: 'Will load here' },
              { icon: 'bookmark' as const, label: 'Breed', value: 'Will load here' },
              { icon: 'calendar' as const, label: 'Birthday', value: 'Will load here' },
              { icon: 'color-palette' as const, label: 'Color', value: 'Will load here' },
            ].map((item, i) => (
              <View key={i} className={`flex-row items-center px-4 py-3.5 ${i > 0 ? 'border-t border-gray-100' : ''}`}>
                <View className="mr-3 h-9 w-9 items-center justify-center rounded-xl bg-gray-100">
                  <Ionicons name={item.icon} size={18} color="#6B7280" />
                </View>
                <View className="flex-1">
                  <Text className="text-xs text-gray-400">{item.label}</Text>
                  <Text className="mt-0.5 text-sm font-medium text-gray-600">{item.value}</Text>
                </View>
              </View>
            ))}
          </View>

          {/* Health Records */}
          <View className="mt-5">
            <Text className="mb-3 text-base font-semibold text-gray-900">Health Records</Text>
            <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-8">
              <Ionicons name="document-text-outline" size={28} color="#9CA3AF" />
              <Text className="mt-2 text-sm text-gray-400">Health records will appear here</Text>
            </View>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
