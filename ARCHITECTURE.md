# PetCloud Technical Architecture

This document provides a comprehensive overview of the PetCloud system architecture, design patterns, and technical decisions.

## System Overview

PetCloud is a full-stack veterinary clinic management system consisting of three main components:

```
┌──────────────────────────────────────────────────────────────┐
│                        PetCloud System                       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│   ┌──────────────┐   ┌──────────────┐  ┌─────────────────┐   │
│   │  Web Portal  │   │  Mobile App  │  │  Backend API    │   │
│   │  (MVC)       │   │  (React)     │  │  (ASP.NET Core) │   │
│   │              │   │              │  │                 │   │
│   │  Razor Views │   │  Expo Router │  │  Controllers    │   │
│   │  + Modern    │   │  + Native    │  │  + Services     │   │
│   │  CSS         │   │  Wind        │  │  + EF Core      │   │
│   └──────┬───────┘   └──────┬───────┘  └────────┬────────┘   │
│          │                  │                   │            │
│          └──────────────────┴───────────────────┘            │
│                             │                                │
│                      ┌──────▼────────┐                       │
│                      │  SQL Server   │                       │
│                      │  (ProjectPurr │                       │
│                      │   DB)         │                       │
│                      └───────────────┘                       │
└──────────────────────────────────────────────────────────────┘
```

---

## Backend Architecture

### Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| **Framework** | ASP.NET Core MVC | 9.0 |
| **Language** | C# | 13.0 |
| **ORM** | Entity Framework Core | 9.0.8 |
| **Database** | SQL Server | 2019+ |
| **Authentication** | Cookie + JWT Bearer | - |
| **API Docs** | Scalar (OpenAPI) | 2.12.34 |

### Project Structure

```
server/
├── Controllers/
│   ├── Api/V1/                 # Mobile API controllers
│   │   ├── AuthController.cs
│   │   ├── DashboardController.cs
│   │   ├── PetsController.cs
│   │   ├── AppointmentsController.cs
│   │   ├── NotificationsController.cs
│   │   └── ProfileController.cs
│   └── [Web Controllers]       # MVC web controllers
│       ├── AccountController.cs
│       ├── DashboardController.cs
│       └── ...
│
├── Models/                     # Domain entities
│   ├── User.cs
│   ├── Owner.cs
│   ├── Pet.cs
│   ├── Appointment.cs
│   ├── ServiceCategory.cs
│   ├── Notification.cs
│   ├── SystemLog.cs
│   └── RefreshToken.cs
│
├── DTOs/                       # API data transfer objects
│   ├── Auth/
│   ├── Dashboard/
│   ├── Pets/
│   ├── Appointments/
│   ├── Profile/
│   ├── Notifications/
│   └── Common/
│
├── Services/                   # Business logic
│   ├── EmailService.cs
│   ├── JwtTokenService.cs
│   └── ServiceForecastService.cs
│
├── Infrastructure/             # Cross-cutting concerns
│   ├── ApiExceptionMiddleware.cs
│   ├── ClaimsPrincipalExtensions.cs
│   └── ...
│
├── Views/                      # Razor views (MVC)
│   ├── Shared/
│   ├── Account/
│   ├── Dashboard/
│   └── ...
│
├── wwwroot/                    # Static files
│   ├── css/
│   ├── js/
│   └── uploads/
│       ├── users/
│       ├── pets/
│       └── petcards/
│
├── Migrations/                 # EF Core migrations
├── App_Data/                   # CSV breed data
├── Program.cs                  # Application entry point
└── appsettings.json           # Configuration
```

---

## Authentication & Authorization

### Dual Authentication Scheme

PetCloud uses **two authentication schemes** to support both web and mobile clients:

```csharp
// Program.cs
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.OpenIdConnectScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.OpenIdConnectScheme, options => { ... })
.AddJwtBearer("Bearer", options => { ... });
```

#### Cookie Authentication (Web Portal)
- **Used by**: Admin, Staff, Owner web portal
- **Session-based**: HttpOnly secure cookies
- **Flow**: Login → Cookie issued → Session maintained
- **Expires**: On browser close or timeout

