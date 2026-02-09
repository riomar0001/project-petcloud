import React, { useState } from 'react';
import { Text, TextInput, View } from 'react-native';
import { AppInputProps } from '../../types';

export const AppInput: React.FC<AppInputProps> = ({ label, error, icon, rightIcon, containerStyle, ...textInputProps }) => {
  const [isFocused, setIsFocused] = useState(false);

  const borderColor = error ? 'border-red-400' : isFocused ? 'border-mountain-meadow-500' : 'border-gray-100';

  const bgColor = isFocused ? 'bg-white' : 'bg-gray-50';

  return (
    <View style={containerStyle} className="mb-4">
      {label && <Text className="mb-1.5 text-sm font-semibold tracking-wide text-gray-700">{label}</Text>}

      <View
        className={`
          flex-row items-center
          ${bgColor} rounded-xl border
          ${borderColor}
        `}
      >
        {icon && <View className="pl-3.5">{icon}</View>}

        <TextInput
          className={`flex-1 px-3.5 py-3.5 text-base text-gray-900 ${icon ? 'pl-2.5' : ''}`}
          placeholderTextColor="#9CA3AF"
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          {...textInputProps}
        />

        {rightIcon && <View className="pr-3.5">{rightIcon}</View>}
      </View>

      {error && (
        <View className="mt-1 flex-row items-center">
          <Text className="ml-0.5 text-xs text-red-500">{error}</Text>
        </View>
      )}
    </View>
  );
};
