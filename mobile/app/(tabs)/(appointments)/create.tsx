import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, TextInput, Modal } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import DateTimePicker from '@react-native-community/datetimepicker';
import { useAppStore } from '@/store/useAppStore';

const appointmentSchema = z.object({
  petId: z.string().min(1, 'Please select a pet'),
  serviceType: z.string().min(1, 'Please select a service'),
  date: z.date({ message: 'Date is required' }),
  time: z.date({ message: 'Time is required' }),
  notes: z.string().optional(),
});

type AppointmentFormData = z.infer<typeof appointmentSchema>;

const SERVICE_OPTIONS = ['Checkup', 'Vaccination', 'Dental', 'Grooming', 'Surgery', 'Boarding'];

export default function CreateAppointmentScreen() {
  const { pets, addAppointment } = useAppStore();
  const [petModalVisible, setPetModalVisible] = useState(false);
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [showTimePicker, setShowTimePicker] = useState(false);

  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<AppointmentFormData>({
    resolver: zodResolver(appointmentSchema),
    defaultValues: {
      petId: '',
      serviceType: '',
      notes: '',
    },
  });

  const selectedServiceType = watch('serviceType');
  const selectedPetId = watch('petId');
  const selectedPet = pets.find((p) => p.id === selectedPetId);

  const onSubmit = (data: AppointmentFormData) => {
    addAppointment({
      petId: data.petId,
      serviceType: data.serviceType,
      date: data.date.toISOString().split('T')[0],
      time: data.time.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      notes: data.notes,
    });
    router.back();
  };

  const SelectPetModal = () => (
    <Modal visible={petModalVisible} transparent animationType="slide">
      <View className="flex-1 justify-end bg-black/50">
        <View className="rounded-t-3xl bg-white p-6 pb-12">
          <View className="mb-4 flex-row items-center justify-between">
            <Text className="text-xl font-bold text-gray-900">Select Pet</Text>
            <TouchableOpacity onPress={() => setPetModalVisible(false)} className="p-2">
              <Ionicons name="close" size={24} color="#6B7280" />
            </TouchableOpacity>
          </View>
          {pets.length === 0 ? (
            <Text className="py-4 text-center text-gray-500">No pets available. Please add a pet first.</Text>
          ) : (
            pets.map((pet) => (
              <TouchableOpacity
                key={pet.id}
                className="flex-row items-center border-b border-gray-100 py-4"
                onPress={() => {
                  setValue('petId', pet.id, { shouldValidate: true });
                  setPetModalVisible(false);
                }}
              >
                <View className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-mountain-meadow-100">
                  <Ionicons name="paw" size={20} color="#059666" />
                </View>
                <View>
                  <Text className="text-base font-semibold text-gray-900">{pet.name}</Text>
                  <Text className="text-xs text-gray-500">{pet.species} • {pet.breed}</Text>
                </View>
              </TouchableOpacity>
            ))
          )}
        </View>
      </View>
    </Modal>
  );

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center bg-white px-5 pb-4 pt-3 shadow-sm">
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

      <ScrollView showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled">
        <View className="px-6 pt-5 pb-8">

          {/* Select Pet */}
          <View className="mb-6">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Select Pet</Text>
            <Controller
              control={control}
              name="petId"
              render={() => (
                <TouchableOpacity
                  onPress={() => setPetModalVisible(true)}
                  className={`flex-row items-center justify-between rounded-2xl border bg-white p-4 ${errors.petId ? 'border-red-500' : 'border-gray-200'
                    }`}
                  activeOpacity={0.7}
                >
                  <View className="flex-row items-center">
                    <View className="mr-3 h-12 w-12 items-center justify-center rounded-full bg-gray-50">
                      <Ionicons name="paw-outline" size={24} color={selectedPet ? "#059666" : "#9CA3AF"} />
                    </View>
                    <View>
                      <Text className={`text-base font-medium ${selectedPet ? 'text-gray-900' : 'text-gray-400'}`}>
                        {selectedPet ? selectedPet.name : 'Choose a pet'}
                      </Text>
                      {selectedPet && <Text className="text-xs text-gray-500">{selectedPet.species}</Text>}
                    </View>
                  </View>
                  <Ionicons name="chevron-down" size={20} color="#9CA3AF" />
                </TouchableOpacity>
              )}
            />
            {errors.petId && <Text className="mt-1 text-xs text-red-500">{errors.petId.message}</Text>}
          </View>

          {/* Select Service (Chips) */}
          <View className="mb-6">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Service Type</Text>
            <View className="flex-row flex-wrap gap-2">
              {SERVICE_OPTIONS.map((service) => {
                const isSelected = selectedServiceType === service;
                return (
                  <TouchableOpacity
                    key={service}
                    onPress={() => setValue('serviceType', service, { shouldValidate: true })}
                    className={`rounded-full px-4 py-2 border ${isSelected ? 'border-[#059666] bg-mountain-meadow-50' : 'border-gray-200 bg-white'
                      }`}
                  >
                    <Text className={`text-sm font-medium ${isSelected ? 'text-[#059666]' : 'text-gray-600'}`}>
                      {service}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </View>
            {errors.serviceType && <Text className="mt-1 text-xs text-red-500">{errors.serviceType.message}</Text>}
          </View>

          {/* Date & Time */}
          <View className="mb-6 flex-row gap-3">
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Date</Text>
              <Controller
                control={control}
                name="date"
                render={({ field: { value } }) => (
                  <>
                    <TouchableOpacity
                      onPress={() => setShowDatePicker(true)}
                      className={`flex-row items-center rounded-2xl border bg-white px-4 py-4 ${errors.date ? 'border-red-500' : 'border-gray-200'
                        }`}
                    >
                      <Ionicons name="calendar-outline" size={20} color="#9CA3AF" className="mr-2" />
                      <Text className={`ml-2 text-base ${value ? 'text-gray-900' : 'text-gray-400'}`}>
                        {value ? value.toLocaleDateString() : 'Select Date'}
                      </Text>
                    </TouchableOpacity>
                    {showDatePicker && (
                      <DateTimePicker
                        value={value || new Date()}
                        mode="date"
                        display="default"
                        minimumDate={new Date()}
                        onChange={(event, date) => {
                          setShowDatePicker(false);
                          if (date) setValue('date', date, { shouldValidate: true });
                        }}
                      />
                    )}
                  </>
                )}
              />
              {errors.date && <Text className="mt-1 text-xs text-red-500">{errors.date.message}</Text>}
            </View>

            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Time</Text>
              <Controller
                control={control}
                name="time"
                render={({ field: { value } }) => (
                  <>
                    <TouchableOpacity
                      onPress={() => setShowTimePicker(true)}
                      className={`flex-row items-center rounded-2xl border bg-white px-4 py-4 ${errors.time ? 'border-red-500' : 'border-gray-200'
                        }`}
                    >
                      <Ionicons name="time-outline" size={20} color="#9CA3AF" className="mr-2" />
                      <Text className={`ml-2 text-base ${value ? 'text-gray-900' : 'text-gray-400'}`}>
                        {value ? value.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Select Time'}
                      </Text>
                    </TouchableOpacity>
                    {showTimePicker && (
                      <DateTimePicker
                        value={value || new Date()}
                        mode="time"
                        display="default"
                        onChange={(event, date) => {
                          setShowTimePicker(false);
                          if (date) setValue('time', date, { shouldValidate: true });
                        }}
                      />
                    )}
                  </>
                )}
              />
              {errors.time && <Text className="mt-1 text-xs text-red-500">{errors.time.message}</Text>}
            </View>
          </View>

          {/* Notes */}
          <View className="mb-8">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Notes (Optional)</Text>
            <Controller
              control={control}
              name="notes"
              render={({ field: { onChange, onBlur, value } }) => (
                <TextInput
                  className="min-h-[100px] rounded-2xl border border-gray-200 bg-white p-4 text-base text-gray-900"
                  placeholder="Any special instructions or concerns..."
                  placeholderTextColor="#9CA3AF"
                  multiline
                  textAlignVertical="top"
                  onBlur={onBlur}
                  onChangeText={onChange}
                  value={value}
                />
              )}
            />
          </View>

          {/* Submit Button */}
          <TouchableOpacity
            onPress={handleSubmit(onSubmit)}
            className="rounded-2xl bg-[#059666] py-4 items-center shadow-sm"
            activeOpacity={0.8}
          >
            <Text className="text-base font-bold text-white">Confirm Booking</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>

      <SelectPetModal />
    </SafeAreaView>
  );
}
