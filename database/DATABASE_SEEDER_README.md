# Database Seeder Guide

This guide explains how to use the database seeder to populate your PetCloud database with sample data for development and testing.

## Overview

The database seeder creates **148 rows** of realistic sample data across all tables:

| Table | Rows | Description |
|-------|------|-------------|
| Users | 15 | 1 Admin, 4 Staff, 10 Owners |
| Owners | 10 | Pet owner profiles |
| Pets | 20 | 2 pets per owner (dogs and cats) |
| ServiceCategories | 8 | Main service types |
| ServiceSubtypes | 30 | Specific services |
| Appointments | 25 | Mix of past, upcoming, and cancelled |
| Notifications | 15 | System, appointment, and health reminders |
| SystemLogs | 20 | Audit trail entries |
| RefreshTokens | 5 | Active mobile sessions |

## Prerequisites

- SQL Server 2019+ or Azure SQL Database
- Database `ProjectPurrDB` already created
- Entity Framework migrations applied
- SQL Server Management Studio (SSMS) or Azure Data Studio

## Step 1: Generate Password Hashes

The seeder uses placeholder password hashes. You need to generate real ASP.NET Identity hashes.

### Option A: Using PowerShell Script (Recommended)

1. Open PowerShell in the project directory
2. Run the password hash generator:

```powershell
.\generate-password-hashes.ps1
```

3. Copy the generated hash from the output
4. Open `database-seeder.sql`
5. Find and replace all instances of:
   ```
   AQAAAAIAAYagAAAAELxxxHashedPasswordHerexxx
   ```
   with the generated hash

### Option B: Using C# Code

Create a simple console app:

```csharp
using Microsoft.AspNetCore.Identity;

var passwordHasher = new PasswordHasher<object>();
var hash = passwordHasher.HashPassword(new object(), "Password123!");
Console.WriteLine(hash);
```

Then replace the placeholder hashes in the SQL file.

### Option C: Skip Password Hashing (Testing Only)

For quick testing, you can skip this step, but users won't be able to login. You'll need to reset passwords through the application.

## Step 2: Run the Seeder

### Using SQL Server Management Studio (SSMS)

1. Open SSMS and connect to your SQL Server
2. Open the file: `database-seeder.sql`
3. Ensure the database context is set to `ProjectPurrDB`:
   ```sql
   USE ProjectPurrDB;
   GO
   ```
4. Click **Execute** or press `F5`
5. Review the output messages for confirmation

### Using Azure Data Studio

1. Open Azure Data Studio
2. Connect to your database
3. Open `database-seeder.sql`
4. Click **Run** or press `F5`
5. Check the results pane for success messages

### Using Command Line (sqlcmd)

```bash
sqlcmd -S localhost -d ProjectPurrDB -E -i database-seeder.sql
```

Replace `-E` with `-U username -P password` if using SQL authentication.

### Using PowerShell

```powershell
Invoke-Sqlcmd -ServerInstance "localhost" -Database "ProjectPurrDB" -InputFile "database-seeder.sql"
```

## Step 3: Verify Data

After running the seeder, verify the data was inserted:

```sql
-- Check row counts
SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users
UNION ALL
SELECT 'Owners', COUNT(*) FROM Owners
UNION ALL
SELECT 'Pets', COUNT(*) FROM Pets
UNION ALL
SELECT 'Appointments', COUNT(*) FROM Appointments;

-- View sample data
SELECT TOP 5 * FROM Users;
SELECT TOP 5 * FROM Pets;
SELECT TOP 5 * FROM Appointments;
```

Expected output:
- Users: 15
- Owners: 10
- Pets: 20
- Appointments: 25

## Sample User Accounts

### Admin Account
- **Email:** admin@happypaws.com
- **Password:** Password123!
- **Role:** Admin

### Staff Accounts
| Name | Email | Password |
|------|-------|----------|
| Sarah Johnson | sarah.johnson@happypaws.com | Password123! |
| Michael Chen | michael.chen@happypaws.com | Password123! |
| Emily Rodriguez | emily.rodriguez@happypaws.com | Password123! |
| David Williams | david.williams@happypaws.com | Password123! |

### Owner Accounts (Mobile App)
| Name | Email | Pets |
|------|-------|------|
| John Smith | john.smith@email.com | Max, Bella |
| Maria Garcia | maria.garcia@email.com | Luna, Simba |
| James Brown | james.brown@email.com | Rocky, Daisy |
| Lisa Anderson | lisa.anderson@email.com | Charlie, Milo |
| Robert Taylor | robert.taylor@email.com | Buddy, Molly |
| Jennifer Martinez | jennifer.martinez@email.com | Cooper, Chloe |
| William Lee | william.lee@email.com | Duke, Lucy |
| Jessica White | jessica.white@email.com | Bailey, Oscar |
| Daniel Harris | daniel.harris@email.com | Sadie, Toby |
| Amanda Clark | amanda.clark@email.com | Maggie, Whiskers |

All owner accounts use password: **Password123!**

## Sample Data Details

### Pets
- 20 pets total (10 dogs, 10 cats)
- Realistic breeds: Golden Retriever, Persian, German Shepherd, etc.
- Ages ranging from 2 to 7 years old
- Each owner has 2 pets

### Appointments
- **Completed (12):** Past appointments with notes
- **Pending (10):** Upcoming appointments scheduled
- **Cancelled (3):** Sample cancelled appointments

