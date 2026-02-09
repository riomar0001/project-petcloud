import { Tabs } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { Platform, View } from 'react-native';

type TabIconProps = {
  name: keyof typeof Ionicons.glyphMap;
  color: string;
  focused: boolean;
};

function TabIcon({ name, color, focused }: TabIconProps) {
  return (
    <View className="items-center">
      <Ionicons name={name} size={22} color={color} />
      {focused && (
        <View className="mt-1 h-1 w-1 rounded-full bg-mountain-meadow-600" />
      )}
    </View>
  );
}

export default function TabLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: '#059666',
        tabBarInactiveTintColor: '#9CA3AF',
        tabBarLabelStyle: {
          fontSize: 11,
          fontWeight: '600',
          marginTop: -2,
        },
        tabBarStyle: {
          backgroundColor: '#FFFFFF',
          borderTopWidth: 1,
          borderTopColor: '#F3F4F6',
          height: Platform.OS === 'ios' ? 88 : 64,
          paddingTop: 8,
          paddingBottom: Platform.OS === 'ios' ? 28 : 10,
          elevation: 8,
          shadowColor: '#000000',
          shadowOffset: { width: 0, height: -4 },
          shadowOpacity: 0.06,
          shadowRadius: 12,
        },
      }}
    >
      <Tabs.Screen
        name="dashboard"
        options={{
          title: 'Home',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon name={focused ? 'home' : 'home-outline'} color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="(appointments)"
        options={{
          title: 'Appointments',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon name={focused ? 'calendar' : 'calendar-outline'} color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="(pets)"
        options={{
          title: 'My Pets',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon name={focused ? 'paw' : 'paw-outline'} color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Profile',
          tabBarIcon: ({ color, focused }) => (
            <TabIcon name={focused ? 'person' : 'person-outline'} color={color} focused={focused} />
          ),
        }}
      />
    </Tabs>
  );
}
