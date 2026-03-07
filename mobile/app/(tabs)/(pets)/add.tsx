import React, { useState, useCallback, useEffect } from 'react';
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
import * as ImagePicker from 'expo-image-picker';
import { Image } from 'expo-image';

const DateTimePicker =
  Platform.OS !== 'web'
    ? require('@react-native-community/datetimepicker').default
    : null;
import { PetsService, ApiError } from '@/api';

const TYPE_OPTIONS = ['Dog', 'Cat', 'Bird', 'Rabbit', 'Other'];

function formatDateForApi(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

type PhotoAsset = { uri: string; name: string; type: string };

export default function AddPetScreen() {
  const [name, setName] = useState('');
  const [type, setType] = useState('');
  const [breed, setBreed] = useState('');
  const [birthdate, setBirthdate] = useState<Date | null>(null);
  const [photo, setPhoto] = useState<PhotoAsset | null>(null);

  const [breeds, setBreeds] = useState<string[]>([]);
  const [loadingBreeds, setLoadingBreeds] = useState(false);

  const [typeModalVisible, setTypeModalVisible] = useState(false);
  const [breedModalVisible, setBreedModalVisible] = useState(false);
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [confirmModalVisible, setConfirmModalVisible] = useState(false);

  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3000);
  }, []);

  // Fetch breeds when type is Dog or Cat
  useEffect(() => {
    if (type === 'Dog' || type === 'Cat') {
      setLoadingBreeds(true);
      setBreed('');
      setBreeds([]);
      PetsService.getBreeds(type.toLowerCase())
        .then(setBreeds)
        .catch(() => setBreeds([]))
        .finally(() => setLoadingBreeds(false));
    } else {
      setBreeds([]);
      setBreed('');
    }
  }, [type]);

  const pickImage = async () => {
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });
    if (!result.canceled && result.assets[0]) {
      const asset = result.assets[0];
      setPhoto({
        uri: asset.uri,
        name: asset.uri.split('/').pop() || 'photo.jpg',
        type: asset.mimeType || 'image/jpeg',
      });
    }
  };

  const validate = () => {
    const errs: Record<string, string> = {};
    if (!name.trim()) errs.name = 'Name is required';
    if (!type) errs.type = 'Type is required';
    if (!birthdate) errs.birthdate = 'Birthdate is required';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = () => {
    if (validate()) setConfirmModalVisible(true);
  };

  const handleConfirmAdd = async () => {
    setConfirmModalVisible(false);
    setSaving(true);
    try {
      await PetsService.createPet(
        {
          Name: name.trim(),
          Type: type,
          Breed: breed.trim() || undefined,
          Birthdate: formatDateForApi(birthdate!),
        },
        photo ?? undefined
      );
      showToast('Pet added successfully!', true);
      setTimeout(() => router.back(), 1000);
    } catch (error: any) {
      showToast(
        error instanceof ApiError ? error.message : 'Failed to add pet',
        false
      );
    } finally {
      setSaving(false);
    }
  };

  const useBreedModal = (type === 'Dog' || type === 'Cat') && breeds.length > 0;

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
          <Text className="text-xl font-bold text-gray-900">Add Pet</Text>
          <Text className="text-xs text-gray-400">Register a new companion</Text>
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
          {/* Photo Upload */}
          <View className="mb-5 items-center">
            <TouchableOpacity onPress={pickImage} activeOpacity={0.8}>
              <View className="h-24 w-24 overflow-hidden rounded-full border-2 border-dashed border-gray-300 bg-gray-50 items-center justify-center">
                {photo ? (
                  <Image source={{ uri: photo.uri }} style={{ width: '100%', height: '100%' }} contentFit="cover" />
                ) : (
                  <Ionicons name="camera-outline" size={32} color="#9CA3AF" />
                )}
              </View>
            </TouchableOpacity>
            <Text className="mt-2 text-xs text-gray-400">Tap to add photo (optional)</Text>
          </View>

          {/* Pet Name */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Pet Name</Text>
            <TextInput
              className={`rounded-2xl border bg-white px-4 py-4 text-base text-gray-900 ${
                errors.name ? 'border-red-500' : 'border-gray-200'
              }`}
              placeholder="e.g. Bella"
              placeholderTextColor="#9CA3AF"
              value={name}
              onChangeText={(t) => { setName(t); setErrors((e) => ({ ...e, name: '' })); }}
            />
            {errors.name ? <Text className="mt-1 text-xs text-red-500">{errors.name}</Text> : null}
          </View>

          {/* Type & Breed */}
          <View className="mb-4 flex-row gap-3">
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Type</Text>
              <TouchableOpacity
                onPress={() => setTypeModalVisible(true)}
                className={`rounded-2xl border bg-white px-4 py-4 ${
                  errors.type ? 'border-red-500' : 'border-gray-200'
                }`}
              >
                <Text className={`text-base ${type ? 'text-gray-900' : 'text-gray-400'}`}>
                  {type || 'Select'}
                </Text>
              </TouchableOpacity>
              {errors.type ? <Text className="mt-1 text-xs text-red-500">{errors.type}</Text> : null}
            </View>

            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Breed</Text>
              {loadingBreeds ? (
                <View className="h-14 items-center justify-center rounded-2xl border border-gray-200 bg-white">
                  <ActivityIndicator size="small" color="#059666" />
                </View>
              ) : useBreedModal ? (
                <TouchableOpacity
                  onPress={() => setBreedModalVisible(true)}
                  className="rounded-2xl border border-gray-200 bg-white px-4 py-4"
                >
                  <Text className={`text-base ${breed ? 'text-gray-900' : 'text-gray-400'}`}>
                    {breed || 'Select'}
                  </Text>
                </TouchableOpacity>
              ) : (
                <TextInput
                  className="rounded-2xl border border-gray-200 bg-white px-4 py-4 text-base text-gray-900"
                  placeholder="e.g. Mixed"
                  placeholderTextColor="#9CA3AF"
                  value={breed}
                  onChangeText={setBreed}
                  editable={!!type}
                />
              )}
            </View>
          </View>

          {/* Birthdate */}
          <View className="mb-8">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Birthdate</Text>
            <TouchableOpacity
              onPress={() => setShowDatePicker(true)}
              className={`rounded-2xl border bg-white px-4 py-4 ${
                errors.birthdate ? 'border-red-500' : 'border-gray-200'
              }`}
            >
              <Text className={`text-base ${birthdate ? 'text-gray-900' : 'text-gray-400'}`}>
                {birthdate ? birthdate.toLocaleDateString() : 'Select date'}
              </Text>
            </TouchableOpacity>
            {errors.birthdate ? (
              <Text className="mt-1 text-xs text-red-500">{errors.birthdate}</Text>
            ) : null}
            {showDatePicker && DateTimePicker && (
              <DateTimePicker
                value={birthdate || new Date()}
                mode="date"
                display="default"
                maximumDate={new Date()}
                onChange={(_, date) => {
                  setShowDatePicker(false);
                  if (date) { setBirthdate(date); setErrors((e) => ({ ...e, birthdate: '' })); }
                }}
              />
            )}
          </View>

          {/* Submit */}
          <TouchableOpacity
            onPress={handleSubmit}
            disabled={saving}
            className={`rounded-2xl py-4 items-center shadow-sm ${saving ? 'bg-gray-300' : 'bg-mountain-meadow-600'}`}
            activeOpacity={0.8}
          >
            {saving ? (
              <ActivityIndicator color="#FFFFFF" />
            ) : (
              <Text className="text-base font-bold text-white">Add Pet</Text>
            )}
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Type Select Modal */}
      <Modal visible={typeModalVisible} transparent animationType="slide">
        <View className="flex-1 justify-end bg-black/50">
          <View className="rounded-t-3xl bg-white p-6 pb-12">
            <View className="mb-4 flex-row items-center justify-between">
              <Text className="text-xl font-bold text-gray-900">Select Type</Text>
              <TouchableOpacity onPress={() => setTypeModalVisible(false)} className="p-2">
                <Ionicons name="close" size={24} color="#6B7280" />
              </TouchableOpacity>
            </View>
            {TYPE_OPTIONS.map((opt) => (
              <TouchableOpacity
                key={opt}
                className="border-b border-gray-100 py-4"
                onPress={() => { setType(opt); setTypeModalVisible(false); setErrors((e) => ({ ...e, type: '' })); }}
              >
                <Text className="text-base text-gray-800">{opt}</Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>
      </Modal>

      {/* Breed Select Modal */}
      <Modal visible={breedModalVisible} transparent animationType="slide">
        <View className="flex-1 justify-end bg-black/50">
          <View className="rounded-t-3xl bg-white p-6 pb-12" style={{ maxHeight: '70%' }}>
            <View className="mb-4 flex-row items-center justify-between">
              <Text className="text-xl font-bold text-gray-900">Select Breed</Text>
              <TouchableOpacity onPress={() => setBreedModalVisible(false)} className="p-2">
                <Ionicons name="close" size={24} color="#6B7280" />
              </TouchableOpacity>
            </View>
            <ScrollView showsVerticalScrollIndicator={false}>
              {breeds.map((opt) => (
                <TouchableOpacity
                  key={opt}
                  className="border-b border-gray-100 py-4"
                  onPress={() => { setBreed(opt); setBreedModalVisible(false); }}
                >
                  <Text className="text-base text-gray-800">{opt}</Text>
                </TouchableOpacity>
              ))}
            </ScrollView>
          </View>
        </View>
      </Modal>

      {/* Confirm Modal */}
      <Modal visible={confirmModalVisible} transparent animationType="fade">
        <View className="flex-1 items-center justify-center bg-black/50 px-6">
          <View className="w-full rounded-3xl bg-white p-6 items-center">
            <View className="mb-4 h-16 w-16 items-center justify-center rounded-full bg-mountain-meadow-100">
              <Ionicons name="paw" size={32} color="#059666" />
            </View>
            <Text className="mb-2 text-xl font-bold text-gray-900">Add {name}?</Text>
            <Text className="mb-6 text-center text-sm text-gray-500">
              Are you sure you want to add this pet to your profile?
            </Text>
            <View className="flex-row w-full gap-3">
              <TouchableOpacity
                onPress={() => setConfirmModalVisible(false)}
                className="flex-1 rounded-2xl bg-gray-100 py-3.5 items-center"
              >
                <Text className="text-base font-semibold text-gray-700">Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                onPress={handleConfirmAdd}
                className="flex-1 rounded-2xl bg-mountain-meadow-600 py-3.5 items-center shadow-sm"
              >
                <Text className="text-base font-bold text-white">Yes, Add Pet</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}
