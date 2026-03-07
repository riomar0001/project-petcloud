import axios, { AxiosInstance, AxiosError, AxiosRequestConfig, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { Platform } from 'react-native';

/**
 * API Configuration
 * - Use localhost for web
 * - Use emulator-friendly host for native (Android emulator: 10.0.2.2)
 * - If you run the app on a physical device, replace with your machine IP (e.g. http://192.168.x.x:5090)
 */
const API_BASE_URL = Platform.OS === 'web' ? 'http://localhost:5090' : 'http://192.168.0.176:5090';
const API_TIMEOUT = 30000;

/**
 * Custom API Error
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
    public errors?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

interface AuthHandlers {
  getRefreshToken: () => string | null;
  onTokensRefreshed: (accessToken: string, refreshToken: string) => void;
  onLogout: () => void;
}

/**
 * API Client
 */
class ApiClient {
  private instance: AxiosInstance;
  private token: string | null = null;
  private authHandlers: AuthHandlers | null = null;
  private isRefreshing = false;
  private refreshQueue: Array<(token: string) => void> = [];

  constructor() {
    this.instance = axios.create({
      baseURL: API_BASE_URL,
      timeout: API_TIMEOUT,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor
    this.instance.interceptors.request.use(
      (config) => {
        if (this.token) {
          config.headers.Authorization = `Bearer ${this.token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor
    this.instance.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        // Attempt silent token refresh on 401, except for auth endpoints
        if (
          error.response?.status === 401 &&
          !originalRequest?._retry &&
          this.authHandlers &&
          !originalRequest?.url?.includes('/auth/')
        ) {
          if (this.isRefreshing) {
            // Queue this request until the ongoing refresh completes
            return new Promise<AxiosResponse>((resolve, reject) => {
              this.refreshQueue.push((newToken: string) => {
                originalRequest.headers.Authorization = `Bearer ${newToken}`;
                resolve(this.instance(originalRequest));
              });
            });
          }

          originalRequest._retry = true;
          this.isRefreshing = true;

          const refreshToken = this.authHandlers.getRefreshToken();

          if (!refreshToken) {
            this.isRefreshing = false;
            this.authHandlers.onLogout();
            throw new ApiError('Session expired. Please log in again.', 401);
          }

          try {
            const res = await this.instance.post('/api/v1/auth/refresh', { refreshToken });
            const { accessToken: newAccess, refreshToken: newRefresh } = res.data.data;

            this.setToken(newAccess);
            this.authHandlers.onTokensRefreshed(newAccess, newRefresh);

            this.refreshQueue.forEach((cb) => cb(newAccess));
            this.refreshQueue = [];

            originalRequest.headers.Authorization = `Bearer ${newAccess}`;
            return this.instance(originalRequest);
          } catch {
            this.refreshQueue = [];
            this.authHandlers.onLogout();
            throw new ApiError('Session expired. Please log in again.', 401);
          } finally {
            this.isRefreshing = false;
          }
        }

        if (error.response) {
          const data = error.response.data as any;
          throw new ApiError(
            data?.message || 'Request failed',
            error.response.status,
            data?.errors
          );
        } else if (error.request) {
          throw new ApiError('Network error. Please check your connection.');
        } else {
          throw new ApiError(error.message || 'Request failed');
        }
      }
    );
  }

  setToken(token: string | null) {
    this.token = token;
  }

  setAuthHandlers(handlers: AuthHandlers) {
    this.authHandlers = handlers;
  }

  async get<T>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.instance.get<T>(url, config);
  }

  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.instance.post<T>(url, data, config);
  }

  async put<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.instance.put<T>(url, data, config);
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.instance.delete<T>(url, config);
  }

  async upload<T>(url: string, formData: FormData, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.instance.post<T>(url, formData, {
      ...config,
      headers: {
        'Content-Type': 'multipart/form-data',
        ...config?.headers,
      },
    });
  }
}

export const apiClient = new ApiClient();
