# PetCloud Mobile API Documentation

## Base URL

```
https://api.yourserver.com/api/v1
```

All API endpoints are prefixed with `/api/v1/`.

## Authentication

The API uses **JWT Bearer Token** authentication.

### Headers

Include the following header in authenticated requests:

```
Authorization: Bearer <access_token>
```

### Token Expiration

- **Access Token**: 60 minutes
- **Refresh Token**: 30 days

Use the `/api/v1/auth/refresh` endpoint to obtain a new access token before expiration.

---

## API Response Format

### Success Response

```json
{
  "success": true,
  "message": "Optional message",
  "data": { /* response data */ }
}
```

### Error Response

```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Validation error 1", "Validation error 2"]
}
```

---

## Endpoints

## Authentication

### 1. Register

Create a new pet owner account.

**Endpoint:** `POST /auth/register`

**Auth Required:** No

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "09171234567",
  "password": "MySecureP@ss123",
  "confirmPassword": "MySecureP@ss123"
}
```

**Validation Rules:**
- `firstName`, `lastName`: Max 50 chars, letters/spaces/hyphens only
- `email`: Valid email format
- `phone`: Exactly 11 digits
- `password`: Minimum 8 characters
- `confirmPassword`: Must match password

**Success Response:** `201 Created`
```json
{
  "success": true,
  "message": "Registration successful!"
}
```

**Error Responses:**
- `409 Conflict`: Email already exists
- `400 Bad Request`: Validation errors

---

### 2. Login

Authenticate and obtain JWT tokens.

**Endpoint:** `POST /auth/login`

**Auth Required:** No

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "password": "MySecureP@ss123",
  "deviceInfo": "iPhone 14 Pro"
}
```

**Success Response (No 2FA):** `200 OK`
```json
{
  "success": true,
  "data": {
    "requires2FA": false,
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "a1b2c3d4e5f6...",
    "expiresAt": "2026-02-09T14:30:00Z",
    "owner": {
      "userId": 5,
      "ownerId": 3,
      "firstName": "John",
      "lastName": "Doe",
      "email": "john.doe@example.com",
      "profileImage": "/uploads/users/pet.png"
    }
  }
}
```

**Success Response (2FA Required):** `200 OK`
```json
{
  "success": true,
  "message": "2FA code sent to your email.",
  "data": {
    "requires2FA": true,
    "twoFactorUserId": 5
  }
}
```

**Error Responses:**
- `401 Unauthorized`: Invalid credentials, account disabled
- `429 Too Many Requests`: Account locked (after 5 failed attempts)

**Notes:**
- 2FA is triggered when:
  - First login or no 2FA verification in last 30 days
  - New device or IP address detected
- Account locked for 3 minutes after 5 failed login attempts
- Admin/Staff users cannot login via mobile API

---

### 3. Verify 2FA

Complete two-factor authentication with email code.

**Endpoint:** `POST /auth/verify-2fa`

**Auth Required:** No

**Request Body:**
```json
{
  "userId": 5,
  "code": "123456",
  "deviceInfo": "iPhone 14 Pro"
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "a1b2c3d4e5f6...",
    "expiresAt": "2026-02-09T14:30:00Z",
    "owner": {
      "userId": 5,
      "ownerId": 3,
      "firstName": "John",
      "lastName": "Doe",
      "email": "john.doe@example.com",
      "profileImage": "/uploads/users/pet.png"
    }
  }
}
```

**Error Responses:**
- `401 Unauthorized`: Invalid or expired code
- `404 Not Found`: User not found

**Notes:**
- 2FA codes expire after 10 minutes
- 2FA codes are 6 digits

---

### 4. Refresh Token

Obtain a new access token using refresh token.

**Endpoint:** `POST /auth/refresh`

**Auth Required:** No

**Request Body:**
```json
{
  "refreshToken": "a1b2c3d4e5f6..."
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "g7h8i9j0k1l2...",
    "expiresAt": "2026-02-09T15:30:00Z"
  }
}
```

**Error Responses:**
- `401 Unauthorized`: Invalid, expired, or revoked token

**Notes:**
- Old refresh token is revoked (token rotation)
- Reuse detection: using a revoked refresh token invalidates all tokens for that device

---

### 5. Logout

Revoke refresh token and end session.

