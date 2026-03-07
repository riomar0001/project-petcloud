import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Modal,
  ActivityIndicator,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { AppointmentsService, PetsService, ApiError } from '@/api';
import type { PetListItem, ServiceCategory, TimeSlot } from '@/api';

// Lazy native-only import — avoids crashing the module on web
const DateTimePicker =
  Platform.OS !== 'web'
    ? require('@react-native-community/datetimepicker').default
    : null;

function formatDateForApi(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export default function CreateAppointmentScreen() {
  // Data
  const [pets, setPets] = useState<PetListItem[]>([]);
  const [services, setServices] = useState<ServiceCategory[]>([]);
  const [timeSlots, setTimeSlots] = useState<TimeSlot[]>([]);

  // Loading states
  const [loadingInit, setLoadingInit] = useState(true);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Form state
  const [selectedPet, setSelectedPet] = useState<PetListItem | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<ServiceCategory | null>(null);
  const [selectedSubtypeId, setSelectedSubtypeId] = useState<number | null>(null);
  const [date, setDate] = useState<Date>(() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d;
  });
  const [selectedTime, setSelectedTime] = useState<string | null>(null);
  const [notes, setNotes] = useState('');
  const [showDatePicker, setShowDatePicker] = useState(false);

  // Modals
  const [petModalVisible, setPetModalVisible] = useState(false);
  const [serviceModalVisible, setServiceModalVisible] = useState(false);
  const [subtypeModalVisible, setSubtypeModalVisible] = useState(false);
  const [timeModalVisible, setTimeModalVisible] = useState(false);

  // Feedback
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3500);
  }, []);

  // Load pets + services on mount
  useEffect(() => {
    (async () => {
      try {
        const [petsData, servicesData] = await Promise.all([
          PetsService.listPets(),
          AppointmentsService.getServices(),
        ]);
        setPets(petsData);
        setServices(servicesData);
      } catch {
        showToast('Failed to load data', false);
      } finally {
        setLoadingInit(false);
      }
    })();
  }, []);

  // Fetch time slots whenever date changes
  useEffect(() => {
    setSelectedTime(null);
    setTimeSlots([]);
    setLoadingSlots(true);
    AppointmentsService.getTimeSlots(formatDateForApi(date))
      .then((res) => setTimeSlots(res.slots))
      .catch(() => setTimeSlots([]))
      .finally(() => setLoadingSlots(false));
  }, [date]);

  const validate = () => {
    const errs: Record<string, string> = {};
    if (!selectedPet) errs.pet = 'Please select a pet';
    if (!selectedCategory) errs.service = 'Please select a service';
    if (!selectedTime) errs.time = 'Please select a time slot';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;
    setSubmitting(true);
    try {
      const message = await AppointmentsService.createAppointment({
        petId: selectedPet!.petId,
        categoryId: selectedCategory!.categoryId,
        subtypeId: selectedSubtypeId,
        appointmentDate: formatDateForApi(date),
        appointmentTime: selectedTime!,
        notes: notes.trim() || null,
      });
      showToast(message, true);
      setTimeout(() => router.back(), 1000);
    } catch (error: any) {
      showToast(
        error instanceof ApiError ? error.message : 'Failed to book appointment',
        false
      );
    } finally {
      setSubmitting(false);
    }
  };

  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);

  if (loadingInit) {
    return (
      <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
        <View className="flex-row items-center bg-white px-5 pb-4 pt-3 shadow-sm">
          <TouchableOpacity
            onPress={() => router.back()}
            className="mr-3 h-10 w-10 items-center justify-center rounded-full bg-gray-100"
            activeOpacity={0.7}
          >
            <Ionicons name="arrow-back" size={20} color="#374151" />
          </TouchableOpacity>
          <Text className="text-xl font-bold text-gray-900">Book Appointment</Text>
        </View>
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      </SafeAreaView>
    );
  }

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

      {/* Toast */}
      {toast && (
        <View className="px-6 pt-3">
          <View
            className={`flex-row items-center rounded-xl px-4 py-3 ${
              toast.success ? 'border border-green-200 bg-green-50' : 'border border-red-200 bg-red-50'
            }`}
          >
            <Ionicons
              name={toast.success ? 'checkmark-circle' : 'alert-circle'}
              size={20}
              color={toast.success ? '#16A34A' : '#EF4444'}
            />
            <Text
              className={`ml-2 flex-1 text-sm font-medium ${
                toast.success ? 'text-green-700' : 'text-red-600'
              }`}
            >
              {toast.message}
            </Text>
          </View>
        </View>
      )}

      <ScrollView showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled">
        <View className="px-6 pt-5 pb-8">

          {/* Select Pet */}
          <View className="mb-5">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Select Pet</Text>
            <TouchableOpacity
              onPress={() => setPetModalVisible(true)}
              className={`flex-row items-center justify-between rounded-2xl border bg-white px-4 py-4 ${
                errors.pet ? 'border-red-500' : 'border-gray-200'
              }`}
            >
              <Text className={`text-base ${selectedPet ? 'text-gray-900' : 'text-gray-400'}`}>
                {selectedPet ? selectedPet.name : 'Choose your pet'}
              </Text>
              <Ionicons name="chevron-down" size={18} color="#6B7280" />
            </TouchableOpacity>
            {errors.pet ? <Text className="mt-1 text-xs text-red-500">{errors.pet}</Text> : null}
          </View>

          {/* Service */}
          <View className="mb-5">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Service</Text>
            <TouchableOpacity
              onPress={() => setServiceModalVisible(true)}
              className={`flex-row items-center justify-between rounded-2xl border bg-white px-4 py-4 ${
                errors.service ? 'border-red-500' : 'border-gray-200'
              }`}
            >
              <Text className={`text-base ${selectedCategory ? 'text-gray-900' : 'text-gray-400'}`}>
                {selectedCategory ? selectedCategory.serviceType : 'Choose a service'}
              </Text>
              <Ionicons name="chevron-down" size={18} color="#6B7280" />
            </TouchableOpacity>
            {errors.service ? (
              <Text className="mt-1 text-xs text-red-500">{errors.service}</Text>
            ) : null}
          </View>

          {/* Subtype (if available) */}
          {selectedCategory && selectedCategory.subtypes.length > 0 && (
            <View className="mb-5">
              <Text className="mb-2 text-sm font-semibold text-gray-700">
                Subtype <Text className="font-normal text-gray-400">(optional)</Text>
              </Text>
              <TouchableOpacity
                onPress={() => setSubtypeModalVisible(true)}
                className="flex-row items-center justify-between rounded-2xl border border-gray-200 bg-white px-4 py-4"
              >
                <Text className={`text-base ${selectedSubtypeId ? 'text-gray-900' : 'text-gray-400'}`}>
                  {selectedCategory.subtypes.find((s) => s.subtypeId === selectedSubtypeId)
                    ?.serviceSubType ?? 'None (optional)'}
                </Text>
                <Ionicons name="chevron-down" size={18} color="#6B7280" />
              </TouchableOpacity>
            </View>
          )}

          {/* Date */}
          <View className="mb-5">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Date</Text>
            <TouchableOpacity
              onPress={() => setShowDatePicker(true)}
              className="flex-row items-center justify-between rounded-2xl border border-gray-200 bg-white px-4 py-4"
            >
              <Text className="text-base text-gray-900">
                {date.toLocaleDateString('en-US', { weekday: 'short', month: 'long', day: 'numeric', year: 'numeric' })}
              </Text>
              <Ionicons name="calendar-outline" size={18} color="#6B7280" />
            </TouchableOpacity>
            {showDatePicker && DateTimePicker && (
              <DateTimePicker
                value={date}
                mode="date"
                display="default"
                minimumDate={tomorrow}
                onChange={(_: any, d: Date | undefined) => {
                  setShowDatePicker(false);
                  if (d) setDate(d);
                }}
              />
            )}
          </View>

          {/* Time Slots */}
          <View className="mb-5">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Time Slot</Text>
            {loadingSlots ? (
              <View className="flex-row items-center justify-between rounded-2xl border border-gray-200 bg-white px-4 py-4">
                <Text className="text-base text-gray-400">Loading slots...</Text>
                <ActivityIndicator size="small" color="#059666" />
              </View>
            ) : (
              <TouchableOpacity
                onPress={() => timeSlots.length > 0 && setTimeModalVisible(true)}
                disabled={timeSlots.length === 0}
                className={`flex-row items-center justify-between rounded-2xl border bg-white px-4 py-4 ${
                  errors.time ? 'border-red-500' : 'border-gray-200'
                }`}
              >
                <Text className={`text-base ${selectedTime ? 'text-gray-900' : 'text-gray-400'}`}>
                  {timeSlots.length === 0
                    ? 'No available slots for this date'
                    : selectedTime
                    ? selectedTime
                    : 'Choose a time slot'}
                </Text>
                <Ionicons name="chevron-down" size={18} color="#6B7280" />
              </TouchableOpacity>
            )}
            {errors.time ? <Text className="mt-1 text-xs text-red-500">{errors.time}</Text> : null}
          </View>

          {/* Notes */}
          <View className="mb-8">
            <Text className="mb-2 text-sm font-semibold text-gray-700">
              Notes <Text className="font-normal text-gray-400">(optional)</Text>
            </Text>
            <TextInput
              placeholder="Any special notes or symptoms..."
              placeholderTextColor="#9CA3AF"
              value={notes}
              onChangeText={setNotes}
              multiline
              numberOfLines={4}
              textAlignVertical="top"
              className="h-28 rounded-2xl border border-gray-200 bg-white px-4 py-3 text-base text-gray-900"
            />
          </View>

          {/* Submit */}
          <TouchableOpacity
            onPress={handleSubmit}
            disabled={submitting}
            className={`rounded-2xl py-4 items-center shadow-sm ${
              submitting ? 'bg-gray-300' : 'bg-mountain-meadow-600'
            }`}
            activeOpacity={0.8}
          >
            {submitting ? (
              <ActivityIndicator color="#FFFFFF" />
            ) : (
              <Text className="text-base font-bold text-white">Confirm Booking</Text>
            )}
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Pet Select Modal */}
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
              <View className="items-center py-6">
                <Text className="text-sm text-gray-400">No pets found. Add a pet first.</Text>
              </View>
            ) : (
              pets.map((p) => (
                <TouchableOpacity
                  key={p.petId}
                  className="border-b border-gray-100 py-4"
                  onPress={() => {
                    setSelectedPet(p);
                    setPetModalVisible(false);
                    setErrors((e) => ({ ...e, pet: '' }));
                  }}
                >
                  <Text className="text-base font-medium text-gray-900">{p.name}</Text>
                  <Text className="text-xs text-gray-400">{p.breed} · {p.type}</Text>
                </TouchableOpacity>
              ))
            )}
          </View>
        </View>
      </Modal>

      {/* Service Select Modal */}
      <Modal visible={serviceModalVisible} transparent animationType="slide">
        <View className="flex-1 justify-end bg-black/50">
          <View className="rounded-t-3xl bg-white p-6 pb-12">
            <View className="mb-4 flex-row items-center justify-between">
              <Text className="text-xl font-bold text-gray-900">Select Service</Text>
              <TouchableOpacity onPress={() => setServiceModalVisible(false)} className="p-2">
                <Ionicons name="close" size={24} color="#6B7280" />
              </TouchableOpacity>
            </View>
            {services.map((svc) => (
              <TouchableOpacity
                key={svc.categoryId}
                className="border-b border-gray-100 py-4"
                onPress={() => {
                  setSelectedCategory(svc);
                  setSelectedSubtypeId(null);
                  setServiceModalVisible(false);
                  setErrors((e) => ({ ...e, service: '' }));
                }}
              >
                <Text className="text-base font-medium text-gray-900">{svc.serviceType}</Text>
                {svc.subtypes.length > 0 && (
                  <Text className="text-xs text-gray-400">
                    {svc.subtypes.length} subtype{svc.subtypes.length !== 1 ? 's' : ''} available
                  </Text>
                )}
              </TouchableOpacity>
            ))}
          </View>
        </View>
      </Modal>

      {/* Time Slot Modal */}
      <Modal visible={timeModalVisible} transparent animationType="slide">
        <View className="flex-1 justify-end bg-black/50">
          <View className="rounded-t-3xl bg-white p-6 pb-12" style={{ maxHeight: '70%' }}>
            <View className="mb-4 flex-row items-center justify-between">
              <Text className="text-xl font-bold text-gray-900">Select Time Slot</Text>
              <TouchableOpacity onPress={() => setTimeModalVisible(false)} className="p-2">
                <Ionicons name="close" size={24} color="#6B7280" />
              </TouchableOpacity>
            </View>
            <ScrollView showsVerticalScrollIndicator={false}>
              {timeSlots.map((slot) => (
                <TouchableOpacity
                  key={slot.time}
                  disabled={!slot.available}
                  className="flex-row items-center justify-between border-b border-gray-100 py-4"
                  onPress={() => {
                    setSelectedTime(slot.time);
                    setTimeModalVisible(false);
                    setErrors((e) => ({ ...e, time: '' }));
                  }}
                >
                  <Text
                    className={`text-base font-medium ${
                      !slot.available ? 'text-gray-300' : selectedTime === slot.time ? 'text-mountain-meadow-600' : 'text-gray-900'
                    }`}
                  >
                    {slot.time}
                  </Text>
                  {!slot.available ? (
                    <Text className="text-xs text-gray-300">Unavailable</Text>
                  ) : selectedTime === slot.time ? (
                    <Ionicons name="checkmark-circle" size={20} color="#059666" />
                  ) : null}
                </TouchableOpacity>
              ))}
            </ScrollView>
          </View>
        </View>
      </Modal>

      {/* Subtype Modal */}
      {selectedCategory && (
        <Modal visible={subtypeModalVisible} transparent animationType="slide">
          <View className="flex-1 justify-end bg-black/50">
            <View className="rounded-t-3xl bg-white p-6 pb-12">
              <View className="mb-4 flex-row items-center justify-between">
                <Text className="text-xl font-bold text-gray-900">Select Subtype</Text>
                <TouchableOpacity onPress={() => setSubtypeModalVisible(false)} className="p-2">
                  <Ionicons name="close" size={24} color="#6B7280" />
                </TouchableOpacity>
              </View>
              <TouchableOpacity
                className="border-b border-gray-100 py-4"
                onPress={() => { setSelectedSubtypeId(null); setSubtypeModalVisible(false); }}
              >
                <Text className="text-base text-gray-400">None (skip)</Text>
              </TouchableOpacity>
              {selectedCategory.subtypes.map((st) => (
                <TouchableOpacity
                  key={st.subtypeId}
                  className="border-b border-gray-100 py-4"
                  onPress={() => { setSelectedSubtypeId(st.subtypeId); setSubtypeModalVisible(false); }}
                >
                  <Text className="text-base font-medium text-gray-900">{st.serviceSubType}</Text>
                </TouchableOpacity>
              ))}
            </View>
          </View>
        </Modal>
      )}
    </SafeAreaView>
  );
}
