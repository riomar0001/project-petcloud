import React, { useCallback, useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useFocusEffect } from 'expo-router';
import { NotificationsService } from '@/api';
import type { NotificationDto } from '@/api';
import { useNotificationStore } from '@/store/useNotificationStore';

function formatRelative(iso: string): string {
  const now = Date.now();
  const diff = now - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'Just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  if (days < 7) return `${days}d ago`;
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function typeIcon(
  type: string
): keyof typeof import('@expo/vector-icons').Ionicons.glyphMap {
  const t = type.toLowerCase();
  if (t.includes('appointment')) return 'calendar';
  if (t.includes('vaccine') || t.includes('vaccination')) return 'medical';
  if (t.includes('deworm')) return 'medkit';
  if (t.includes('reminder')) return 'alarm';
  return 'notifications';
}

function typeColor(type: string): string {
  const t = type.toLowerCase();
  if (t.includes('appointment')) return '#059666';
  if (t.includes('vaccine')) return '#3B82F6';
  if (t.includes('deworm')) return '#F59E0B';
  if (t.includes('reminder')) return '#8B5CF6';
  return '#6B7280';
}

export default function NotificationsScreen() {
  const { setUnreadCount, reset } = useNotificationStore();
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [markingAll, setMarkingAll] = useState(false);
  const [unread, setUnread] = useState(0);

  const load = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const data = await NotificationsService.listNotifications({ pageSize: 50 });
      setNotifications(data.items);
      setUnread(data.unreadCount);
      setUnreadCount(data.unreadCount);
    } catch {
      // silently fail
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [setUnreadCount]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  const handleMarkAsRead = async (n: NotificationDto) => {
    if (n.isRead) return;
    try {
      await NotificationsService.markAsRead(n.notificationId);
      setNotifications((prev) =>
        prev.map((item) =>
          item.notificationId === n.notificationId ? { ...item, isRead: true } : item
        )
      );
      setUnread((prev) => Math.max(0, prev - 1));
      setUnreadCount(Math.max(0, unread - 1));
    } catch {
      // silently fail
    }
  };

  const handleMarkAllAsRead = async () => {
    setMarkingAll(true);
    try {
      await NotificationsService.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnread(0);
      reset();
    } catch {
      // silently fail
    } finally {
      setMarkingAll(false);
    }
  };

  return (
    <SafeAreaView className="flex-1 bg-gray-50" edges={['top']}>
      {/* Header */}
      <View className="flex-row items-center justify-between bg-white px-6 pb-4 pt-4">
        <View>
          <Text className="text-2xl font-bold text-gray-900">Notifications</Text>
          <Text className="mt-0.5 text-sm text-gray-400">Stay updated with your pets</Text>
        </View>
        {unread > 0 && (
          <TouchableOpacity
            onPress={handleMarkAllAsRead}
            disabled={markingAll}
            className="flex-row items-center rounded-xl bg-mountain-meadow-50 px-3 py-2"
            activeOpacity={0.7}
          >
            {markingAll ? (
              <ActivityIndicator size="small" color="#059666" />
            ) : (
              <>
                <Ionicons name="checkmark-done" size={16} color="#059666" />
                <Text className="ml-1.5 text-xs font-semibold text-mountain-meadow-700">
                  Mark all read
                </Text>
              </>
            )}
          </TouchableOpacity>
        )}
      </View>

      {loading ? (
        <View className="flex-1 items-center justify-center">
          <ActivityIndicator size="large" color="#059666" />
        </View>
      ) : (
        <ScrollView
          showsVerticalScrollIndicator={false}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(true); }}
              tintColor="#059666"
            />
          }
        >
          <View className="px-6 pt-4 pb-8">
            {notifications.length === 0 ? (
              <View className="items-center rounded-2xl border border-dashed border-gray-200 bg-white py-16">
                <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-mountain-meadow-50">
                  <Ionicons name="notifications-outline" size={40} color="#059666" />
                </View>
                <Text className="text-lg font-semibold text-gray-900">No notifications yet</Text>
                <Text className="mt-1 text-center text-sm text-gray-400">
                  Appointment reminders and updates{'\n'}will appear here
                </Text>
              </View>
            ) : (
              notifications.map((n, i) => {
                const color = typeColor(n.type);
                return (
                  <TouchableOpacity
                    key={n.notificationId}
                    onPress={() => handleMarkAsRead(n)}
                    activeOpacity={0.8}
                    className={`mb-3 rounded-2xl border p-4 ${
                      n.isRead
                        ? 'border-gray-100 bg-white'
                        : 'border-mountain-meadow-200 bg-mountain-meadow-50'
                    }`}
                  >
                    <View className="flex-row items-start">
                      <View
                        className="mr-3 h-10 w-10 items-center justify-center rounded-full"
                        style={{ backgroundColor: color + '20' }}
                      >
                        <Ionicons name={typeIcon(n.type)} size={20} color={color} />
                      </View>
                      <View className="flex-1">
                        <View className="flex-row items-start justify-between">
                          <Text
                            className={`flex-1 text-sm leading-5 ${
                              n.isRead
                                ? 'font-normal text-gray-600'
                                : 'font-semibold text-gray-900'
                            }`}
                          >
                            {n.message}
                          </Text>
                          {!n.isRead && (
                            <View className="ml-2 mt-1 h-2 w-2 rounded-full bg-mountain-meadow-500" />
                          )}
                        </View>
                        <View className="mt-1.5 flex-row items-center">
                          <Text className="text-xs text-gray-400">{formatRelative(n.createdAt)}</Text>
                          <Text className="mx-1.5 text-xs text-gray-300">·</Text>
                          <Text className="text-xs text-gray-400">{n.type}</Text>
                        </View>
                      </View>
                    </View>
                  </TouchableOpacity>
                );
              })
            )}
          </View>
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
