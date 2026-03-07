import React, { useCallback, useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, ActivityIndicator } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router, useLocalSearchParams, useFocusEffect } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { Image } from 'expo-image';
import { PetsService } from '@/api';
import type { PetDetail, PetCardRecord } from '@/api';
import { resolveImageUrl } from '@/utils/imageUrl';

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function calcAge(birthdate: string): string {
  const birth = new Date(birthdate);
  const now = new Date();
  const months =
    (now.getFullYear() - birth.getFullYear()) * 12 +
    (now.getMonth() - birth.getMonth());
  if (months < 12) return `${months} mo`;
  return `${Math.floor(months / 12)} yr${Math.floor(months / 12) !== 1 ? 's' : ''}`;
}

export default function PetDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const petId = Number(id);

  const [pet, setPet] = useState<PetDetail | null>(null);
  const [records, setRecords] = useState<PetCardRecord[]>([]);
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      (async () => {
        setLoading(true);
        try {
          const [detail, card] = await Promise.all([
            PetsService.getPetDetail(petId),
            PetsService.getPetCard(petId, { pageSize: 10 }),
          ]);
          if (active) {
            setPet(detail);
            setRecords(card.records);
          }
        } catch {
          // silently fail
        } finally {
          if (active) setLoading(false);
        }
      })();
      return () => { active = false; };
    }, [petId])
  );

  if (loading) {
    return (
      <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
        <View className="flex-row items-center bg-white px-5 pb-4 pt-3">
          <TouchableOpacity
            onPress={() => router.back()}
            className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-gray-100"
            activeOpacity={0.7}
          >
            <Ionicons name="arrow-back" size={20} color="#374151" />
          </TouchableOpacity>
          <Text className="text-xl font-bold text-gray-900">Pet Profile</Text>
        </View>
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      </SafeAreaView>
    );
  }

  const photoUrl = resolveImageUrl(pet?.photoUrl);

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
          <Text className="text-xl font-bold text-gray-900">{pet?.name ?? 'Pet Profile'}</Text>
          <Text className="text-xs text-gray-400">{pet?.type} · {pet?.breed}</Text>
        </View>
        <TouchableOpacity
          onPress={() =>
            router.push({
              pathname: '/(tabs)/(pets)/update',
              params: { petId: id },
            })
          }
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
            {photoUrl ? (
              <Image
                source={{ uri: photoUrl }}
                style={{ width: 80, height: 80, borderRadius: 40 }}
                contentFit="cover"
                transition={200}
              />
            ) : (
              <View className="mb-3 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-100">
                <Ionicons name="paw" size={36} color="#059666" />
              </View>
            )}
            <Text className="mt-3 text-xl font-bold text-gray-900">{pet?.name}</Text>
            <Text className="mt-0.5 text-sm text-gray-400">
              {pet?.breed} · {calcAge(pet?.birthdate ?? '')} old
            </Text>
          </View>

          {/* Info Cards */}
          <View className="mb-5 flex-row gap-3">
            {[
              { icon: 'paw' as const, label: 'Type', value: pet?.type ?? '—' },
              { icon: 'gift' as const, label: 'Age', value: calcAge(pet?.birthdate ?? '') },
              { icon: 'calendar' as const, label: 'Born', value: pet?.birthdate ? formatDate(pet.birthdate) : '—' },
            ].map((item, i) => (
              <View key={i} className="flex-1 items-center rounded-2xl border border-gray-100 bg-white p-3">
                <Ionicons name={item.icon} size={18} color="#059666" />
                <Text className="mt-1.5 text-center text-sm font-bold text-gray-900">{item.value}</Text>
                <Text className="text-xs text-gray-400">{item.label}</Text>
              </View>
            ))}
          </View>

          {/* Details */}
          <View className="rounded-2xl border border-gray-100 bg-white">
            {[
              { icon: 'bookmark' as const, label: 'Breed', value: pet?.breed ?? '—' },
              { icon: 'person-outline' as const, label: 'Owner', value: pet?.ownerName ?? '—' },
            ].map((item, i) => (
              <View
                key={i}
                className={`flex-row items-center px-4 py-3.5 ${i > 0 ? 'border-t border-gray-100' : ''}`}
              >
                <View className="mr-3 h-9 w-9 items-center justify-center rounded-xl bg-gray-100">
                  <Ionicons name={item.icon} size={18} color="#6B7280" />
                </View>
                <View className="flex-1">
                  <Text className="text-xs text-gray-400">{item.label}</Text>
                  <Text className="mt-0.5 text-sm font-medium text-gray-700">{item.value}</Text>
                </View>
              </View>
            ))}
          </View>

          {/* Health Records */}
          <View className="mt-5">
            <Text className="mb-3 text-base font-semibold text-gray-900">Health Records</Text>
            {records.length > 0 ? (
              records.map((rec) => (
                <View
                  key={rec.appointmentId}
                  className="mb-3 rounded-2xl border border-gray-100 bg-white p-4"
                >
                  <View className="mb-2 flex-row items-center justify-between">
                    <Text className="text-sm font-semibold text-gray-900">
                      {rec.serviceType ?? 'Visit'}
                    </Text>
                    <Text className="text-xs text-gray-400">{formatDate(rec.appointmentDate)}</Text>
                  </View>
                  {rec.serviceSubtype && (
                    <Text className="mb-1 text-xs text-gray-500">{rec.serviceSubtype}</Text>
                  )}
                  {rec.administeredBy && (
                    <Text className="text-xs text-gray-400">By {rec.administeredBy}</Text>
                  )}
                  {rec.notes && (
                    <Text className="mt-1 text-xs text-gray-500">{rec.notes}</Text>
                  )}
                  {rec.dueDate && (
                    <View className="mt-2 flex-row items-center">
                      <Ionicons name="alarm-outline" size={12} color="#D97706" />
                      <Text className="ml-1 text-xs font-medium text-amber-600">
                        Next due: {formatDate(rec.dueDate)}
                      </Text>
                    </View>
                  )}
                </View>
              ))
            ) : (
              <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-8">
                <Ionicons name="document-text-outline" size={28} color="#9CA3AF" />
                <Text className="mt-2 text-sm text-gray-400">No health records yet</Text>
              </View>
            )}
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
