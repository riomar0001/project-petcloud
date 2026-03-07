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
import { router, useLocalSearchParams } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { Image } from 'expo-image';

const DateTimePicker =
  Platform.OS !== 'web'
    ? require('@react-native-community/datetimepicker').default
    : null;
import { PetsService, ApiError } from '@/api';
import { resolveImageUrl } from '@/utils/imageUrl';

const TYPE_OPTIONS = ['Dog', 'Cat', 'Bird', 'Rabbit', 'Other'];

function formatDateForApi(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

type PhotoAsset = { uri: string; name: string; type: string };

export default function UpdatePetScreen() {
  const { petId } = useLocalSearchParams<{ petId: string }>();
  const id = Number(petId);

  const [initialLoading, setInitialLoading] = useState(true);
  const [name, setName] = useState('');
  const [type, setType] = useState('');
  const [breed, setBreed] = useState('');
  const [birthdate, setBirthdate] = useState<Date | null>(null);
  const [photo, setPhoto] = useState<PhotoAsset | null>(null);
  const [existingPhotoUrl, setExistingPhotoUrl] = useState<string | null>(null);

  const [breeds, setBreeds] = useState<string[]>([]);
  const [loadingBreeds, setLoadingBreeds] = useState(false);

  const [typeModalVisible, setTypeModalVisible] = useState(false);
  const [breedModalVisible, setBreedModalVisible] = useState(false);
  const [showDatePicker, setShowDatePicker] = useState(false);

  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3000);
  }, []);

  // Load existing pet data
  useEffect(() => {
    (async () => {
      try {
        const detail = await PetsService.getPetDetail(id);
        setName(detail.name);
        setType(detail.type);
        setBreed(detail.breed);
        setBirthdate(new Date(detail.birthdate));
        setExistingPhotoUrl(resolveImageUrl(detail.photoUrl));
      } catch {
        showToast('Failed to load pet data', false);
      } finally {
        setInitialLoading(false);
      }
    })();
  }, [id]);

  // Fetch breeds when type changes to Dog or Cat
  useEffect(() => {
    if (type === 'Dog' || type === 'Cat') {
      setLoadingBreeds(true);
      setBreeds([]);
      PetsService.getBreeds(type.toLowerCase())
        .then(setBreeds)
        .catch(() => setBreeds([]))
        .finally(() => setLoadingBreeds(false));
    } else {
      setBreeds([]);
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

  const handleSave = async () => {
    if (!validate()) return;
    setSaving(true);
    try {
      await PetsService.updatePet(
        id,
        {
          Name: name.trim(),
          Type: type,
          Breed: breed.trim() || undefined,
          Birthdate: formatDateForApi(birthdate!),
        },
        photo ?? undefined
      );
      showToast('Pet updated successfully!', true);
      setTimeout(() => router.back(), 1000);
    } catch (error: any) {
      showToast(
        error instanceof ApiError ? error.message : 'Failed to update pet',
        false
      );
    } finally {
      setSaving(false);
    }
  };

  if (initialLoading) {
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
          <Text className="text-xl font-bold text-gray-900">Edit Pet</Text>
        </View>
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      </SafeAreaView>
    );
  }

  const previewUri = photo?.uri ?? existingPhotoUrl;
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
          <Text className="text-xl font-bold text-gray-900">Edit Pet</Text>
          <Text className="text-xs text-gray-400">Update pet information</Text>
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
          {/* Photo */}
          <View className="mb-5 items-center">
            <TouchableOpacity onPress={pickImage} activeOpacity={0.8}>
              <View className="h-24 w-24 overflow-hidden rounded-full border-2 border-dashed border-gray-300 bg-gray-50 items-center justify-center">
                {previewUri ? (
                  <Image source={{ uri: previewUri }} style={{ width: '100%', height: '100%' }} contentFit="cover" />
                ) : (
                  <Ionicons name="camera-outline" size={32} color="#9CA3AF" />
                )}
              </View>
            </TouchableOpacity>
            <Text className="mt-2 text-xs text-gray-400">Tap to change photo</Text>
          </View>

          {/* Name */}
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
                 onChange={(_: any, date: Date | undefined) => {
                  setShowDatePicker(false);
                  if (date) { setBirthdate(date); setErrors((e) => ({ ...e, birthdate: '' })); }
                }}
              />
            )}
          </View>

          {/* Save */}
          <TouchableOpacity
            onPress={handleSave}
            disabled={saving}
            className={`rounded-2xl py-4 items-center shadow-sm ${saving ? 'bg-gray-300' : 'bg-mountain-meadow-600'}`}
            activeOpacity={0.8}
          >
            {saving ? (
              <ActivityIndicator color="#FFFFFF" />
            ) : (
              <Text className="text-base font-bold text-white">Save Changes</Text>
            )}
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Type Modal */}
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
                onPress={() => {
                  setType(opt);
                  setTypeModalVisible(false);
                  setErrors((e) => ({ ...e, type: '' }));
                }}
              >
                <Text className="text-base text-gray-800">{opt}</Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>
      </Modal>

      {/* Breed Modal */}
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
    </SafeAreaView>
  );
}
