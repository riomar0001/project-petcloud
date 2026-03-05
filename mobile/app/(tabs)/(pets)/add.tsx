import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, TextInput, Modal } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { Image } from 'expo-image';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import DateTimePicker from '@react-native-community/datetimepicker';
import { useAppStore } from '@/store/useAppStore';

const petSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  species: z.string().min(1, 'Species is required'),
  breed: z.string().min(1, 'Breed is required'),
  gender: z.string().min(1, 'Gender is required'),
  weight: z.string().min(1, 'Weight is required'),
  birthday: z.date({ message: 'Birthday is required' }),
});

type PetFormData = z.infer<typeof petSchema>;

const SPECIES_OPTIONS = ['Dog', 'Cat', 'Bird', 'Rabbit', 'Other'];
const GENDER_OPTIONS = ['Male', 'Female'];

export default function AddPetScreen() {
  const addPet = useAppStore((state) => state.addPet);
  const [photoUri, setPhotoUri] = useState<string | null>(null);

  // Modals state
  const [speciesModalVisible, setSpeciesModalVisible] = useState(false);
  const [genderModalVisible, setGenderModalVisible] = useState(false);
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [confirmModalVisible, setConfirmModalVisible] = useState(false);
  const [pendingPetData, setPendingPetData] = useState<PetFormData | null>(null);

  const {
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<PetFormData>({
    resolver: zodResolver(petSchema),
    defaultValues: {
      name: '',
      species: '',
      breed: '',
      gender: '',
      weight: '',
    },
  });

  const pickImage = async () => {
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled) {
      setPhotoUri(result.assets[0].uri);
    }
  };

  const onSubmit = (data: PetFormData) => {
    setPendingPetData(data);
    setConfirmModalVisible(true);
  };

  const handleConfirmAdd = () => {
    if (pendingPetData) {
      addPet({
        name: pendingPetData.name,
        species: pendingPetData.species,
        breed: pendingPetData.breed,
        gender: pendingPetData.gender,
        weight: parseFloat(pendingPetData.weight) || 0,
        birthday: pendingPetData.birthday.toISOString().split('T')[0],
        photoUri: photoUri || undefined,
      });
      setConfirmModalVisible(false);
      router.back();
    }
  };

  const SelectModal = ({ visible, onClose, options, onSelect, title }: any) => (
    <Modal visible={visible} transparent animationType="slide">
      <View className="flex-1 justify-end bg-black/50">
        <View className="rounded-t-3xl bg-white p-6 pb-12">
          <View className="mb-4 flex-row items-center justify-between">
            <Text className="text-xl font-bold text-gray-900">{title}</Text>
            <TouchableOpacity onPress={onClose} className="p-2">
              <Ionicons name="close" size={24} color="#6B7280" />
            </TouchableOpacity>
          </View>
          {options.map((opt: string) => (
            <TouchableOpacity
              key={opt}
              className="border-b border-gray-100 py-4"
              onPress={() => {
                onSelect(opt);
                onClose();
              }}
            >
              <Text className="text-base text-gray-800">{opt}</Text>
            </TouchableOpacity>
          ))}
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
          <Text className="text-xl font-bold text-gray-900">Add Pet</Text>
          <Text className="text-xs text-gray-400">Register a new companion</Text>
        </View>
      </View>

      <ScrollView showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled">
        <View className="px-6 pt-5 pb-8">
          {/* Photo Upload */}
          <View className="mb-5 items-center">
            <TouchableOpacity onPress={pickImage} activeOpacity={0.8}>
              <View className="h-24 w-24 overflow-hidden rounded-full border-2 border-dashed border-gray-300 bg-gray-50 items-center justify-center">
                {photoUri ? (
                  <Image source={{ uri: photoUri }} style={{ width: '100%', height: '100%' }} />
                ) : (
                  <Ionicons name="camera-outline" size={32} color="#9CA3AF" />
                )}
              </View>
            </TouchableOpacity>
            <Text className="mt-2 text-xs text-gray-400">Tap to add photo</Text>
          </View>

          {/* Pet Name */}
          <View className="mb-4">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Pet Name</Text>
            <Controller
              control={control}
              name="name"
              render={({ field: { onChange, onBlur, value } }) => (
                <TextInput
                  className={`rounded-2xl border bg-white px-4 py-4 text-base text-gray-900 ${errors.name ? 'border-red-500' : 'border-gray-200'
                    }`}
                  placeholder="e.g. Bella"
                  placeholderTextColor="#9CA3AF"
                  onBlur={onBlur}
                  onChangeText={onChange}
                  value={value}
                />
              )}
            />
            {errors.name && <Text className="mt-1 text-xs text-red-500">{errors.name.message}</Text>}
          </View>

          {/* Species & Breed */}
          <View className="mb-4 flex-row gap-3">
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Species</Text>
              <Controller
                control={control}
                name="species"
                render={({ field: { value } }) => (
                  <TouchableOpacity
                    onPress={() => setSpeciesModalVisible(true)}
                    className={`rounded-2xl border bg-white px-4 py-4 ${errors.species ? 'border-red-500' : 'border-gray-200'
                      }`}
                  >
                    <Text className={`text-base ${value ? 'text-gray-900' : 'text-gray-400'}`}>
                      {value || 'Select'}
                    </Text>
                  </TouchableOpacity>
                )}
              />
              {errors.species && <Text className="mt-1 text-xs text-red-500">{errors.species.message}</Text>}
            </View>
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Breed</Text>
              <Controller
                control={control}
                name="breed"
                render={({ field: { onChange, onBlur, value } }) => (
                  <TextInput
                    className={`rounded-2xl border bg-white px-4 py-4 text-base text-gray-900 ${errors.breed ? 'border-red-500' : 'border-gray-200'
                      }`}
                    placeholder="e.g. Golden Retriever"
                    placeholderTextColor="#9CA3AF"
                    onBlur={onBlur}
                    onChangeText={onChange}
                    value={value}
                  />
                )}
              />
              {errors.breed && <Text className="mt-1 text-xs text-red-500">{errors.breed.message}</Text>}
            </View>
          </View>

          {/* Gender & Birthday */}
          <View className="mb-4 flex-row gap-3">
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Gender</Text>
              <Controller
                control={control}
                name="gender"
                render={({ field: { value } }) => (
                  <TouchableOpacity
                    onPress={() => setGenderModalVisible(true)}
                    className={`rounded-2xl border bg-white px-4 py-4 ${errors.gender ? 'border-red-500' : 'border-gray-200'
                      }`}
                  >
                    <Text className={`text-base ${value ? 'text-gray-900' : 'text-gray-400'}`}>
                      {value || 'Select'}
                    </Text>
                  </TouchableOpacity>
                )}
              />
              {errors.gender && <Text className="mt-1 text-xs text-red-500">{errors.gender.message}</Text>}
            </View>
            <View className="flex-1">
              <Text className="mb-2 text-sm font-semibold text-gray-700">Birthday</Text>
              <Controller
                control={control}
                name="birthday"
                render={({ field: { value } }) => (
                  <>
                    <TouchableOpacity
                      onPress={() => setShowDatePicker(true)}
                      className={`rounded-2xl border bg-white px-4 py-4 ${errors.birthday ? 'border-red-500' : 'border-gray-200'
                        }`}
                    >
                      <Text className={`text-base ${value ? 'text-gray-900' : 'text-gray-400'}`}>
                        {value ? value.toLocaleDateString() : 'Select Date'}
                      </Text>
                    </TouchableOpacity>
                    {showDatePicker && (
                      <DateTimePicker
                        value={value || new Date()}
                        mode="date"
                        display="default"
                        maximumDate={new Date()}
                        onChange={(event, date) => {
                          setShowDatePicker(false);
                          if (date) setValue('birthday', date, { shouldValidate: true });
                        }}
                      />
                    )}
                  </>
                )}
              />
              {errors.birthday && <Text className="mt-1 text-xs text-red-500">{errors.birthday.message}</Text>}
            </View>
          </View>

          {/* Weight */}
          <View className="mb-8">
            <Text className="mb-2 text-sm font-semibold text-gray-700">Weight (kg)</Text>
            <Controller
              control={control}
              name="weight"
              render={({ field: { onChange, onBlur, value } }) => (
                <TextInput
                  className={`rounded-2xl border bg-white px-4 py-4 text-base text-gray-900 ${errors.weight ? 'border-red-500' : 'border-gray-200'
                    }`}
                  placeholder="e.g. 15.5"
                  placeholderTextColor="#9CA3AF"
                  keyboardType="numeric"
                  onBlur={onBlur}
                  onChangeText={onChange}
                  value={value}
                />
              )}
            />
            {errors.weight && <Text className="mt-1 text-xs text-red-500">{errors.weight.message}</Text>}
          </View>

          {/* Submit Button */}
          <TouchableOpacity
            onPress={handleSubmit(onSubmit)}
            className="rounded-2xl bg-[#059666] py-4 items-center shadow-sm"
            activeOpacity={0.8}
          >
            <Text className="text-base font-bold text-white">Add Pet</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Select Modals */}
      <SelectModal
        title="Select Species"
        visible={speciesModalVisible}
        onClose={() => setSpeciesModalVisible(false)}
        options={SPECIES_OPTIONS}
        onSelect={(val: string) => setValue('species', val, { shouldValidate: true })}
      />
      <SelectModal
        title="Select Gender"
        visible={genderModalVisible}
        onClose={() => setGenderModalVisible(false)}
        options={GENDER_OPTIONS}
        onSelect={(val: string) => setValue('gender', val, { shouldValidate: true })}
      />

      {/* Confirm Modal */}
      <Modal visible={confirmModalVisible} transparent animationType="fade">
        <View className="flex-1 items-center justify-center bg-black/50 px-6">
          <View className="w-full rounded-3xl bg-white p-6 items-center">
            <View className="mb-4 h-16 w-16 items-center justify-center rounded-full bg-mountain-meadow-100">
              <Ionicons name="paw" size={32} color="#059666" />
            </View>
            <Text className="mb-2 text-xl font-bold text-gray-900">Add {pendingPetData?.name}?</Text>
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
                className="flex-1 rounded-2xl bg-[#059666] py-3.5 items-center shadow-sm"
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
