import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, TextInput, Platform } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';

export default function CreateAppointmentScreen() {
  const [pet, setPet] = useState('Geste');
  const appointmentTypes = ['Checkup', 'Vaccination', 'Dental', 'Grooming', 'Surgery', 'Emergency'];
  const [selectedType, setSelectedType] = useState(appointmentTypes[0]);
  const [date, setDate] = useState(new Date());
  const [showPicker, setShowPicker] = useState(false);
  const [notes, setNotes] = useState('');
  const times = ['08:00 AM','09:00 AM','10:00 AM','11:00 AM','12:00 PM','01:00 PM','02:00 PM','03:00 PM'];
  const [selectedTime, setSelectedTime] = useState(times[1]);

  const handleConfirm = () => {
    console.log({ pet, service: selectedType, date, time: selectedTime, notes });
    router.push('/(tabs)/(appointments)');
  };

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
          <Text className="text-xl font-bold text-gray-900">Book Appointment</Text>
          <Text className="text-xs text-gray-400">Schedule a new visit</Text>
        </View>
      </View>

      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="px-6 pt-5 pb-8">
          {/* Select Pet */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Select Pet</Text>
            <TouchableOpacity
              onPress={() => {
                if (Platform.OS === 'web') {
                  const input = window.prompt('Select pet name', pet || '');
                  if (input !== null) setPet(input);
                } else {
                  setPet(pet || 'Geste');
                }
              }}
              className="mb-0 flex-row items-center justify-between rounded-lg border border-gray-300 px-4 py-3 bg-white"
            >
              <Text className="text-gray-700">{pet || 'Select pet'}</Text>
              <Ionicons name="chevron-down" size={18} color="#6B7280" />
            </TouchableOpacity>
          </View>

          {/* Appointment */}
          <View className="mb-4">
            <Text className="mb-3 text-sm font-semibold text-gray-700">Appointment Type</Text>
            <View className="mb-2 flex-row flex-wrap">
              {appointmentTypes.map((t) => (
                <TouchableOpacity
                  key={t}
                  onPress={() => setSelectedType(t)}
                  className={`mr-3 mb-3 px-4 py-2 rounded-full border ${selectedType === t ? 'bg-mountain-meadow-600 border-transparent' : 'bg-white border-gray-300'}`}
                >
                  <Text className={`${selectedType === t ? 'text-white' : 'text-gray-700'} text-sm font-semibold`}>{t}</Text>
                </TouchableOpacity>
              ))}
            </View>
          </View>

          {/* Date & Time */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Date</Text>
            <View className="mb-3 flex-row items-center">
              <TouchableOpacity
                onPress={() => {
                  if (Platform.OS === 'web') {
                    const input = window.prompt('Enter date (MM/DD/YYYY)', date.toLocaleDateString());
                    if (input) {
                      const parsed = new Date(input);
                      if (!isNaN(parsed.getTime())) setDate(parsed);
                    }
                  } else {
                    setShowPicker(true);
                  }
                }}
                className="flex-1 mr-3 flex-row items-center rounded-lg border border-gray-300 px-4 py-3 bg-white"
              >
                <Text className="text-gray-500">{date.toLocaleDateString()}</Text>
                <Ionicons name="calendar-outline" size={18} color="#6B7280" className="ml-auto" />
              </TouchableOpacity>

              <TouchableOpacity
                onPress={() => {
                  if (Platform.OS === 'web') {
                    const input = window.prompt('Select time', selectedTime);
                    if (input) setSelectedTime(input);
                  } else {
                    const currentIndex = times.indexOf(selectedTime);
                    setSelectedTime(times[(currentIndex + 1) % times.length]);
                  }
                }}
                className="w-32 flex-row items-center justify-between rounded-lg border border-gray-300 px-3 py-3 bg-white"
              >
                <Text className="text-gray-700">{selectedTime}</Text>
                <Ionicons name="chevron-down" size={18} color="#6B7280" />
              </TouchableOpacity>
            </View>

            {Platform.OS !== 'web' && showPicker && (() => {
              try {
                // require at runtime so Metro/web bundler doesn't try to resolve the native-only package for web
                // eslint-disable-next-line @typescript-eslint/no-var-requires, @typescript-eslint/ban-ts-comment
                // @ts-ignore
                const DateTimePicker = eval('require')('@react-native-community/datetimepicker').default;
                return (
                  <DateTimePicker
                    value={date}
                    mode="datetime"
                    display="default"
                    onChange={(event, selectedDate) => {
                      setShowPicker(false);
                      if (selectedDate) setDate(selectedDate);
                    }}
                  />
                );
              } catch (err) {
                return null;
              }
            })()}
          </View>

          {/* Notes */}
          <View className="mb-6">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Notes</Text>
            <TextInput
              placeholder="Any special notes or symptoms..."
              value={notes}
              onChangeText={setNotes}
              multiline
              className="h-28 rounded-lg border border-gray-300 px-4 py-3 bg-white"
            />
          </View>

          {/* Footer Buttons */}
          <View className="flex-row items-center justify-between">
            <TouchableOpacity
              onPress={() => router.back()}
              className="flex-1 mr-3 rounded-full border border-gray-300 px-4 py-3 bg-white"
            >
              <Text className="text-center text-sm text-gray-700">Cancel</Text>
            </TouchableOpacity>
            <TouchableOpacity
              disabled={!pet || !selectedType}
              onPress={handleConfirm}
              className={`flex-1 ml-3 rounded-full py-3 ${!pet || !selectedType ? 'bg-gray-300' : 'bg-mountain-meadow-600'}`}
            >
              <Text className="text-center text-sm font-semibold text-white">Save Changes</Text>
            </TouchableOpacity>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
