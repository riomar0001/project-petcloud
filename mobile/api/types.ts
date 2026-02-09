import {  ZodError } from 'zod';
import { ApiError } from './client';

/**
 * Standard API Response wrapper from backend
 */
export interface ApiResponse<T> {
  success?: boolean;
  message?: string | null;
  data?: T;
}

/**
 * Unwrap API response and handle errors
 */
export function unwrapResponse<T>(response: ApiResponse<T>): T {
  if (!response.success || response.data === undefined) {
    throw new ApiError(response.message || 'Request failed');
  }
  return response.data;
}

/**
 * Handle Zod validation errors
 */
export function handleValidationError(error: ZodError<any>): ApiError {
  const errorMap = error.issues.reduce((acc, issue) => {
    const path = issue.path.join('.');
    if (!acc[path]) {
      acc[path] = [];
    }
    acc[path].push(issue.message);
    return acc;
  }, {} as Record<string, string[]>);

  return new ApiError(
    error.issues[0]?.message || 'Validation error',
    400,
    errorMap
  );
}
