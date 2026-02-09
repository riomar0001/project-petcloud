import React from 'react';
import { TouchableOpacity, Text, ActivityIndicator, View } from 'react-native';
import { AppButtonProps } from '../types';

export const AppButton: React.FC<AppButtonProps> = ({
  title,
  onPress,
  variant = 'primary',
  loading = false,
  disabled = false,
  icon,
  containerStyle,
  size = 'md',
}) => {
  const isDisabled = disabled || loading;

  const sizeStyles = {
    sm: 'py-2.5 px-4',
    md: 'py-4 px-6',
    lg: 'py-5 px-8',
  };

  const textSizes = {
    sm: 'text-sm',
    md: 'text-base',
    lg: 'text-lg',
  };

  const getButtonStyles = (): string => {
    const base = `rounded-2xl items-center justify-center flex-row ${sizeStyles[size]}`;

    if (isDisabled) {
      return `${base} bg-gray-200`;
    }

    switch (variant) {
      case 'primary':
        return `${base} bg-mountain-meadow-600`;
      case 'secondary':
        return `${base} bg-mountain-meadow-100`;
      case 'outline':
        return `${base} border-2 border-mountain-meadow-600 bg-transparent`;
      case 'ghost':
        return `${base} bg-transparent`;
      default:
        return `${base} bg-mountain-meadow-600`;
    }
  };

  const getTextStyles = (): string => {
    const base = `font-semibold ${textSizes[size]}`;

    if (isDisabled) {
      return `${base} text-gray-400`;
    }

    switch (variant) {
      case 'primary':
        return `${base} text-white`;
      case 'secondary':
        return `${base} text-mountain-meadow-700`;
      case 'outline':
        return `${base} text-mountain-meadow-600`;
      case 'ghost':
        return `${base} text-mountain-meadow-600`;
      default:
        return `${base} text-white`;
    }
  };

  const spinnerColor = variant === 'primary' ? '#FFFFFF' : '#059666';

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={isDisabled}
      activeOpacity={0.8}
      style={containerStyle}
      className={getButtonStyles()}
    >
      {loading ? (
        <ActivityIndicator color={spinnerColor} size="small" />
      ) : (
        <>
          {icon && <View className="mr-2">{icon}</View>}
          <Text className={getTextStyles()}>{title}</Text>
        </>
      )}
    </TouchableOpacity>
  );
};