**Endpoint:** `POST /auth/logout`

**Auth Required:** Yes (Bearer token)

**Request Body:**
```json
{
  "refreshToken": "a1b2c3d4e5f6..."
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Logged out successfully."
}
```

---

### 6. Forgot Password

Request password reset link via email.

**Endpoint:** `POST /auth/forgot-password`

**Auth Required:** No

**Request Body:**
```json
{
  "email": "john.doe@example.com"
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Reset link sent! Please check your email."
}
```

**Error Responses:**
- `404 Not Found`: Email not found

**Notes:**
- Reset link expires in 1 hour
- Email contains web portal reset link

---

### 7. Reset Password

Set new password using reset token.

**Endpoint:** `POST /auth/reset-password`

**Auth Required:** No

**Request Body:**
```json
{
  "token": "guid-token-from-email",
  "newPassword": "NewSecureP@ss456",
  "confirmPassword": "NewSecureP@ss456"
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Password reset successful! You can now log in."
}
```

**Error Responses:**
- `400 Bad Request`: Invalid/expired token, new password same as old

---

## Dashboard

### 1. Get Dashboard

Retrieve owner's dashboard with pets, appointments, and due items.

**Endpoint:** `GET /dashboard`

**Auth Required:** Yes (Owner role)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "userName": "John Doe",
    "pets": [
      {
        "petId": 10,
        "name": "Max",
        "breed": "Golden Retriever",
        "photoUrl": "https://api.yourserver.com/uploads/pets/max.jpg",
        "birthdate": "2022-05-15T00:00:00Z"
      }
    ],
    "upcomingAppointments": [
      {
        "appointmentId": 45,
        "appointmentDate": "2026-02-15T10:00:00Z",
        "status": "Pending",
        "petId": 10,
        "petName": "Max",
        "serviceType": "Vaccination"
      }
    ],
    "vaccineDue": [
      {
        "appointmentId": 46,
        "dueDate": "2026-02-12T00:00:00Z",
        "petId": 10,
        "petName": "Max",
        "serviceType": "Rabies Vaccination"
      }
    ],
    "dewormDue": []
  }
}
```

**Notes:**
- Returns vaccination/deworming items due within 5 days
- Upcoming appointments include pending and future appointments

---

## Pets

### 1. List All Pets

Get all pets belonging to authenticated owner.

**Endpoint:** `GET /pets`

**Auth Required:** Yes (Owner role)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "petId": 10,
      "name": "Max",
      "type": "Dog",
      "breed": "Golden Retriever",
      "birthdate": "2022-05-15T00:00:00Z",
      "photoUrl": "https://api.yourserver.com/uploads/pets/max.jpg",
      "age": "3 year(s) old",
      "createdAt": "2023-01-10T08:30:00Z"
    }
  ]
}
```

---

### 2. Get Pet Details

Retrieve detailed information for a specific pet including appointment history.

**Endpoint:** `GET /pets/{id}`

**Auth Required:** Yes (Owner role)

**Query Parameters:**
- `page` (int, default: 1): Page number for appointments
- `pageSize` (int, default: 5): Items per page
- `search` (string, optional): Filter appointments by notes text
- `categoryFilter` (int, optional): Filter by service category ID

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "petId": 10,
    "name": "Max",
    "type": "Dog",
    "breed": "Golden Retriever",
    "birthdate": "2022-05-15T00:00:00Z",
    "sex": "Male",
    "color": "Golden",
    "photoUrl": "https://api.yourserver.com/uploads/pets/max.jpg",
    "age": "3 year(s) old",
    "ownerName": "John Doe",
    "appointments": {
      "items": [
        {
          "appointmentId": 45,
          "appointmentDate": "2026-01-15T10:00:00Z",
          "serviceType": "Vaccination",
          "serviceSubtype": "Rabies",
          "status": "Completed",
          "notes": "Annual rabies vaccination administered",
          "dueDate": "2027-01-15T00:00:00Z"
        }
      ],
      "page": 1,
      "pageSize": 5,
      "totalPages": 2,
      "totalItems": 8
    }
  }
}
```

**Error Responses:**
- `404 Not Found`: Pet not found
- `403 Forbidden`: Pet does not belong to authenticated owner

---

### 3. Get Pet Card

Retrieve or generate PDF pet health card.

**Endpoint:** `GET /pets/{id}/card`

**Auth Required:** Yes (Owner role)

**Query Parameters:**
- `regenerate` (bool, default: false): Force regenerate PDF

**Success Response:** `200 OK`
Returns PDF file with headers:
```
Content-Type: application/pdf
Content-Disposition: inline; filename="Pet_Card_Max_10.pdf"
```

**Error Responses:**
- `404 Not Found`: Pet not found
- `403 Forbidden`: Pet does not belong to authenticated owner
- `500 Internal Server Error`: PDF generation failed

**Notes:**
- PDF is A5 size with QR code
- Includes vaccination and deworming records
- Cached PDFs are reused unless `regenerate=true`

---

### 4. Get Breed List

Retrieve available breeds for pet type.

**Endpoint:** `GET /pets/breeds`

**Auth Required:** Yes (Owner role)

**Query Parameters:**
- `type` (string, required): "Dog" or "Cat"

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": ["Golden Retriever", "Labrador", "Beagle", "..."]
}
```

