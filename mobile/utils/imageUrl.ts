import { Platform } from 'react-native';

const API_BASE_URL =
  Platform.OS === 'web' ? 'http://localhost:5090' : 'http://192.168.1.42:5090';

/**
 * Resolves a potentially relative image path to a full URL.
 * If the path is already absolute (starts with http), returns it as-is.
 */
export function resolveImageUrl(path: string | null | undefined): string | null {
  if (!path) return null;
  if (path.startsWith('http')) return path;
  return `${API_BASE_URL}${path}`;
}
