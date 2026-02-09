import React from 'react';
import { View, Text } from 'react-native';
import { Ionicons } from '@expo/vector-icons';

interface ProgressStepsProps {
  currentStep: number;
  totalSteps: number;
  labels: string[];
}

export const ProgressSteps: React.FC<ProgressStepsProps> = ({
  currentStep,
  totalSteps,
  labels,
}) => {
  return (
    <View className="mb-8">
      <View className="flex-row items-center justify-between px-2">
        {Array.from({ length: totalSteps }, (_, i) => {
          const stepNum = i + 1;
          const isCompleted = stepNum < currentStep;
          const isActive = stepNum === currentStep;
          const isLast = stepNum === totalSteps;

          return (
            <React.Fragment key={stepNum}>
              {/* Step circle */}
              <View className="items-center">
                <View
                  className={`h-10 w-10 items-center justify-center rounded-full ${
                    isCompleted
                      ? 'bg-mountain-meadow-600'
                      : isActive
                        ? 'bg-mountain-meadow-600'
                        : 'bg-gray-200'
                  }`}
                >
                  {isCompleted ? (
                    <Ionicons name="checkmark" size={20} color="#FFFFFF" />
                  ) : (
                    <Text
                      className={`text-sm font-bold ${
                        isActive ? 'text-white' : 'text-gray-400'
                      }`}
                    >
                      {stepNum}
                    </Text>
                  )}
                </View>
                <Text
                  className={`mt-1.5 text-xs font-medium ${
                    isActive || isCompleted ? 'text-mountain-meadow-700' : 'text-gray-400'
                  }`}
                >
                  {labels[i]}
                </Text>
              </View>

              {/* Connector line */}
              {!isLast && (
                <View
                  className={`mx-1 h-0.5 flex-1 ${
                    isCompleted ? 'bg-mountain-meadow-500' : 'bg-gray-200'
                  }`}
                  style={{ marginBottom: 18 }}
                />
              )}
            </React.Fragment>
          );
        })}
      </View>
    </View>
  );
};