**Notes:**
- Breeds loaded from CSV files in `App_Data/`

---

## Appointments

### 1. Get Available Slots

Retrieve available appointment time slots for a date.

**Endpoint:** `GET /appointments/available-slots`

**Auth Required:** Yes (Owner role)

**Query Parameters:**
- `date` (string, required): Date in ISO format (YYYY-MM-DD)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "date": "2026-02-15",
    "slots": [
      {
        "time": "09:00",
        "isAvailable": true
      },
      {
        "time": "10:00",
        "isAvailable": false
      }
    ]
  }
}
```

---

### 2. Book Appointment

Create a new appointment booking.

**Endpoint:** `POST /appointments`

**Auth Required:** Yes (Owner role)

**Request Body:**
```json
{
  "petId": 10,
  "categoryId": 3,
  "subtypeId": 8,
  "appointmentDate": "2026-02-15T10:00:00Z",
  "notes": "First vaccination for puppy"
}
```

**Success Response:** `201 Created`
```json
{
  "success": true,
  "message": "Appointment booked successfully!",
  "data": {
    "appointmentId": 50
  }
}
```

**Error Responses:**
- `400 Bad Request`: Slot unavailable, validation errors
- `404 Not Found`: Pet not found

---

### 3. Get My Appointments

Retrieve all appointments for authenticated owner.

**Endpoint:** `GET /appointments`

**Auth Required:** Yes (Owner role)

**Query Parameters:**
- `status` (string, optional): Filter by status (Pending, Completed, Cancelled)
- `page` (int, default: 1)
- `pageSize` (int, default: 10)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "appointmentId": 45,
        "appointmentDate": "2026-02-15T10:00:00Z",
        "petName": "Max",
        "serviceType": "Vaccination",
        "status": "Pending"
      }
    ],
    "page": 1,
    "totalPages": 3,
    "totalItems": 25
  }
}
```

---

### 4. Cancel Appointment

Cancel a pending appointment.

**Endpoint:** `DELETE /appointments/{id}`

**Auth Required:** Yes (Owner role)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Appointment cancelled successfully."
}
```

**Error Responses:**
- `404 Not Found`: Appointment not found
- `403 Forbidden`: Not your appointment
- `400 Bad Request`: Cannot cancel completed/cancelled appointment

---

## Notifications

### 1. Get Notifications

Retrieve notifications for authenticated owner.

**Endpoint:** `GET /notifications`

**Auth Required:** Yes (Owner role)

**Query Parameters:**
- `unreadOnly` (bool, default: false): Show only unread notifications

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "notificationId": 12,
      "type": "Appointment",
      "message": "Your appointment for Max is tomorrow at 10:00 AM",
      "createdAt": "2026-02-14T08:00:00Z",
      "isRead": false
    }
  ]
}
```

---

### 2. Mark as Read

Mark notification(s) as read.

**Endpoint:** `POST /notifications/mark-read`

**Auth Required:** Yes (Owner role)

**Request Body:**
```json
{
  "notificationIds": [12, 13, 14]
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Notifications marked as read."
}
```

---

### 3. Mark All as Read

Mark all notifications as read.

**Endpoint:** `POST /notifications/mark-all-read`