#### JWT Bearer Authentication (Mobile API)
- **Used by**: Mobile app (Owner role only)
- **Stateless**: JWT tokens in `Authorization: Bearer <token>` header
- **Tokens**:
  - **Access Token**: Short-lived (60 minutes), contains user claims
  - **Refresh Token**: Long-lived (30 days), stored in database
- **Flow**: Login → JWT issued → Token refresh before expiry

### JWT Token Structure

**Access Token Claims:**
```json
{
  "sub": "5",                           // User ID
  "email": "john.doe@example.com",
  "name": "John Doe",
  "role": "Owner",
  "ownerId": "3",                       // Owner ID
  "iat": 1675950000,
  "exp": 1675953600,
  "iss": "PetCloudAPI",
  "aud": "PetCloudMobile"
}
```

**Token Storage:**
- **Mobile**: Secure storage (Expo SecureStore → iOS Keychain / Android Keystore)
- **Backend**: Refresh tokens stored in `RefreshTokens` table

### Authorization Policies

```csharp
// Program.cs
builder.Services.AddAuthorization(options => {
    options.AddPolicy("OwnerOnly", policy => {
        policy.AuthenticationSchemes.Add("Bearer");
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Owner");
        policy.RequireClaim("ownerId");
    });
});
```

**Usage:**
```csharp
[Authorize(Policy = "OwnerOnly")]
public class PetsController : ControllerBase { ... }
```

### Token Refresh Flow

```
┌────────┐                ┌────────┐                ┌──────────┐
│ Mobile │                │  API   │                │ Database │
└───┬────┘                └───┬────┘                └─────┬────┘
    │                         │                           │
    │  POST /auth/refresh     │                           │
    │  {refreshToken}         │                           │
    ├────────────────────────>│                           │
    │                         │  Find RefreshToken        │
    │                         ├──────────────────────────>│
    │                         │<──────────────────────────┤
    │                         │  Validate & check reuse   │
    │                         │                           │
    │                         │  Generate new tokens      │
    │                         │                           │
    │                         │  Revoke old token         │
    │                         │  Store new token          │
    │                         ├──────────────────────────>│
    │                         │<──────────────────────────┤
    │  200 OK                 │                           │
    │  {accessToken, refresh} │                           │
    │<────────────────────────┤                           │
```

**Reuse Detection:**
- If a revoked refresh token is reused → All tokens for that device are revoked
- Prevents token theft attacks

### Two-Factor Authentication (2FA)

**Flow:**
```
1. User logs in → Check if 2FA required
   ├─ New device detected → Require 2FA
   ├─ New IP address → Require 2FA
   ├─ 30 days since last 2FA → Require 2FA
   └─ Otherwise → Issue tokens directly

2. If 2FA required:
   ├─ Generate 6-digit code
   ├─ Store in User.TwoFactorCode (expires 10 min)
   ├─ Send email via EmailService
   └─ Return {requires2FA: true, twoFactorUserId: 5}

3. User submits code:
   ├─ Validate code & expiry
   ├─ Clear TwoFactorCode
   ├─ Update LastTwoFactorVerification
   └─ Issue JWT tokens
```

**Database Fields:**
```csharp
public class User {
    public string? TwoFactorCode { get; set; }
    public DateTime? TwoFactorExpiry { get; set; }
    public DateTime? LastTwoFactorVerification { get; set; }
    public string? LastLoginIP { get; set; }
    public string? LastLoginDevice { get; set; }
    public bool TwoFactorEnabled { get; set; }
}
```

---

## Database Design

### Entity Relationship Diagram