### Service Categories
1. Vaccination (8 subtypes)
2. Deworming & Preventives (5 subtypes)
3. Surgery (6 subtypes)
4. Grooming & Wellness (4 subtypes)
5. Diagnostics & Laboratory (4 subtypes)
6. Medications & Treatment (2 subtypes)
7. Confinement & Hospitalization (1 subtype)
8. Professional & Specialty Services (0 subtypes)

### Notifications
- System announcements
- Appointment reminders
- Vaccination due alerts
- Staff notifications
- Mix of read and unread statuses

## Resetting the Database

To clear all seeded data and start fresh:

### Option 1: Re-run the Seeder
The seeder automatically deletes all existing data before inserting new records.

Simply run `database-seeder.sql` again.

### Option 2: Manual Deletion

```sql
USE ProjectPurrDB;
GO

DELETE FROM SystemLogs;
DELETE FROM Notifications;
DELETE FROM Appointments;
DELETE FROM ServiceSubtypes;
DELETE FROM ServiceCategories;
DELETE FROM RefreshTokens;
DELETE FROM Pets;
DELETE FROM Owners;
DELETE FROM Users;

-- Reset identity seeds
DBCC CHECKIDENT ('SystemLogs', RESEED, 0);
DBCC CHECKIDENT ('Notifications', RESEED, 0);
DBCC CHECKIDENT ('Appointments', RESEED, 0);
DBCC CHECKIDENT ('ServiceSubtypes', RESEED, 0);
DBCC CHECKIDENT ('ServiceCategories', RESEED, 0);
DBCC CHECKIDENT ('RefreshTokens', RESEED, 0);
DBCC CHECKIDENT ('Pets', RESEED, 0);
DBCC CHECKIDENT ('Owners', RESEED, 0);
DBCC CHECKIDENT ('Users', RESEED, 0);
```

### Option 3: Drop and Recreate Database

```bash
# Using EF Core migrations
dotnet ef database drop --force
dotnet ef database update
```

Then run the seeder again.

## Testing with Sample Data

### Web Portal Testing
1. Login as admin: admin@happypaws.com / Password123!
2. Browse owners, pets, appointments
3. Create new appointments
4. Test service category management
5. View system logs and notifications

### Mobile App Testing
1. Login as owner: john.smith@email.com / Password123!
2. View dashboard with pets (Max, Bella)
3. Check upcoming appointments
4. Test booking new appointments
5. View vaccination due dates
6. Test 2FA flow (if enabled)

### API Testing
Use Postman or similar tool:

```bash
# Login
POST /api/v1/auth/login
{
  "email": "john.smith@email.com",
  "password": "Password123!"
}

# Get pets
GET /api/v1/pets
Authorization: Bearer <token>

# Get appointments
GET /api/v1/appointments
Authorization: Bearer <token>
```

## Customization

To customize the seeder data:

1. **Add More Users:** Copy and modify the INSERT statements in the Users section
2. **Add More Pets:** Add entries to the Pets INSERT block
3. **Change Passwords:** Update the password hash in Step 1
4. **Modify Dates:** Adjust appointment dates and timestamps
5. **Add Services:** Add more ServiceCategories and ServiceSubtypes

### Example: Adding a New Owner

```sql
-- Add to Users table
INSERT INTO Users (UserID, FirstName, LastName, Email, Phone, Password, Type, Status, CreatedAt, ProfileImage, TwoFactorEnabled, FailedLoginAttempts)
VALUES
(16, 'New', 'Owner', 'new.owner@email.com', '09181234577', 'YOUR_HASH_HERE', 'Owner', 'Active', GETDATE(), 'pet.png', 1, 0);

-- Add to Owners table
INSERT INTO Owners (OwnerID, UserID, Name, Email, Phone, Address)
VALUES
(11, 16, 'New Owner', 'new.owner@email.com', '09181234577', '123 New Street, Manila');

-- Add pets, appointments, etc.
```

## Troubleshooting

### Error: "Cannot insert explicit value for identity column"
**Solution:** Ensure `SET IDENTITY_INSERT TableName ON;` is present before INSERT and `OFF` after.

### Error: "Foreign key constraint violation"
**Solution:** Ensure parent records (Users, Owners) are inserted before child records (Pets, Appointments).

### Error: "Invalid column name"
**Solution:** Verify your database schema matches the seeder. Run migrations:
```bash
dotnet ef database update
```

### Error: "String or binary data would be truncated"
**Solution:** Check that string values don't exceed column max lengths (e.g., Phone = 11 digits).

### Passwords Not Working
**Solution:** Ensure you replaced the placeholder hashes with actual ASP.NET Identity hashes from Step 1.

### Two-Factor Authentication Issues
**Solution:**
- Verify email settings in appsettings.json
- Check User.TwoFactorEnabled values
- Test with users where TwoFactorEnabled = 0

## Production Warning

This seeder is for **development and testing only**.

**DO NOT** run this seeder on production databases as it will:
- Delete all existing data
- Reset identity seeds
- Use test passwords
- Create sample data

For production:
- Create separate production seed scripts
- Use strong, unique passwords
- Seed only reference data (ServiceCategories, ServiceSubtypes)
- Import real user data separately

## Next Steps

After seeding:

1. Test web portal login with admin account
2. Test mobile app login with owner account
3. Verify API endpoints work with sample data
4. Test appointment booking flow
5. Verify email notifications (if SMTP configured)
6. Test PDF generation for pet health cards
7. Review system logs and audit trail

## Support

For issues or questions:
- Check [SETUP.md](./SETUP.md) for configuration help
- Review [API_DOCUMENTATION.md](./API_DOCUMENTATION.md) for endpoint details
- See [ARCHITECTURE.md](./ARCHITECTURE.md) for database schema

---

**Last Updated:** February 2026
