import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, Alert, ActivityIndicator } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router, useLocalSearchParams } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { AppointmentsService } from '@/api';

function formatDate(iso: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });
}

function formatTime(iso: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

function statusColor(status: string): string {
  switch (status.toLowerCase()) {
    case 'confirmed': return '#059666';
    case 'pending': return '#F59E0B';
    case 'completed': return '#3B82F6';
    case 'cancelled': return '#EF4444';
    case 'cancellationrequested': return '#F97316';
    default: return '#6B7280';
  }
}

function statusBg(status: string): string {
  switch (status.toLowerCase()) {
    case 'confirmed': return 'bg-mountain-meadow-100';
    case 'pending': return 'bg-amber-100';
    case 'completed': return 'bg-blue-100';
    case 'cancelled': return 'bg-red-100';
    case 'cancellationrequested': return 'bg-orange-100';
    default: return 'bg-gray-100';
  }
}

export default function AppointmentDetailScreen() {
  const params = useLocalSearchParams<{
    id: string;
    petName: string;
    serviceType: string;
    serviceSubtype: string;
    appointmentDate: string;
    status: string;
    notes: string;
  }>();

  const appointmentId = Number(params.id);
  const [cancelling, setCancelling] = useState(false);

  const isCancellable =
    params.status.toLowerCase() === 'pending' ||
    params.status.toLowerCase() === 'confirmed';

  const handleCancel = () => {
    Alert.alert(
      'Cancel Appointment',
      'Are you sure you want to request cancellation for this appointment?',
      [
        { text: 'No', style: 'cancel' },
        {
          text: 'Yes, Cancel',
          style: 'destructive',
          onPress: async () => {
            setCancelling(true);
            try {
              await AppointmentsService.cancelAppointment(appointmentId);
              Alert.alert('Success', 'Cancellation request submitted.', [
                { text: 'OK', onPress: () => router.back() },
              ]);
            } catch (error: any) {
              Alert.alert('Error', error.message || 'Failed to cancel appointment.');
            } finally {
              setCancelling(false);
            }
          },
        },
      ]
    );
  };

  const color = statusColor(params.status);
  const bg = statusBg(params.status);

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
          <Text className="text-xs text-gray-400">#{params.id}</Text>
        </View>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-5 pb-8">
          {/* Status Badge */}
          <View className="mb-5 items-center rounded-2xl border border-gray-100 bg-white p-5">
            <View className="mb-3 h-14 w-14 items-center justify-center rounded-full bg-mountain-meadow-100">
              <Ionicons name="calendar" size={28} color="#059666" />
            </View>
            <View className={`rounded-full px-4 py-1.5 ${bg}`}>
              <Text className="text-sm font-semibold" style={{ color }}>
                {params.status}
              </Text>
            </View>
          </View>

          {/* Details */}
          <View className="rounded-2xl border border-gray-100 bg-white">
            {[
              {
                icon: 'paw' as const,
                label: 'Pet',
                value: params.petName || '—',
              },
              {
                icon: 'medkit' as const,
                label: 'Service',
                value: [params.serviceType, params.serviceSubtype].filter(Boolean).join(' · ') || '—',
              },
              {
                icon: 'calendar' as const,
                label: 'Date',
                value: formatDate(params.appointmentDate),
              },
              {
                icon: 'time-outline' as const,
                label: 'Time',
                value: formatTime(params.appointmentDate),
              },
              ...(params.notes
                ? [{ icon: 'chatbubble-outline' as const, label: 'Notes', value: params.notes }]
                : []),
            ].map((item, i) => (
              <View
                key={i}
                className={`flex-row items-start px-4 py-4 ${i > 0 ? 'border-t border-gray-100' : ''}`}
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

          {/* Actions */}
          {isCancellable && (
            <TouchableOpacity
              onPress={handleCancel}
              disabled={cancelling}
              className="mt-5 flex-row items-center justify-center rounded-2xl border border-red-200 bg-red-50 py-4"
              activeOpacity={0.7}
            >
              {cancelling ? (
                <ActivityIndicator color="#EF4444" />
              ) : (
                <>
                  <Ionicons name="close-circle-outline" size={18} color="#EF4444" />
                  <Text className="ml-2 text-sm font-semibold text-red-500">
                    Request Cancellation
                  </Text>
                </>
              )}
            </TouchableOpacity>
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