```
┌─────────────┐       ┌─────────────┐       ┌──────────────┐
│    User     │       │    Owner    │       │     Pet      │
├─────────────┤       ├─────────────┤       ├──────────────┤
│ UserID (PK) │───┐   │ OwnerID(PK) │───┐   │ PetID (PK)   │
│ Email       │   └──>│ UserID (FK) │   └──>│ OwnerID (FK) │
│ Password    │       │ Name        │       │ Name         │
│ Type        │       │ Phone       │       │ Type         │
│ Status      │       │ Email       │       │ Breed        │
│ ...         │       │ Address     │       │ Birthdate    │
└─────────────┘       └─────────────┘       │ PhotoPath    │
                                             └──────┬───────┘
                                                    │
                                                    │
┌──────────────┐       ┌──────────────┐           │
│Notification  │       │ Appointment  │<──────────┘
├──────────────┤       ├──────────────┤
│NotificationID│       │AppointmentID │
│ Type         │       │ PetID (FK)   │
│ Message      │       │ CategoryID   │
│ TargetRole   │       │ SubtypeID    │
│ TargetUserID │       │ Appointment  │
│ IsRead       │       │ Date         │
└──────────────┘       │ Status       │
                       │ Notes        │
                       │ DueDate      │
                       └──────────────┘

┌──────────────┐       ┌──────────────┐
│ServiceCat    │       │ServiceSubtype│
├──────────────┤       ├──────────────┤
│ CategoryID   │───┐   │ SubtypeID    │
│ ServiceType  │   └──>│ CategoryID   │
│ Description  │       │ SubtypeName  │
└──────────────┘       └──────────────┘
```

### Core Entities

#### User
- Central entity for authentication
- Types: `Admin`, `Staff`, `Owner`
- Status: `Active`, `Inactive`
- Password hashed with ASP.NET Identity `IPasswordHasher<User>`

#### Owner
- Extends User entity (1-to-1 relationship)
- Contains pet owner specific information
- Links to multiple Pets

#### Pet
- Belongs to one Owner
- Has many Appointments
- Fields: Name, Type (Dog/Cat), Breed, Birthdate, Sex, Color, PhotoPath
- Age calculated dynamically from Birthdate

#### Appointment
- Central entity for clinic operations
- Links to Pet, ServiceCategory, ServiceSubtype
- Status: `Pending`, `Completed`, `Cancelled`
- DueDate: For recurring services (vaccinations, deworming)

#### RefreshToken
- JWT refresh token management
- Fields: Token (hashed), UserId, DeviceInfo, ExpiresAt, IsRevoked, ReusedAt
- Enables token rotation and reuse detection

#### Notification
- Targeted notifications: by role or specific user
- Fields: Type, Message, TargetRole, TargetUserId, IsRead
- Used for appointment reminders, system alerts

#### SystemLog
- Audit trail for all major actions
- Fields: ActionType (Create/Update/Delete), Module, Description, PerformedBy, Timestamp

### Indexes & Performance

**Key Indexes:**
```sql
-- User lookups
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Type_Status ON Users(Type, Status);

-- Appointment queries
CREATE INDEX IX_Appointments_PetID_AppointmentDate ON Appointments(PetID, AppointmentDate);
CREATE INDEX IX_Appointments_Status_AppointmentDate ON Appointments(Status, AppointmentDate);
CREATE INDEX IX_Appointments_DueDate ON Appointments(DueDate);

-- Notifications
CREATE INDEX IX_Notifications_TargetUserID_IsRead ON Notifications(TargetUserID, IsRead);

-- Refresh tokens
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_UserId_IsRevoked ON RefreshTokens(UserId, IsRevoked);
```

---

## Design Patterns

### 1. Direct DbContext Access Pattern

**Approach:**
```csharp
public class PetsController : ControllerBase {
    private readonly ApplicationDbContext _context;

    public PetsController(ApplicationDbContext context) {
        _context = context;
    }

    public IActionResult GetPets() {
        var pets = _context.Pets
            .Where(p => p.OwnerID == ownerId)
            .ToList();
        return Ok(pets);
    }
}
```

**Rationale:**
- **Simplicity**: No extra abstraction layers (no repository/service layer)
- **Transparency**: Direct LINQ queries in controllers
- **Rapid development**: Faster iteration for CRUD operations
- **EF Core power**: Full access to EF Core features

**Trade-offs:**
- Less testable (tight coupling to DbContext)
- Business logic in controllers (not ideal for complex scenarios)
- Suitable for smaller projects or MVPs

