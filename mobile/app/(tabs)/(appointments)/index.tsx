import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

const mockAppointments = [
  {
    id: '1',
    pet: 'Geste',
    type: 'Checkup',
    date: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000),
    time: '10:00 AM',
    vet: 'Dr. Sarah',
    status: 'upcoming',
  },
  {
    id: '2',
    pet: 'Bella',
    type: 'Vaccination',
    date: new Date(Date.now() + 5 * 24 * 60 * 60 * 1000),
    time: '02:00 PM',
    vet: 'Dr. Mike',
    status: 'upcoming',
  },
  {
    id: '3',
    pet: 'Max',
    type: 'Dental',
    date: new Date(Date.now() + 10 * 24 * 60 * 60 * 1000),
    time: '09:00 AM',
    vet: 'Dr. Sarah',
    status: 'upcoming',
  },
];

const getServiceIconColor = (type: string) => {
  const colors: { [key: string]: string } = {
    'Checkup': '#059666',
    'Vaccination': '#3B82F6',
    'Dental': '#F59E0B',
    'Grooming': '#EC4899',
    'Surgery': '#EF4444',
    'Emergency': '#DC2626',
  };
  return colors[type] || '#6B7280';
};

const getServiceIcon = (type: string) => {
  const icons: { [key: string]: string } = {
    'Checkup': 'stethoscope',
    'Vaccination': 'medical',
    'Dental': 'medkit',
    'Grooming': 'sparkles',
    'Surgery': 'cut',
    'Emergency': 'alert-circle',
  };
  return icons[type] || 'calendar';
};

export default function AppointmentsScreen() {
  const [filterType, setFilterType] = useState('all');

  const upcomingCount = mockAppointments.length;
  
  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="bg-mountain-meadow-50 px-6 py-4">
        <View className="mb-2">
          <Text className="text-2xl font-bold text-gray-900">My Appointments</Text>
          <Text className="mt-0.5 text-sm text-gray-400">View and schedule Appointments</Text>
        </View>
      </View>

      <View className="px-6 mt-3 mb-6 flex-row justify-end">
        <TouchableOpacity
          onPress={() => router.push('/(tabs)/(appointments)/create')}
          className="flex-row items-center justify-center rounded-full bg-mountain-meadow-600 px-5 h-12 shadow"
          activeOpacity={0.7}
          style={{ elevation: 4 }}
        >
          <Ionicons name="add" size={20} color="#FFFFFF" />
          <Text className="ml-3 text-base font-semibold text-white">Book Appointment</Text>
        </TouchableOpacity>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-6 pb-8">
          <View className="mb-6 flex-row justify-between">
            <View className="flex-1 mr-3 rounded-xl bg-white px-4 py-4 shadow-sm">
              <Text className="text-xs text-gray-400">Upcoming</Text>
              <Text className="mt-1 text-2xl font-bold text-mountain-meadow-600">{upcomingCount}</Text>
            </View>
            <View className="flex-1 mx-1.5 rounded-xl bg-white px-4 py-4 shadow-sm">
              <Text className="text-xs text-gray-400">Completed</Text>
              <Text className="mt-1 text-2xl font-bold text-blue-600">0</Text>
            </View>
          </View>

          {/* Filter*/}
          <View className="mb-4 flex-row">
            {['all', 'upcoming', 'completed'].map((tab) => (
              <TouchableOpacity
                key={tab}
                onPress={() => setFilterType(tab)}
                className={`mr-3 rounded-full px-4 py-2 ${filterType === tab ? 'bg-mountain-meadow-600' : 'bg-white border border-gray-300'}`}
              >
                <Text className={`text-xs font-semibold capitalize ${filterType === tab ? 'text-white' : 'text-gray-700'}`}>
                  {tab}
                </Text>
              </TouchableOpacity>
            ))}
          </View>

          {/* Appointments List */}
          <View>
            <Text className="mb-3 text-sm font-semibold text-gray-700">Upcoming Appointments</Text>
            {mockAppointments.length > 0 ? (
              mockAppointments.map((apt) => (
                <TouchableOpacity
                  key={apt.id}
                  onPress={() => {/* navigate to detail */}}
                  className="mb-3 flex-row rounded-xl bg-white p-4 shadow-sm"
                >
                  {/* Service Icon */}
                  <View
                    className="mr-4 h-12 w-12 items-center justify-center rounded-full"
                    style={{ backgroundColor: getServiceIconColor(apt.type) + '20' }}
                  >
                    <Ionicons
                      name={getServiceIcon(apt.type)}
                      size={24}
                      color={getServiceIconColor(apt.type)}
                    />
                  </View>

                  {/* Appointment Details */}
                  <View className="flex-1">
                    <View className="mb-1 flex-row items-center justify-between">
                      <Text className="text-sm font-semibold text-gray-900">{apt.pet}</Text>
                      <View
                        className="px-2 py-1 rounded-full"
                        style={{ backgroundColor: getServiceIconColor(apt.type) + '15' }}
                      >
                        <Text
                          className="text-xs font-semibold"
                          style={{ color: getServiceIconColor(apt.type) }}
                        >
                          {apt.type}
                        </Text>
                      </View>
                    </View>
                    <Text className="text-xs text-gray-500">with {apt.vet}</Text>
                    <View className="mt-2 flex-row items-center">
                      <Ionicons name="calendar-outline" size={14} color="#6B7280" />
                      <Text className="ml-1 text-xs text-gray-600">
                        {apt.date.toLocaleDateString()} at {apt.time}
                      </Text>
                    </View>
                  </View>

                  {/* Arrow */}
                  <Ionicons name="chevron-forward" size={20} color="#D1D5DB" />
                </TouchableOpacity>
              ))
            ) : (
              <View className="items-center py-12">
                <View className="mb-3 h-16 w-16 items-center justify-center rounded-full bg-mountain-meadow-50">
                  <Ionicons name="calendar-outline" size={32} color="#059666" />
                </View>
                <Text className="text-sm font-semibold text-gray-900">No upcoming appointments</Text>
                <Text className="mt-1 text-xs text-gray-400">Schedule your first visit</Text>
              </View>
            )}
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
