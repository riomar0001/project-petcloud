import React, { useCallback, useState } from 'react';
import { Image } from 'expo-image';
import { useFocusEffect, router } from 'expo-router';
import { View, Text, ScrollView, TouchableOpacity, ActivityIndicator } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { resolveImageUrl } from '@/utils/imageUrl';
import { DashboardService } from '@/api';
import type { DashboardResponse } from '@/api';
import { useNotificationStore } from '@/store/useNotificationStore';
import { useProfileStore } from '@/store/useProfileStore';

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function calcAge(birthdate: string): string {
  const birth = new Date(birthdate);
  const now = new Date();
  const years = now.getFullYear() - birth.getFullYear();
  const months = (now.getFullYear() - birth.getFullYear()) * 12 + (now.getMonth() - birth.getMonth());
  if (months < 12) return `${months}mo`;
  return `${years}y`;
}

export default function DashboardScreen() {
  const { profile, fetchProfile } = useProfileStore();
  const { unreadCount } = useNotificationStore();
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useFocusEffect(
    useCallback(() => {
      let active = true;
      (async () => {
        setLoading(true);
        try {
          const [dash] = await Promise.all([DashboardService.getDashboard(), profile === null ? fetchProfile() : Promise.resolve()]);
          if (active) setData(dash);
        } catch {
          // silently fail
        } finally {
          if (active) setLoading(false);
        }
      })();
      return () => {
        active = false;
      };
    }, [])
  );

  const firstName = profile?.firstName ?? data?.userName ?? '';
  const photoUrl = resolveImageUrl(profile?.profileImageUrl);

  const hour = new Date().getHours();
  const greeting = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';

  const stats = [
    {
      icon: 'paw' as const,
      label: 'My Pets',
      value: data?.pets.length ?? 0,
      color: '#A855F7',
      bg: 'bg-purple-100'
    },
    {
      icon: 'calendar' as const,
      label: 'Upcoming Appts',
      value: data?.upcomingAppointments.length ?? 0,
      color: '#059666',
      bg: 'bg-mountain-meadow-100'
    },
    {
      icon: 'medical' as const,
      label: 'Vaccine Due',
      value: data?.vaccineDue.length ?? 0,
      color: '#3B82F6',
      bg: 'bg-blue-100'
    },
    {
      icon: 'medkit' as const,
      label: 'Deworm Due',
      value: data?.dewormDue.length ?? 0,
      color: '#6B7280',
      bg: 'bg-gray-200'
    }
  ];

  return (
    <SafeAreaView className="flex-col bg-gray-100" edges={['top']}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View className="mx-6 mt-5 flex-row items-center rounded-2xl bg-white px-6 pb-5 pt-4 shadow-sm">
          {/* Avatar */}
          <TouchableOpacity onPress={() => router.push('/(tabs)/(profile)')} activeOpacity={0.8}>
            {photoUrl ? (
              <Image source={{ uri: photoUrl }} style={{ width: 48, height: 48, borderRadius: 24 }} contentFit="cover" transition={200} />
            ) : (
              <View className="h-12 w-12 items-center justify-center rounded-full bg-mountain-meadow-100">
                <Ionicons name="person" size={24} color="#059666" />
              </View>
            )}
          </TouchableOpacity>

          {/* Greeting — next to avatar */}
          <View className="ml-3 flex-1">
            <Text className="text-2xl font-bold text-gray-900">
              {greeting}, {firstName || 'there'}!
            </Text>
            <Text className="text-md text-gray-400">Welcome to PurrVet</Text>
          </View>

          {/* Notification Bell */}
          <TouchableOpacity
            onPress={() => router.push('/(tabs)/notifications')}
            activeOpacity={0.7}
            className="h-11 w-11 items-center justify-center rounded-full bg-gray-100"
          >
            <Ionicons name="notifications-outline" size={22} color="#374151" />
            {unreadCount > 0 && <View className="absolute right-1.5 top-1.5 h-2.5 w-2.5 rounded-full border border-white bg-red-500" />}
          </TouchableOpacity>
        </View>

        <View className="px-6 pb-8 pt-5">
          {/* Quick Stats */}
          {loading ? (
            <View className="items-center py-8">
              <ActivityIndicator size="large" color="#059666" />
            </View>
          ) : (
            <>
              <View className="flex-row flex-wrap justify-between gap-y-3">
                {stats.map((stat, i) => (
                  <View key={i} className="w-[48%] rounded-xl border border-gray-100 bg-white p-4 shadow-sm">
                    <View className={`mb-3 h-10 w-10 items-center justify-center rounded-xl ${stat.bg}`}>
                      <Ionicons name={stat.icon} size={20} color={stat.color} />
                    </View>
                    <Text className="text-2xl font-bold text-gray-900">{stat.value}</Text>
                    <Text className="mt-0.5 text-xs text-gray-400">{stat.label}</Text>
                  </View>
                ))}
              </View>

              {/* My Pets */}
              {data && data.pets.length > 0 && (
                <View className="mt-6">
                  <View className="mb-3 flex-row items-center justify-between">
                    <Text className="text-base font-semibold text-gray-900">My Pets</Text>
                    <TouchableOpacity onPress={() => router.push('/(tabs)/(pets)')}>
                      <Text className="text-xs font-semibold text-mountain-meadow-600">See all</Text>
                    </TouchableOpacity>
                  </View>
                  <ScrollView horizontal showsHorizontalScrollIndicator={false} className="-mx-1">
                    {data.pets.map((pet) => {
                      const petPhoto = resolveImageUrl(pet.photoUrl);
                      return (
                        <TouchableOpacity
                          key={pet.petId}
                          onPress={() => router.push(`/(tabs)/(pets)/${pet.petId}`)}
                          className="mx-1 w-24 items-center rounded-2xl border border-gray-100 bg-white p-3"
                          activeOpacity={0.8}
                        >
                          {petPhoto ? (
                            <Image source={{ uri: petPhoto }} style={{ width: 48, height: 48, borderRadius: 24 }} contentFit="cover" transition={200} />
                          ) : (
                            <View className="h-12 w-12 items-center justify-center rounded-full bg-mountain-meadow-100">
                              <Ionicons name="paw" size={22} color="#059666" />
                            </View>
                          )}
                          <Text className="mt-2 text-center text-xs font-semibold text-gray-900" numberOfLines={1}>
                            {pet.name}
                          </Text>
                          <Text className="text-center text-xs text-gray-400">{calcAge(pet.birthdate)}</Text>
                        </TouchableOpacity>
                      );
                    })}
                  </ScrollView>
                </View>
              )}

              {/* Upcoming Appointments */}
              <View className="mt-6">
                <View className="mb-3 flex-row items-center justify-between">
                  <Text className="text-base font-semibold text-gray-900">Upcoming Appointments</Text>
                  <TouchableOpacity onPress={() => router.push('/(tabs)/(appointments)')}>
                    <Text className="text-xs font-semibold text-mountain-meadow-600">See all</Text>
                  </TouchableOpacity>
                </View>
                {data && data.upcomingAppointments.length > 0 ? (
                  data.upcomingAppointments.slice(0, 3).map((apt) => (
                    <TouchableOpacity
                      key={apt.appointmentId}
                      onPress={() =>
                        router.push({
                          pathname: '/(tabs)/(appointments)/[id]',
                          params: {
                            id: apt.appointmentId.toString(),
                            petName: apt.petName,
                            serviceType: apt.serviceType ?? '',
                            appointmentDate: apt.appointmentDate,
                            status: apt.status,
                            notes: ''
                          }
                        })
                      }
                      className="mb-3 flex-row items-center rounded-xl border border-gray-100 bg-white p-4"
                      activeOpacity={0.8}
                    >
                      <View className="mr-3 h-10 w-10 items-center justify-center rounded-xl bg-mountain-meadow-100">
                        <Ionicons name="calendar" size={20} color="#059666" />
                      </View>
                      <View className="flex-1">
                        <Text className="text-sm font-semibold text-gray-900">{apt.petName}</Text>
                        <Text className="text-xs text-gray-400">
                          {apt.serviceType ?? 'Appointment'} · {formatDate(apt.appointmentDate)}
                        </Text>
                      </View>
                      <View className="rounded-full bg-mountain-meadow-100 px-2 py-0.5">
                        <Text className="text-xs font-semibold text-mountain-meadow-700">{apt.status}</Text>
                      </View>
                    </TouchableOpacity>
                  ))
                ) : (
                  <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-10">
                    <View className="mb-3 h-14 w-14 items-center justify-center rounded-full bg-gray-100">
                      <Ionicons name="calendar-outline" size={28} color="#9CA3AF" />
                    </View>
                    <Text className="text-sm font-medium text-gray-500">No upcoming appointments</Text>
                    <Text className="mt-1 text-xs text-gray-400">Your scheduled visits will appear here</Text>
                  </View>
                )}
              </View>

              {/* Health Alerts */}
              {data && (data.vaccineDue.length > 0 || data.dewormDue.length > 0) && (
                <View className="mt-6">
                  <Text className="mb-3 text-base font-semibold text-gray-900">Health Alerts</Text>
                  {data.vaccineDue.map((v) => (
                    <View key={v.appointmentId} className="mb-2 flex-row items-center rounded-xl border border-blue-100 bg-blue-50 p-4">
                      <Ionicons name="medical" size={18} color="#3B82F6" />
                      <View className="ml-3 flex-1">
                        <Text className="text-sm font-semibold text-gray-900">{v.petName} — Vaccine Due</Text>
                        {v.dueDate && <Text className="text-xs text-gray-400">Due {formatDate(v.dueDate)}</Text>}
                      </View>
                    </View>
                  ))}
                  {data.dewormDue.map((d) => (
                    <View key={d.appointmentId} className="mb-2 flex-row items-center rounded-xl border border-amber-100 bg-amber-50 p-4">
                      <Ionicons name="medkit" size={18} color="#D97706" />
                      <View className="ml-3 flex-1">
                        <Text className="text-sm font-semibold text-gray-900">{d.petName} — Deworming Due</Text>
                        {d.dueDate && <Text className="text-xs text-gray-400">Due {formatDate(d.dueDate)}</Text>}
                      </View>
                    </View>
                  ))}
                </View>
              )}
            </>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
