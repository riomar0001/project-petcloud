# API Services - Pure Axios + Zod

Clean, simple API layer with no Hey API dependency.

## Structure

```
services/api/
├── client.ts           # Axios client with interceptors
├── types.ts            # Shared types and utilities
├── auth/
│   ├── schemas.ts      # Zod validation schemas
│   └── service.ts      # Auth service methods
└── index.ts            # Public exports
```

## Usage

### Login

```typescript
import { AuthService, apiClient, ApiError } from '@/services/api';

try {
  const result = await AuthService.login({
    email: 'user@example.com',
    password: 'password123'
  });

  if (result.requires2FA) {
    // Handle 2FA
    console.log('2FA required for user:', result.twoFactorUserId);
  } else {
    // Store tokens
    apiClient.setToken(result.accessToken!);
    console.log('Access token:', result.accessToken);
  }
} catch (error) {
  if (error instanceof ApiError) {
    console.error(error.message);      // User-friendly message
    console.error(error.status);       // HTTP status code
    console.error(error.errors);       // Field validation errors
  }
}
```

### Register

```typescript
const result = await AuthService.register({
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  password: 'password123',
  phoneNumber: '+1234567890'  // optional
});
```

### 2FA Verification

```typescript
const tokens = await AuthService.verify2FA({
  userId: 123,
  code: '123456'
});

apiClient.setToken(tokens.accessToken);
```

### Password Reset

```typescript
// Request reset
await AuthService.forgotPassword({
  email: 'user@example.com'
});

// Reset with token
await AuthService.resetPassword({
  token: 'reset-token-from-email',
  newPassword: 'newPassword123'
});
```

### Token Refresh

```typescript
const tokens = await AuthService.refreshToken('your-refresh-token');
apiClient.setToken(tokens.accessToken);
```

### Logout

```typescript
await AuthService.logout();
apiClient.setToken(null);
```

## Features

### 1. **Automatic Token Injection**

```typescript
// Set token once
apiClient.setToken('your-jwt-token');

// All subsequent requests automatically include:
// Authorization: Bearer your-jwt-token
```

### 2. **Zod Validation**

```typescript
// Input validation
AuthService.login({
  email: 'invalid-email',  // Throws validation error
  password: '123'          // Throws validation error
});

// Response validation
// API returns unexpected data → Throws validation error
```

### 3. **Clean Error Handling**

```typescript
try {
  await AuthService.login(data);
} catch (error) {
  if (error instanceof ApiError) {
    // Network/API errors
    if (error.status === 401) {
      showError('Invalid credentials');
    } else if (error.errors) {
      // Field-specific errors
      Object.entries(error.errors).forEach(([field, messages]) => {
        console.log(`${field}: ${messages.join(', ')}`);
      });
    }
  }
}
```

### 4. **Response Unwrapping**

```typescript
// Your API returns: { success: true, message: "OK", data: {...} }
// Service returns: {...} (just the data!)

const result = await AuthService.login(data);
console.log(result.accessToken);  // Direct access!
```

## Adding New Services

### 1. Create Schemas

```typescript
// services/api/pets/schemas.ts
import { z } from 'zod';

export const createPetSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  species: z.enum(['Dog', 'Cat']),
  breed: z.string(),
});

export const petSchema = z.object({
  id: z.number(),
  name: z.string(),
  species: z.string(),
  breed: z.string(),
});

export type CreatePetRequest = z.infer<typeof createPetSchema>;
export type Pet = z.infer<typeof petSchema>;
```

### 2. Create Service

```typescript
// services/api/pets/service.ts
import { z } from 'zod';
import { apiClient } from '../client';
import { handleValidationError, ApiResponse } from '../types';
import { createPetSchema, petSchema, CreatePetRequest, Pet } from './schemas';

export class PetsService {
  static async getPets(): Promise<Pet[]> {
    try {
      const response = await apiClient.get<ApiResponse<Pet[]>>('/api/v1/pets');
      return z.array(petSchema).parse(response.data.data);
    } catch (error) {
      throw error;
    }
  }

  static async createPet(data: CreatePetRequest): Promise<Pet> {
    try {
      const validated = createPetSchema.parse(data);
      const response = await apiClient.post<ApiResponse<Pet>>('/api/v1/pets', validated);
      return petSchema.parse(response.data.data);
    } catch (error) {
      if (error instanceof z.ZodError) {
        throw handleValidationError(error);
      }
      throw error;
    }
  }
}
```

### 3. Export

```typescript
// services/api/index.ts
export { PetsService } from './pets/service';
export type { Pet, CreatePetRequest } from './pets/schemas';
```

## Configuration

Update API base URL in `client.ts`:

```typescript
const API_BASE_URL = 'https://your-api.com';
const API_TIMEOUT = 30000;
```

## Best Practices

1. **Always validate inputs and outputs with Zod**
2. **Use the ApiError class for consistent error handling**
3. **Keep schemas in separate files from services**
4. **Set tokens via apiClient.setToken() for authenticated requests**
5. **Handle errors in UI components, not in services**

## Result

**Before (Hey API):**

```typescript
const response = await getPets();
const pets = response.data?.data; // Nested confusion
```

**After (Pure Axios):**

```typescript
const pets = await PetsService.getPets(); // Clean and simple!
```

No more nested responses. No code generation. Just clean, simple TypeScript.