**Auth Required:** Yes (Owner role)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "All notifications marked as read."
}
```

---

## Profile

### 1. Get Profile

Retrieve authenticated user's profile.

**Endpoint:** `GET /profile`

**Auth Required:** Yes (Owner role)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": {
    "userId": 5,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "09171234567",
    "profileImage": "https://api.yourserver.com/uploads/users/pet.png",
    "twoFactorEnabled": true,
    "createdAt": "2023-01-10T08:30:00Z"
  }
}
```

---

### 2. Update Profile

Update user profile information.

**Endpoint:** `PUT /profile`

**Auth Required:** Yes (Owner role)

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "phone": "09171234567"
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Profile updated successfully."
}
```

---

### 3. Upload Profile Picture

Upload or update profile picture.

**Endpoint:** `POST /profile/picture`

**Auth Required:** Yes (Owner role)

**Request Body:** `multipart/form-data`
- `file`: Image file (JPEG, PNG, max 5MB)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Profile picture updated.",
  "data": {
    "profileImage": "https://api.yourserver.com/uploads/users/john_doe.jpg"
  }
}
```

**Notes:**
- Image is automatically cropped to 500x500 pixels
- Saved as JPEG format

---

### 4. Change Password

Change user password.

**Endpoint:** `POST /profile/change-password`

**Auth Required:** Yes (Owner role)

**Request Body:**
```json
{
  "currentPassword": "OldP@ss123",
  "newPassword": "NewSecureP@ss456",
  "confirmPassword": "NewSecureP@ss456"
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Password changed successfully."
}
```

**Error Responses:**
- `401 Unauthorized`: Incorrect current password
- `400 Bad Request`: New password same as old

---

### 5. Toggle 2FA

Enable or disable two-factor authentication.

**Endpoint:** `POST /profile/toggle-2fa`

**Auth Required:** Yes (Owner role)

**Request Body:**
```json
{
  "enable": true
}
```

**Success Response:** `200 OK`
```json
{
  "success": true,
  "message": "Two-factor authentication enabled."
}
```

---

## Service Categories

### 1. Get Service Categories

Retrieve all service categories with subtypes.

**Endpoint:** `GET /service-categories`

**Auth Required:** Yes (Owner role)

**Success Response:** `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "categoryId": 3,
      "serviceType": "Vaccination",
      "subtypes": [
        {
          "subtypeId": 8,
          "subtypeName": "Rabies"
        },
        {
          "subtypeId": 9,
          "subtypeName": "Distemper"
        }
      ]
    }
  ]
}
```

---

## Error Handling

### HTTP Status Codes

- `200 OK`: Success
- `201 Created`: Resource created
- `400 Bad Request`: Validation error
- `401 Unauthorized`: Authentication failed
- `403 Forbidden`: Authorization failed
- `404 Not Found`: Resource not found
- `409 Conflict`: Resource conflict (e.g., duplicate email)
- `429 Too Many Requests`: Rate limited
- `500 Internal Server Error`: Server error

### Common Error Responses

**Validation Error:**
```json
{
  "success": false,
  "message": "Validation failed.",
  "errors": [
    "Email is required.",
    "Password must be at least 8 characters."
  ]
}
```

**Unauthorized:**
```json
{
  "success": false,
  "message": "Invalid or expired token."
}
```

---

## Security Best Practices

1. **Store tokens securely**: Use secure storage (e.g., iOS Keychain, Android Keystore)
2. **Implement token refresh**: Refresh access token before expiration
3. **Handle token expiration**: Redirect to login on 401 responses
4. **Validate SSL certificates**: Only connect over HTTPS
5. **Logout on security events**: Revoke tokens on password change/logout
6. **Rate limiting**: Respect 429 responses

---

## Rate Limiting

- **Login attempts**: 5 attempts per account, 3-minute lockout
- **2FA attempts**: 10 attempts per code, code expires in 10 minutes
- **Password reset**: 1 request per minute per email

---

## Testing

### Postman Collection

Import the Postman collection for easy API testing:
[Download Collection](./postman_collection.json)

### Example cURL Request

```bash
curl -X POST https://api.yourserver.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "MySecureP@ss123"
  }'
```

---

## Support

For API support and questions:
- GitHub Issues: [Create Issue](https://github.com/yourusername/purrvet/issues)
- Email: support@happypaws.com

---

**API Version:** v1
**Last Updated:** February 2026
