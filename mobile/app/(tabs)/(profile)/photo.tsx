import React, { useState, useCallback } from 'react';
import { View, Text, TouchableOpacity, ActivityIndicator } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import * as ImagePicker from 'expo-image-picker';
import { ProfileService } from '@/api';
import { AppButton } from '@/components/ui/button';

export default function UpdatePhotoScreen() {
  const [selectedImage, setSelectedImage] = useState<{
    uri: string;
    name: string;
    type: string;
  } | null>(null);
  const [uploading, setUploading] = useState(false);
  const [toast, setToast] = useState<{ message: string; success: boolean } | null>(null);

  const showToast = useCallback((message: string, success: boolean) => {
    setToast({ message, success });
    setTimeout(() => setToast(null), 3000);
  }, []);

  const pickImage = async () => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      showToast('Permission to access photos is required.', false);
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled && result.assets[0]) {
      const asset = result.assets[0];
      const uri = asset.uri;
      const name = uri.split('/').pop() || 'photo.jpg';
      const type = asset.mimeType || 'image/jpeg';
      setSelectedImage({ uri, name, type });
    }
  };

  const takePhoto = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') {
      showToast('Permission to access camera is required.', false);
      return;
    }

    const result = await ImagePicker.launchCameraAsync({
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled && result.assets[0]) {
      const asset = result.assets[0];
      const uri = asset.uri;
      const name = uri.split('/').pop() || 'photo.jpg';
      const type = asset.mimeType || 'image/jpeg';
      setSelectedImage({ uri, name, type });
    }
  };

  const handleUpload = async () => {
    if (!selectedImage) return;

    setUploading(true);
    try {
      await ProfileService.updatePhoto(selectedImage);
      showToast('Profile photo updated!', true);
      setTimeout(() => router.back(), 1000);
    } catch (error: any) {
      showToast(error.message || 'Failed to upload photo', false);
    } finally {
      setUploading(false);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center bg-white px-4 pb-4 pt-4">
        <TouchableOpacity
          onPress={() => router.back()}
          className="mr-3 h-10 w-10 items-center justify-center rounded-xl bg-gray-100"
          activeOpacity={0.7}
        >
          <Ionicons name="arrow-back" size={20} color="#374151" />
        </TouchableOpacity>
        <View>
          <Text className="text-xl font-bold text-gray-900">Update Photo</Text>
          <Text className="text-xs text-gray-400">Choose a new profile picture</Text>
        </View>
      </View>

      {/* Toast */}
      {toast && (
        <View className="px-6 pt-3">
          <View
            className={`flex-row items-center rounded-xl px-4 py-3 ${
              toast.success
                ? 'border border-green-200 bg-green-50'
                : 'border border-red-200 bg-red-50'
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

      <View className="flex-1 px-6 pt-5">
        {/* Preview */}
        <View className="items-center rounded-2xl border border-gray-100 bg-white p-8">
          {selectedImage ? (
            <Image
              source={{ uri: selectedImage.uri }}
              style={{ width: 160, height: 160, borderRadius: 80 }}
              contentFit="cover"
            />
          ) : (
            <View className="h-40 w-40 items-center justify-center rounded-full bg-gray-100">
              <Ionicons name="person" size={64} color="#9CA3AF" />
            </View>
          )}
          <Text className="mt-4 text-sm text-gray-400">
            {selectedImage ? 'Looking good! Upload when ready.' : 'Select a photo to preview'}
          </Text>
        </View>

        {/* Actions */}
        <View className="mt-5 flex-row gap-3">
          <TouchableOpacity
            onPress={pickImage}
            className="flex-1 flex-row items-center justify-center rounded-2xl border border-gray-100 bg-white py-4"
            activeOpacity={0.7}
          >
            <Ionicons name="images-outline" size={20} color="#059666" />
            <Text className="ml-2 text-sm font-semibold text-gray-900">Gallery</Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={takePhoto}
            className="flex-1 flex-row items-center justify-center rounded-2xl border border-gray-100 bg-white py-4"
            activeOpacity={0.7}
          >
            <Ionicons name="camera-outline" size={20} color="#059666" />
            <Text className="ml-2 text-sm font-semibold text-gray-900">Camera</Text>
          </TouchableOpacity>
        </View>

        {/* Upload Button */}
        {selectedImage && (
          <View className="mt-5">
            <AppButton
              title="Upload Photo"
              onPress={handleUpload}
              loading={uploading}
              variant="primary"
              icon={
                !uploading ? (
                  <Ionicons name="cloud-upload-outline" size={16} color="#FFFFFF" />
                ) : undefined
              }
            />
          </View>
        )}
      </View>
    </SafeAreaView>
  );
}
