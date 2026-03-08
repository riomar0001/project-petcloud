import React, { useCallback, useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, ActivityIndicator, RefreshControl } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router, useFocusEffect } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { AppointmentsService } from '@/api';
import type { AppointmentListItem } from '@/api';

type FilterType = 'all' | 'upcoming' | 'completed' | 'cancelled';

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

function statusColor(status: string): string {
  switch (status.toLowerCase()) {
    case 'confirmed': return '#059666';
    case 'pending': return '#F59E0B';
    case 'completed': return '#3B82F6';
    case 'cancelled': return '#EF4444';
    case 'cancellation requested':
    case 'cancellationrequested': return '#F97316';
    default: return '#6B7280';
  }
}

function serviceColor(type: string | null | undefined): string {
  const t = (type ?? '').toLowerCase();
  if (t.includes('vaccine') || t.includes('vaccination')) return '#3B82F6';
  if (t.includes('deworm') || t.includes('deworming')) return '#F59E0B';
  if (t.includes('checkup') || t.includes('consultation')) return '#059666';
  if (t.includes('dental')) return '#8B5CF6';
  if (t.includes('groo')) return '#EC4899';
  return '#6B7280';
}

function serviceIcon(type: string | null | undefined): keyof typeof import('@expo/vector-icons').Ionicons.glyphMap {
  const t = (type ?? '').toLowerCase();
  if (t.includes('vaccine') || t.includes('vaccination')) return 'medical';
  if (t.includes('deworm') || t.includes('deworming')) return 'medkit';
  if (t.includes('dental')) return 'build-outline';
  if (t.includes('groo')) return 'color-wand-outline';
  return 'calendar';
}

function applyFilter(items: AppointmentListItem[], filter: FilterType): AppointmentListItem[] {
  if (filter === 'all') return items;
  if (filter === 'upcoming') {
    return items.filter((a) =>
      ['pending', 'confirmed', 'requested', 'r', 'cancellation requested'].includes(a.status.toLowerCase())
    );
  }
  if (filter === 'completed') {
    return items.filter((a) => a.status.toLowerCase() === 'completed');
  }
  if (filter === 'cancelled') {
    return items.filter(
      (a) =>
        a.status.toLowerCase() === 'cancelled' ||
        a.status.toLowerCase() === 'cancellationrequested'
    );
  }
  return items;
}

