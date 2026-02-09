import axios, { AxiosInstance, AxiosError, AxiosRequestConfig, AxiosResponse } from 'axios';

/**
 * API Configuration
 */
const API_BASE_URL = 'https://localhost:7055';
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

/**
 * API Client
 */
class ApiClient {
  private instance: AxiosInstance;
  private token: string | null = null;

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
      (error: AxiosError) => {
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