### 2. DTO Pattern (API Layer)

**Separation of concerns:**
```
Domain Model (Database) → DTO (API Response) → Client
```

**Example:**
```csharp
// Domain Model
public class Pet {
    public int PetID { get; set; }
    public string Name { get; set; }
    public Owner Owner { get; set; }  // Navigation property
    // ... many other properties
}

// DTO (API Response)
public class PetListItemDto {
    public int PetId { get; set; }
    public string Name { get; set; }
    public string Age { get; set; }  // Computed
    // Only fields needed by mobile app
}
```

**Benefits:**
- API versioning flexibility
- Reduce payload size
- Hide internal structure
- Add computed fields

### 3. Middleware Pattern

**ApiExceptionMiddleware:**
```csharp
public class ApiExceptionMiddleware {
    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        } catch (Exception ex) {
            // Centralized error handling
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

**Benefits:**
- Centralized error handling
- Consistent error responses
- Logging and monitoring
- Prevents sensitive error details leaking

### 4. Extension Methods Pattern

**ClaimsPrincipalExtensions:**
```csharp
public static class ClaimsPrincipalExtensions {
    public static int GetUserId(this ClaimsPrincipal user) {
        return int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    public static int GetOwnerId(this ClaimsPrincipal user) {
        return int.Parse(user.FindFirst("ownerId")?.Value);
    }
}
```

**Usage:**
```csharp
var ownerId = User.GetOwnerId();  // Clean and reusable
```

---

## API Design

### RESTful Principles

**Endpoint Naming:**
```
GET    /api/v1/pets           → List all pets
GET    /api/v1/pets/{id}      → Get pet details
POST   /api/v1/pets           → Create pet
PUT    /api/v1/pets/{id}      → Update pet
DELETE /api/v1/pets/{id}      → Delete pet
```

### Consistent Response Format

**Success:**
```json
{
  "success": true,
  "message": "Optional message",
  "data": { /* payload */ }
}
```

**Error:**
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Validation error 1"]
}
```

### API Versioning

- **URL Path Versioning**: `/api/v1/`, `/api/v2/`
- Allows breaking changes in new versions
- Old versions remain available for backward compatibility

### OpenAPI / Swagger

**Scalar UI** (modern alternative to Swagger UI):
- Auto-generated API documentation
- Interactive API testing
- Endpoint summaries and descriptions

**Annotations:**
```csharp
[HttpPost("login")]
[EndpointSummary("Log in")]
[EndpointDescription("Authenticate with email and password...")]
[ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
[ProducesResponseType(typeof(ApiErrorResponse), 401)]
public async Task<IActionResult> Login([FromBody] LoginRequest request) { ... }
```

---

## Mobile Architecture

### Technology Stack

| Layer | Technology |
|-------|------------|
| **Framework** | React Native (Expo) |
| **Language** | TypeScript |
| **Navigation** | Expo Router (file-based) |
| **State Management** | Zustand |
| **Styling** | TailwindCSS (NativeWind) |
| **HTTP Client** | Axios |
| **Secure Storage** | expo-secure-store |

### Project Structure

```
mobile/
├── app/                       # Expo Router pages
│   ├── (auth)/               # Auth screens
│   │   ├── login.tsx
│   │   ├── register.tsx
│   │   └── forgot-password.tsx
│   ├── (tabs)/               # Tab navigation
│   │   ├── index.tsx         # Dashboard
│   │   ├── pets.tsx
│   │   ├── appointments.tsx
│   │   └── profile.tsx
│   └── _layout.tsx           # Root layout
│
├── components/               # Reusable components
│   ├── ui/                   # UI primitives
│   ├── pet-card.tsx
│   └── appointment-item.tsx
│
├── api/                      # API client (generated)
│   ├── services/
│   └── types/
│
├── store/                    # Zustand stores
│   ├── authStore.ts
│   ├── petStore.ts
│   └── notificationStore.ts
│
├── types/                    # TypeScript types
│   └── index.ts
│
├── assets/                   # Images, fonts
└── global.css               # Tailwind styles
```

### State Management (Zustand)

**authStore.ts:**
```typescript
import create from 'zustand';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;

  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshAccessToken: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  refreshToken: null,

  login: async (email, password) => {
    const response = await authApi.login({ email, password });
    set({
      user: response.data.owner,
      accessToken: response.data.accessToken,
      refreshToken: response.data.refreshToken
    });
  },

  logout: () => {
    set({ user: null, accessToken: null, refreshToken: null });
  }
}));
```

**Benefits:**
- Simple, minimal boilerplate
- TypeScript support
- No providers needed
- DevTools integration

### Token Management

**Automatic Token Refresh:**
```typescript
// Axios interceptor
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Token expired, refresh it
      const newToken = await refreshAccessToken();
      error.config.headers['Authorization'] = `Bearer ${newToken}`;
      return api.request(error.config);
    }
    return Promise.reject(error);
  }
);
```

---

## Design System

### Web Portal CSS Architecture

**Modern CSS Framework:**
- `dashboard-modern.css` - Core styles
- `modern-extensions.css` - Extended components

**Design Tokens:**
```css
:root {
  --color-primary: #00b4d8;
  --color-secondary: #0077b6;
  --font-family: 'Inter', sans-serif;
  --border-radius: 12px;
  --shadow-card: 0 4px 20px rgba(0,0,0,0.08);
}
```

**Component Classes:**
- `.card-accent` - Modern card with subtle shadow
- `.form-group-modern` - Styled form inputs
- `.btn-modern-primary` - Primary action button
- `.modal-modern` - Modal dialogs
- `.breadcrumb-modern` - Navigation breadcrumbs
- `.stats-icon-box` - Dashboard statistics cards

**Responsive:**
```css
@media (max-width: 768px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}
```

### Mobile Design System (TailwindCSS)

**NativeWind Configuration:**
```javascript
// tailwind.config.js
module.exports = {
  theme: {
    extend: {
      colors: {
        primary: '#00b4d8',
        secondary: '#0077b6',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui'],
      }
    }
  }
}
```

**Usage:**
```tsx
<View className="bg-white rounded-xl shadow-lg p-4">
  <Text className="text-lg font-semibold text-gray-900">
    Pet Name
  </Text>
</View>
```

---

## Key Dependencies

### Backend (NuGet Packages)

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server EF Core provider |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT authentication |
| `Microsoft.Identity.Web` | OpenID Connect / Azure AD |
| `QuestPDF` | PDF generation (pet cards) |
| `MailKit` | Email sending (SMTP) |
| `CsvHelper` | CSV parsing (breed data) |
| `BCrypt.Net-Next` | Password hashing (fallback) |
| `System.Drawing.Common` | Image processing |
| `Microsoft.ML` | Machine learning (forecasting) |
| `Scalar.AspNetCore` | API documentation |

### Mobile (npm Packages)

| Package | Purpose |
|---------|---------|
| `expo` | React Native framework |
| `expo-router` | File-based navigation |
| `axios` | HTTP client |
| `zustand` | State management |
| `nativewind` | TailwindCSS for React Native |
| `expo-secure-store` | Secure token storage |
| `expo-image-picker` | Camera/gallery access |
| `jwt-decode` | JWT token parsing |
| `zod` | Runtime validation |

---

## Security Architecture

### Password Security

**Hashing Algorithm:**
- ASP.NET Identity `IPasswordHasher<User>`
- Uses PBKDF2 with HMAC-SHA256
- Salted hashes (unique salt per password)
- Configurable iteration count

```csharp
var hashedPassword = _passwordHasher.HashPassword(user, plainPassword);
```

### Account Lockout

**Brute Force Protection:**
- Max 5 failed login attempts
- 3-minute lockout after threshold
- Counter reset on successful login

### Inactivity Monitoring

**Automatic Deactivation:**
- Account disabled after 100 days of inactivity
- Inactivity measured from `LastTwoFactorVerification` or `CreatedAt`
- Prevents abandoned accounts

### Input Validation

**Server-side Validation:**
```csharp
[Required, EmailAddress]
public string Email { get; set; }

[Required, MinLength(8)]
public string Password { get; set; }

[RegularExpression(@"^\d{11}$")]
public string Phone { get; set; }
```

**Prevents:**
- SQL injection (via parameterized queries)
- XSS (via Razor encoding)
- Command injection
- Path traversal

### File Upload Security

**Image Upload Validation:**
1. File extension whitelist (.jpg, .jpeg, .png)
2. Max file size (5MB)
3. MIME type validation
4. Resize/crop to standard dimensions (500x500)
5. Store outside web root
6. Serve via controller action (not direct access)

---

## Performance Optimizations

### Database

1. **Indexes** on frequently queried columns
2. **Eager loading** with `.Include()` to prevent N+1 queries
3. **Pagination** for large result sets
4. **Projection** with `.Select()` to reduce payload

### API

1. **Response compression** (gzip)
2. **Caching headers** for static files
3. **Async/await** for all I/O operations
4. **DTO projection** to minimize payload size

### Mobile

1. **Image optimization** with Expo Image
2. **Lazy loading** components
3. **Memoization** with `useMemo` / `useCallback`
4. **Token caching** in secure storage

---

## Monitoring & Logging

### Logging

**ASP.NET Core Logging:**
```csharp
_logger.LogInformation("User {UserId} logged in", userId);
_logger.LogError(ex, "Failed to send email to {Email}", email);
```

**Levels:**
- `Trace` - Detailed debugging
- `Debug` - Development diagnostics
- `Information` - General flow
- `Warning` - Unexpected but handled
- `Error` - Failures requiring attention
- `Critical` - System failures

### Audit Trail

**SystemLog Entity:**
```csharp
_context.SystemLogs.Add(new SystemLog {
    ActionType = "Create",
    Module = "Appointment",
    Description = $"Appointment created for pet {petName}",
    PerformedBy = $"UserID:{userId}",
    Timestamp = DateTime.Now
});
```

---

## Testing Strategy

### Backend Testing

**Unit Tests:**
- Test business logic in isolation
- Mock dependencies (`IPasswordHasher`, `DbContext`)

**Integration Tests:**
- Test API endpoints end-to-end
- Use in-memory database or test database

### Mobile Testing

**Component Tests:**
- Test UI components in isolation
- Use React Native Testing Library

**E2E Tests:**
- Test critical user flows (login, booking)
- Use Detox or Maestro

---

## Deployment Architecture

### Production Environment

```
                    ┌─────────────────┐
                    │  Load Balancer  │
                    └────────┬────────┘
                             │
                ┌────────────┴────────────┐
                │                         │
         ┌──────▼──────┐          ┌──────▼──────┐
         │ IIS Server 1│          │ IIS Server 2│
         │ (ASP.NET)   │          │ (ASP.NET)   │
         └──────┬──────┘          └──────┬──────┘
                │                         │
                └────────────┬────────────┘
                             │
                      ┌──────▼──────┐
                      │  SQL Server │
                      │  (Primary)  │
                      └─────────────┘
```

### CI/CD Pipeline

```
GitHub Push
  ↓
GitHub Actions / Azure DevOps
  ↓
Build & Test
  ↓
Publish Artifacts
  ↓
Deploy to Staging
  ↓
Run Smoke Tests
  ↓
Deploy to Production
```

---

## Future Architecture Considerations

### Scalability

- **Horizontal scaling**: Add more web servers behind load balancer
- **Caching layer**: Redis for session management, API responses
- **CDN**: Serve static assets and images
- **Read replicas**: Offload read queries to SQL Server replicas

### Microservices (Future)

Potential separation:
- **Auth Service**: User authentication & JWT management
- **Appointment Service**: Booking logic & scheduling
- **Notification Service**: Email, SMS, push notifications
- **File Service**: Image upload & processing

### Message Queue

For async operations:
- **RabbitMQ** or **Azure Service Bus**
- Background jobs: Email sending, PDF generation, ML forecasting

---

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [React Native Documentation](https://reactnative.dev/)
- [Expo Documentation](https://docs.expo.dev/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

---

**Last Updated:** February 2026