export default function AppointmentsScreen() {
  const [appointments, setAppointments] = useState<AppointmentListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [filterType, setFilterType] = useState<FilterType>('all');

  const load = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const data = await AppointmentsService.listAppointments();
      setAppointments(data);
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

  const displayed = applyFilter(appointments, filterType);
  const upcomingCount = appointments.filter(
    (a) => a.status.toLowerCase() === 'pending' || a.status.toLowerCase() === 'confirmed'
  ).length;
  const completedCount = appointments.filter((a) => a.status.toLowerCase() === 'completed').length;

  const FILTERS: { key: FilterType; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'upcoming', label: 'Upcoming' },
    { key: 'completed', label: 'Completed' },
    { key: 'cancelled', label: 'Cancelled' },
  ];

  return (
    <SafeAreaView className="flex-1 bg-gray-100" edges={['top']}>
      {/* Header */}
      <View className="bg-white px-6 py-4">
        <Text className="text-2xl font-bold text-gray-900">My Appointments</Text>
        <Text className="mt-0.5 text-md text-gray-400">View and schedule appointments</Text>
      </View>

      <View className="px-6 mt-3 mb-4 flex-row justify-end">
        <TouchableOpacity
          onPress={() => router.push('/(tabs)/(appointments)/create')}
          className="flex-row items-center justify-center rounded-full bg-mountain-meadow-600 px-5 h-12 shadow"
          activeOpacity={0.7}
          style={{ elevation: 4 }}
        >
          <Ionicons name="add" size={20} color="#FFFFFF" />
          <Text className="ml-2 text-sm font-semibold text-white">Book Appointment</Text>
        </TouchableOpacity>
      </View>

      {loading ? (
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      ) : (
        <ScrollView
          showsVerticalScrollIndicator={false}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(true); }}
              tintColor="#059666"
            />
          }
        >
          <View className="px-6 pb-8">
            {/* Stats */}
            <View className="mb-5 flex-row gap-3">
              <View className="flex-1 rounded-xl bg-white px-4 py-4 shadow-sm">
                <Text className="text-xs text-gray-400">Upcoming</Text>
                <Text className="mt-1 text-2xl font-bold text-mountain-meadow-600">{upcomingCount}</Text>
              </View>
              <View className="flex-1 rounded-xl bg-white px-4 py-4 shadow-sm">
                <Text className="text-xs text-gray-400">Completed</Text>
                <Text className="mt-1 text-2xl font-bold text-blue-600">{completedCount}</Text>
              </View>
            </View>

            {/* Filters */}
            <ScrollView horizontal showsHorizontalScrollIndicator={false} className="mb-4 -mx-1">
              {FILTERS.map((f) => (
                <TouchableOpacity
                  key={f.key}
                  onPress={() => setFilterType(f.key)}
                  className={`mr-2 mx-1 rounded-full px-4 py-2 ${
                    filterType === f.key
                      ? 'bg-mountain-meadow-600'
                      : 'bg-white border border-gray-200'
                  }`}
                >
                  <Text
                    className={`text-xs font-semibold ${
                      filterType === f.key ? 'text-white' : 'text-gray-700'
                    }`}
                  >
                    {f.label}
                  </Text>
                </TouchableOpacity>
              ))}
            </ScrollView>

            {/* List */}
            {displayed.length > 0 ? (
              displayed.map((apt) => {
                const color = serviceColor(apt.serviceType);
                return (
                  <TouchableOpacity
                    key={apt.appointmentId}
                    onPress={() =>
                      router.push({
                        pathname: '/(tabs)/(appointments)/[id]',
                        params: {
                          id: apt.appointmentId.toString(),
                          petName: apt.petName ?? '',
                          serviceType: apt.serviceType ?? '',
                          serviceSubtype: apt.serviceSubtype ?? '',
                          appointmentDate: apt.appointmentDate,
                          status: apt.status,
                          notes: apt.notes ?? '',
                        },
                      })
                    }
                    className="mb-3 flex-row rounded-2xl bg-white p-4 shadow-sm"
                    activeOpacity={0.8}
                  >
                    <View
                      className="mr-4 h-12 w-12 items-center justify-center rounded-full"
                      style={{ backgroundColor: color + '20' }}
                    >
                      <Ionicons name={serviceIcon(apt.serviceType)} size={22} color={color} />
                    </View>

                    <View className="flex-1">
                      <View className="mb-1 flex-row items-center justify-between">
                        <Text className="text-sm font-semibold text-gray-900">{apt.petName ?? '—'}</Text>
                        <View
                          className="rounded-full px-2 py-0.5"
                          style={{ backgroundColor: statusColor(apt.status) + '20' }}
                        >
                          <Text
                            className="text-xs font-semibold"
                            style={{ color: statusColor(apt.status) }}
                          >
                            {apt.status}
                          </Text>
                        </View>
                      </View>
                      <Text className="text-xs font-medium text-gray-600">
                        {apt.serviceType ?? 'Appointment'}
                        {apt.serviceSubtype ? ` · ${apt.serviceSubtype}` : ''}
                      </Text>
                      <View className="mt-1.5 flex-row items-center">
                        <Ionicons name="calendar-outline" size={13} color="#6B7280" />
                        <Text className="ml-1 text-xs text-gray-500">
                          {formatDate(apt.appointmentDate)} at {formatTime(apt.appointmentDate)}
                        </Text>
                      </View>
                    </View>

                    <Ionicons name="chevron-forward" size={18} color="#D1D5DB" style={{ alignSelf: 'center' }} />
                  </TouchableOpacity>
                );
              })
            ) : (
              <View className="items-center py-12">
                <View className="mb-3 h-16 w-16 items-center justify-center rounded-full bg-mountain-meadow-50">
                  <Ionicons name="calendar-outline" size={32} color="#059666" />
                </View>
                <Text className="text-sm font-semibold text-gray-900">No appointments found</Text>
                <Text className="mt-1 text-xs text-gray-400">
                  {filterType === 'all' ? 'Schedule your first visit' : `No ${filterType} appointments`}
                </Text>
              </View>
            )}
          </View>
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
