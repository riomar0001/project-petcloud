import React, { useCallback, useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, ActivityIndicator, RefreshControl } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router, useFocusEffect } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { Image } from 'expo-image';
import { PetsService } from '@/api';
import type { PetListItem } from '@/api';
import { resolveImageUrl } from '@/utils/imageUrl';

function calcAge(birthdate: string): string {
  const birth = new Date(birthdate);
  const now = new Date();
  const months =
    (now.getFullYear() - birth.getFullYear()) * 12 +
    (now.getMonth() - birth.getMonth());
  if (months < 12) return `${months} mo`;
  const years = Math.floor(months / 12);
  return `${years} yr${years !== 1 ? 's' : ''}`;
}

export default function PetsScreen() {
  const [pets, setPets] = useState<PetListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const data = await PetsService.listPets();
      setPets(data);
    } catch {
      // silently fail
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center justify-between bg-white px-6 pb-4 pt-4">
        <View>
          <Text className="text-2xl font-bold text-gray-900">My Pets</Text>
          <Text className="mt-0.5 text-sm text-gray-400">Your furry companions</Text>
        </View>
        <TouchableOpacity
          onPress={() => router.push('/(tabs)/(pets)/add')}
          className="h-10 w-10 items-center justify-center rounded-xl bg-mountain-meadow-600"
          activeOpacity={0.8}
        >
          <Ionicons name="add" size={22} color="#FFFFFF" />
        </TouchableOpacity>
      </View>

      {loading ? (
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      ) : (
        <ScrollView
          showsVerticalScrollIndicator={false}
          contentContainerStyle={{ flexGrow: 1 }}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => {
                setRefreshing(true);
                load(true);
              }}
              tintColor="#059666"
            />
          }
        >
          {pets.length === 0 ? (
            <View className="flex-1 items-center justify-center px-6 pb-20">
              <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-50">
                <Ionicons name="paw-outline" size={40} color="#059666" />
              </View>
              <Text className="text-lg font-semibold text-gray-900">No pets added yet</Text>
              <Text className="mt-1 text-center text-sm text-gray-400">
                Add your first pet to start{'\n'}managing their health records
              </Text>
              <TouchableOpacity
                onPress={() => router.push('/(tabs)/(pets)/add')}
                className="mt-5 flex-row items-center rounded-xl bg-mountain-meadow-600 px-6 py-3"
                activeOpacity={0.8}
              >
                <Ionicons name="add-circle-outline" size={18} color="#FFFFFF" />
                <Text className="ml-2 text-sm font-semibold text-white">Add Pet</Text>
              </TouchableOpacity>
            </View>
          ) : (
            <View className="px-6 pt-5 pb-8">
              {pets.map((pet) => {
                const photoUrl = resolveImageUrl(pet.photoUrl);
                return (
                  <TouchableOpacity
                    key={pet.petId}
                    onPress={() => router.push(`/(tabs)/(pets)/${pet.petId}`)}
                    className="mb-3 flex-row items-center rounded-2xl border border-gray-100 bg-white p-4"
                    activeOpacity={0.8}
                  >
                    {photoUrl ? (
                      <Image
                        source={{ uri: photoUrl }}
                        style={{ width: 56, height: 56, borderRadius: 28 }}
                        contentFit="cover"
                        transition={200}
                      />
                    ) : (
                      <View className="h-14 w-14 items-center justify-center rounded-full bg-mountain-meadow-100">
                        <Ionicons name="paw" size={26} color="#059666" />
                      </View>
                    )}

                    <View className="ml-4 flex-1">
                      <Text className="text-base font-bold text-gray-900">{pet.name}</Text>
                      <Text className="mt-0.5 text-sm text-gray-500">
                        {pet.breed} · {pet.type}
                      </Text>
                      <Text className="mt-0.5 text-xs text-gray-400">{calcAge(pet.birthdate)} old</Text>
                    </View>

                    <Ionicons name="chevron-forward" size={18} color="#D1D5DB" />
                  </TouchableOpacity>
                );
              })}

              <TouchableOpacity
                onPress={() => router.push('/(tabs)/(pets)/add')}
                className="mt-1 flex-row items-center justify-center rounded-2xl border border-dashed border-mountain-meadow-300 bg-mountain-meadow-50 py-4"
                activeOpacity={0.8}
              >
                <Ionicons name="add-circle-outline" size={18} color="#059666" />
                <Text className="ml-2 text-sm font-semibold text-mountain-meadow-700">Add Another Pet</Text>
              </TouchableOpacity>
            </View>
          )}
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
