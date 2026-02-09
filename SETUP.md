# PetCloud Setup & Installation Guide

This guide will walk you through setting up the PetCloud veterinary clinic management system on your local machine or server.

## Prerequisites

### Required Software

| Software | Version | Download Link |
|----------|---------|---------------|
| .NET SDK | 9.0+ | [Download](https://dotnet.microsoft.com/download) |
| SQL Server | 2019+ | [Download](https://www.microsoft.com/sql-server/sql-server-downloads) |
| Node.js | 18+ | [Download](https://nodejs.org/) |
| Git | Latest | [Download](https://git-scm.com/) |

### Optional Tools

- **Visual Studio 2022** (recommended for backend development)
- **VS Code** with C# and React Native extensions
- **SQL Server Management Studio** (SSMS) for database management
- **Postman** for API testing

---

## Backend Setup (ASP.NET Core)

### 1. Clone Repository

```bash
git clone https://github.com/yourusername/purrvet-v2.git
cd purrvet-v2
```

### 2. Configure Database Connection

Navigate to the server directory:
```bash
cd server
```

Edit `appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ProjectPurrDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Connection String Examples:**

**Local SQL Server (Windows Authentication):**
```
Server=localhost;Database=ProjectPurrDB;Trusted_Connection=True;TrustServerCertificate=True;
```

**SQL Server (SQL Authentication):**
```
Server=localhost;Database=ProjectPurrDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

**Azure SQL Database:**
```
Server=tcp:yourserver.database.windows.net,1433;Database=ProjectPurrDB;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;
```

### 3. Configure JWT Settings

Update JWT configuration in `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyWithAtLeast32Characters!!",
    "Issuer": "PetCloudAPI",
    "Audience": "PetCloudMobile",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

**⚠️ Security Warning:** Use a strong, unique secret key in production. Generate one using:

```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))

# Linux/Mac
openssl rand -base64 32
```

### 4. Configure Email Settings

Update SMTP configuration in `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "Happy Paws Clinic",
    "SenderEmail": "noreply@happypaws.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  }
}
```

**Gmail Setup:**
1. Enable 2-Step Verification in your Google Account
2. Generate an App Password: [Google App Passwords](https://myaccount.google.com/apppasswords)
3. Use the app password in the configuration

### 5. Restore NuGet Packages

```bash
dotnet restore
```

### 6. Apply Database Migrations

```bash
dotnet ef database update
```

This will create the `ProjectPurrDB` database with all required tables.

**If migrations fail:**
```bash
# Drop and recreate database
dotnet ef database drop --force
dotnet ef database update
```

### 7. Seed Initial Data (Optional)

Create an admin user by running the application and navigating to the registration page, or manually insert into the database:

```sql
-- Execute in SSMS or Azure Data Studio
USE ProjectPurrDB;

-- Create admin user (password: Admin@123)
INSERT INTO Users (FirstName, LastName, Email, Phone, Password, Type, Status, CreatedAt, ProfileImage)
VALUES ('Admin', 'User', 'admin@happypaws.com', '09171234567',
'AQAAAAIAAYagAAAAELxxx...', 'Admin', 'Active', GETDATE(), 'pet.png');
```

**Note:** The password hash shown above is just an example. Hash passwords properly using ASP.NET Identity's `IPasswordHasher`.

### 8. Run the Backend

```bash
dotnet run
```

Or with hot reload:
```bash
dotnet watch run
```

The API will start on:
- **HTTPS:** https://localhost:5001
- **HTTP:** http://localhost:5000

**Verify Setup:**
- Web Portal: https://localhost:5001
- API Health: https://localhost:5001/api/v1/health (if implemented)
- Swagger UI: https://localhost:5001/scalar/v1 (API documentation)

### 9. Create Upload Directories

Ensure these directories exist:

```bash
# PowerShell (Windows)
New-Item -ItemType Directory -Force -Path "wwwroot/uploads/users"
New-Item -ItemType Directory -Force -Path "wwwroot/uploads/pets"
New-Item -ItemType Directory -Force -Path "wwwroot/uploads/petcards"

# Bash (Linux/Mac)
mkdir -p wwwroot/uploads/{users,pets,petcards}
```

---

## Mobile App Setup (React Native / Expo)

### 1. Navigate to Mobile Directory

```bash
cd mobile
```

### 2. Install Dependencies

```bash
npm install
```

If you encounter peer dependency issues:
```bash
npm install --legacy-peer-deps
```

### 3. Configure API Base URL

Create or edit `.env` file in the `mobile` directory:

```env
API_BASE_URL=http://192.168.1.100:5000/api/v1
API_TIMEOUT=30000
```

**Network Configuration:**

**For Android Emulator:**
```env
API_BASE_URL=http://10.0.2.2:5000/api/v1
```

**For iOS Simulator:**
```env
API_BASE_URL=http://localhost:5000/api/v1
```

**For Physical Device (same network):**
```env
API_BASE_URL=http://YOUR_LOCAL_IP:5000/api/v1
```

Find your local IP:
```bash
# Windows
ipconfig

# Linux/Mac
ifconfig
```

### 4. Generate API Client (Optional)

If you have OpenAPI specification:

```bash
npm run gen:api
```

This generates TypeScript client code from the API's OpenAPI/Swagger definition.

### 5. Start Expo Development Server

```bash
npm start
```

Or use specific commands:

```bash
# Start for Android
npm run android

# Start for iOS (Mac only)
npm run ios

# Start for Web
npm run web
```

### 6. Run on Device/Emulator

**Option A: Expo Go App**
1. Install Expo Go on your phone ([iOS](https://apps.apple.com/app/expo-go/id982107779) | [Android](https://play.google.com/store/apps/details?id=host.exp.exponent))
2. Scan the QR code from the terminal

**Option B: Android Studio Emulator**
1. Open Android Studio
2. Start an emulator (AVD Manager)
3. Press `a` in the Expo terminal

**Option C: Xcode Simulator (Mac only)**
1. Install Xcode from App Store
2. Press `i` in the Expo terminal

### 7. Testing Mobile App

**Test Login:**
1. Register a new account via the mobile app
2. Check email for 2FA code (if enabled)
3. Complete login and verify dashboard loads

---

## Database Setup

### Option 1: Automatic (Entity Framework)

The migrations will automatically create the database structure:

```bash
cd server
dotnet ef database update
```

### Option 2: Manual SQL Script

If you prefer manual setup, export the schema from an existing database:

```bash
# Generate migration script
dotnet ef migrations script -o Database.sql
```

Then execute in SSMS:
```sql
-- Run Database.sql in SQL Server Management Studio
```

### Database Tables

The system creates these main tables:
- **Users** - All users (Admin, Staff, Owner)
- **Owners** - Pet owner profiles
- **Pets** - Pet records
- **Appointments** - Appointment bookings
- **ServiceCategories** - Service types (Vaccination, Surgery, etc.)
- **ServiceSubtypes** - Specific services under categories
- **Notifications** - System notifications
- **SystemLogs** - Audit trail
- **RefreshTokens** - JWT refresh tokens

### Seed Data: Service Categories

Insert default service categories:

```sql
-- Vaccination
INSERT INTO ServiceCategories (ServiceType, Description)
VALUES ('Vaccination', 'Immunization services');

-- Deworming & Preventives
INSERT INTO ServiceCategories (ServiceType, Description)
VALUES ('Deworming & Preventives', 'Parasite prevention');

-- Surgery
INSERT INTO ServiceCategories (ServiceType, Description)
VALUES ('Surgery', 'Surgical procedures');

-- Grooming & Wellness
INSERT INTO ServiceCategories (ServiceType, Description)
VALUES ('Grooming & Wellness', 'Grooming and wellness care');
```

### Breed Data CSV Files

Place breed CSV files in `server/App_Data/`:
- `DogBreeds.csv`
- `CatBreeds.csv`

**Format:**
```csv
Breed
Golden Retriever
Labrador Retriever
Beagle
```

---

## Configuration Reference

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjectPurrDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "YourSecretKeyHere",
    "Issuer": "PetCloudAPI",
    "Audience": "PetCloudMobile",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "Happy Paws Clinic",
    "SenderEmail": "noreply@happypaws.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  },
  "FileUpload": {
    "MaxFileSizeBytes": 5242880,
    "AllowedExtensions": [".jpg", ".jpeg", ".png"],
    "UploadPath": "wwwroot/uploads"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Troubleshooting

### Common Issues

#### 1. Port Already in Use

```
Error: Failed to bind to address https://127.0.0.1:5001
```

**Solution:**
```bash
# Find process using port
netstat -ano | findstr :5001

# Kill process (replace PID with actual process ID)
taskkill /PID 12345 /F
```

Or change port in `Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:7001;http://localhost:7000"
```

#### 2. Database Connection Failed

```
A network-related error occurred while establishing a connection to SQL Server
```

**Solutions:**
- Verify SQL Server is running (Services → SQL Server)
- Check connection string server name
- Enable TCP/IP in SQL Server Configuration Manager
- Add firewall exception for SQL Server port (1433)
- For Azure SQL, check firewall rules

#### 3. Entity Framework Migrations Error

```
Unable to create an object of type 'ApplicationDbContext'
```

**Solution:**
```bash
# Ensure you're in the server directory
cd server

# Try with startup project specified
dotnet ef database update --project PetCloud.csproj
```

#### 4. Mobile App Cannot Connect to API

```
Network request failed / Connection refused
```

**Solutions:**
- Verify API is running (check terminal)
- Check API base URL in mobile `.env`
- Ensure phone/emulator is on same network
- Disable Windows Firewall temporarily
- For Android emulator, use `10.0.2.2` instead of `localhost`
- For HTTPS, ensure certificate is trusted

#### 5. Build Error: PurrVet.exe Locked

```
MSB3027: Could not copy "PurrVet.exe"
```

**Solutions:**
```bash
# Stop the running application (Ctrl+C in terminal)

# Or kill the process
taskkill /IM PetCloud.exe /F

# Clean and rebuild
dotnet clean
dotnet build
```

#### 6. Email Not Sending

**Solutions:**
- Verify SMTP credentials
- Check SMTP server and port
- For Gmail, ensure "Less secure app access" or use App Password
- Check spam folder
- Review logs for detailed error message

#### 7. Image Upload Fails

**Solutions:**
- Verify upload directories exist (`wwwroot/uploads/pets`)
- Check file size (max 5MB by default)
- Ensure allowed file extensions (.jpg, .jpeg, .png)
- Verify write permissions on upload folder

---

## Production Deployment

### Backend Deployment (IIS)

#### 1. Publish Application

```bash
cd server
dotnet publish -c Release -o ./publish
```

#### 2. Configure IIS

1. Install ASP.NET Core Runtime on server
2. Create new IIS website
3. Point to publish folder
4. Set application pool to "No Managed Code"
5. Configure HTTPS binding

#### 3. Update appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD_SERVER;Database=ProjectPurrDB;..."
  },
  "Jwt": {
    "Secret": "PRODUCTION_SECRET_KEY_HERE"
  }
}
```

### Mobile App Deployment

#### iOS (App Store)

```bash
cd mobile
eas build --platform ios
eas submit --platform ios
```

#### Android (Google Play)

```bash
cd mobile
eas build --platform android
eas submit --platform android
```

Refer to [Expo EAS Documentation](https://docs.expo.dev/build/introduction/) for detailed deployment steps.

---

## Health Check

After setup, verify all components:

### Backend Checklist
- [ ] API responds on https://localhost:5001
- [ ] Database connection successful
- [ ] Login endpoint works (/api/v1/auth/login)
- [ ] File uploads work (profile picture)
- [ ] Email sending works (test forgot password)
- [ ] PDF generation works (pet card download)

### Mobile App Checklist
- [ ] App launches without errors
- [ ] Can reach login screen
- [ ] Registration flow completes
- [ ] Can login and reach dashboard
- [ ] Images load properly
- [ ] API calls succeed

### Test Commands

```bash
# Backend - Test API endpoint
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123"}'

# Database - Check connection
dotnet ef dbcontext info

# Mobile - Check dependencies
cd mobile && npm ls
```

---

## Getting Help

If you encounter issues:

1. **Check logs**: Review application logs in terminal/console
2. **Database logs**: Check SQL Server error logs
3. **Network issues**: Use Postman to test API directly
4. **Search issues**: Check GitHub repository issues
5. **Create issue**: [Report Bug](https://github.com/yourusername/purrvet/issues/new)

---

## Next Steps

After successful setup:

1. Review [API Documentation](./API_DOCUMENTATION.md) for endpoint details
2. Read [Architecture Guide](./ARCHITECTURE.md) to understand the system
3. Check [User Guide](./USER_GUIDE.md) for usage instructions
4. Start developing features!

---

**Setup Complete!**

You're now ready to develop and run PetCloud locally.
