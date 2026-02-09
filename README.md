# PetCloud - Veterinary Clinic Management System

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![React Native](https://img.shields.io/badge/React%20Native-0.81-blue.svg)

PetCloud (formerly PurrVet) is a comprehensive veterinary clinic management application designed for Happy Paws Veterinary Clinic. It provides a complete solution for managing pets, appointments, owners, and clinic operations through both web and mobile interfaces.

## Features

### For Pet Owners (Mobile App)
- Register and manage account with 2FA security
- Add and manage multiple pets with photo uploads
- Book and track appointments
- View vaccination and deworming schedules
- Receive notifications for upcoming appointments
- Download pet health cards (PDF)
- Manage profile and settings

### For Clinic Staff & Admin (Web Portal)
- Comprehensive appointment management
- Dashboard with clinic statistics and insights
- Owner and pet records management
- Service category and subtype configuration
- Detailed medical records tracking
- Role-based access control (Admin, Staff, Owner)
- Email notifications and system logs
- ML-powered service forecasting

## Architecture

### Backend (ASP.NET Core 9.0)
- **Framework**: ASP.NET Core MVC 9.0
- **Database**: SQL Server (ProjectPurrDB)
- **Authentication**:
  - Session-based (Cookie) for web portal
  - JWT Bearer tokens for mobile API
- **API**: RESTful API with `/api/v1/` prefix
- **ORM**: Entity Framework Core 9.0
- **Design Pattern**: Direct DbContext access from controllers

### Frontend
- **Web**: Razor Views with modern CSS design system
- **Mobile**: React Native (Expo) with TypeScript
- **State Management**: Zustand
- **Styling**: TailwindCSS (NativeWind)

### Key Technologies
- **Authentication**: ASP.NET Identity password hashing
- **Email**: MailKit with SMTP
- **PDF Generation**: QuestPDF (A5 pet health cards)
- **Image Processing**: System.Drawing.Common (500x500 crop)
- **ML**: Microsoft.ML for service forecasting
- **CSV Import**: CsvHelper for breed data

## Project Structure

```
PurrVet/
├── server/                 # ASP.NET Core backend
│   ├── Controllers/        # MVC and API controllers
│   │   ├── Api/V1/        # Mobile API endpoints
│   │   └── [Web]/         # Web MVC controllers
│   ├── Models/            # Entity models and ViewModels
│   ├── DTOs/              # API data transfer objects
│   ├── Services/          # Business logic services
│   ├── Infrastructure/    # JWT, middleware, extensions
│   ├── Views/             # Razor views
│   ├── wwwroot/           # Static files and uploads
│   └── App_Data/          # CSV breed data
│
├── mobile/                # React Native mobile app
│   ├── app/              # Expo Router pages
│   ├── components/       # Reusable UI components
│   ├── api/              # API client (generated)
│   ├── store/            # Zustand state management
│   └── types/            # TypeScript type definitions
│
└── docs/                 # Documentation
```

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- SQL Server 2019+
- Node.js 18+ & npm
- Visual Studio 2022 or VS Code

### Backend Setup
```bash
cd server
dotnet restore
dotnet ef database update
dotnet run
```

### Mobile App Setup
```bash
cd mobile
npm install
npx expo start
```

For detailed setup instructions, see [SETUP.md](./SETUP.md).

## Documentation

- **[Setup Guide](./SETUP.md)** - Installation and configuration
- **[API Documentation](./API_DOCUMENTATION.md)** - RESTful API endpoints
- **[Architecture Guide](./ARCHITECTURE.md)** - Technical architecture details
- **[User Guide](./USER_GUIDE.md)** - Application usage instructions

## Security Features

- **Two-Factor Authentication (2FA)**: Email-based OTP verification
- **JWT Token Rotation**: Secure refresh token mechanism with reuse detection
- **Password Security**: ASP.NET Identity password hashing (PBKDF2)
- **Account Lockout**: 3-minute lockout after 5 failed login attempts
- **Inactivity Monitoring**: Automatic account deactivation after 100 days
- **Device Tracking**: Location and device-based 2FA triggers
- **Authorization Policies**: Role-based and claim-based access control

## Design System

The web portal uses a modern, consistent design language:
- **Font**: Inter (standardized across all layouts)
- **CSS Framework**: Custom modern design system
- **Key Classes**: `card-accent`, `form-group-modern`, `btn-modern-primary`, `modal-modern`
- **Responsive**: Mobile-first responsive design

## Database Roles

1. **Admin**: Full system access, user management, system configuration
2. **Staff**: Manage appointments, pets, owners, view reports
3. **Owner**: Mobile app access, view own pets and appointments

## Configuration

Key configuration in `appsettings.json`:
- Database connection strings
- JWT settings (secret, issuer, audience, expiration)
- Email SMTP configuration
- File upload paths

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is proprietary software developed for Happy Paws Veterinary Clinic.

## Authors

- **Development Team** - PetCloud Project

## Known Issues

- `PurrVet.exe` may remain locked by running process - use Task Manager if build fails
- MSB3027 warnings may appear but can be ignored if no CS errors
- Large PDF generation may timeout on slow connections

---

**Happy Paws Veterinary Clinic** - Caring for your pets with technology
