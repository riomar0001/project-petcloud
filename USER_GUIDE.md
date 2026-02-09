# PetCloud User Guide

Complete guide for using the PetCloud veterinary clinic management system.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Mobile App (Pet Owners)](#mobile-app-pet-owners)
3. [Web Portal (Staff & Admin)](#web-portal-staff--admin)
4. [Common Tasks](#common-tasks)
5. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Account Types

PetCloud has three types of users:

| Role | Access | Features |
|------|--------|----------|
| **Owner** | Mobile App | Manage pets, book appointments, view records |
| **Staff** | Web Portal | Manage appointments, pets, owners, services |
| **Admin** | Web Portal | Full system access, user management, reports |

### System Requirements

**Mobile App:**
- iOS 13.0+ or Android 8.0+
- Active internet connection
- Email account for 2FA verification

**Web Portal:**
- Modern web browser (Chrome, Firefox, Safari, Edge)
- Minimum screen resolution: 1280x720
- JavaScript enabled

---

## Mobile App (Pet Owners)

### 1. Registration & Login

#### Creating an Account

1. Download and open the PetCloud mobile app
2. Tap **"Sign Up"** on the login screen
3. Fill in your details:
   - First Name & Last Name (letters only)
   - Email address (will be your username)
   - Phone number (11 digits)
   - Password (minimum 8 characters)
   - Confirm password
4. Tap **"Create Account"**
5. Check your email for verification (if enabled)

#### Logging In

1. Enter your email and password
2. Tap **"Sign In"**
3. **If Two-Factor Authentication (2FA) is triggered:**
   - Check your email for 6-digit code
   - Enter the code within 10 minutes
   - Tap **"Verify"**
4. You'll be taken to your dashboard

**2FA is triggered when:**
- First login or no login in 30 days
- Login from new device
- Login from new location/IP address

#### Forgot Password

1. Tap **"Forgot Password?"** on login screen
2. Enter your registered email
3. Tap **"Send Reset Link"**
4. Check your email for password reset link
5. Click link and set new password
6. Return to app and login

---

### 2. Dashboard

Your dashboard shows:
- **Your Pets**: All registered pets with photos
- **Upcoming Appointments**: Pending and scheduled appointments
- **Vaccinations Due**: Vaccines due within 5 days
- **Deworming Due**: Deworming treatments due within 5 days

**Quick Actions:**
- Tap a pet to view details
- Tap an appointment to see more info
- Tap "+" to add a new pet

---

### 3. Managing Pets

#### Adding a Pet

1. Go to **"Pets"** tab
2. Tap **"Add Pet"** button
3. Fill in pet details:
   - **Name**: Your pet's name
   - **Type**: Dog or Cat
   - **Breed**: Select from list
   - **Birthdate**: Date of birth
   - **Sex**: Male or Female
   - **Color**: Pet's color/markings
   - **Photo**: Take photo or select from gallery
4. Tap **"Save"**

**Photo Tips:**
- Use good lighting
- Center your pet's face
- Photo will be cropped to square
- Supported formats: JPG, PNG
- Max size: 5MB

#### Viewing Pet Details

1. Go to **"Pets"** tab
2. Tap on a pet card
3. View:
   - Pet information
   - Age (calculated automatically)
   - Owner details
   - **Appointment History**: Past and upcoming appointments
     - Filter by service type
     - Search by notes
     - View service details, dates, notes

#### Downloading Pet Health Card

1. Open pet details
2. Tap **"Download Health Card"** (PDF icon)
3. PDF includes:
   - Pet photo and details
   - Vaccination records
   - Deworming records
   - QR code for verification
4. Share or save the PDF

**Uses for Pet Health Card:**
- Travel documentation
- Boarding facilities
- New veterinarian visits
- Emergency reference

---

### 4. Booking Appointments

#### Creating an Appointment

1. Go to **"Appointments"** tab
2. Tap **"Book Appointment"**
3. Select:
   - **Pet**: Choose from your pets
   - **Service Category**: Vaccination, Surgery, Grooming, etc.
   - **Service Type**: Specific service (e.g., Rabies Vaccine)
   - **Date**: Appointment date
   - **Time**: Available time slot (green = available)
   - **Notes**: Optional special instructions
4. Review details
5. Tap **"Confirm Booking"**

**Important Notes:**
- Appointments can be booked up to 30 days in advance
- Time slots are 30 minutes apart
- Clinic hours: 9:00 AM - 5:00 PM (typically)
- You'll receive confirmation notification

#### Viewing Appointments

**Appointments Tab:**
- **Upcoming**: Future appointments
- **Past**: Completed appointments
- **Cancelled**: Cancelled appointments

**Filter Options:**
- By status (Pending, Completed, Cancelled)
- By pet
- By date range

#### Cancelling an Appointment

1. Go to **"Appointments"** tab
2. Tap on the appointment
3. Tap **"Cancel Appointment"**
4. Confirm cancellation
5. Notification will be sent to clinic staff

**Cancellation Policy:**
- Can only cancel pending appointments
- Cannot cancel within 24 hours of appointment (contact clinic)
- Completed appointments cannot be cancelled

---

### 5. Notifications

#### Viewing Notifications

1. Tap **bell icon** in top navigation
2. See all notifications:
   - Appointment reminders
   - Vaccination due alerts
   - System announcements
3. Tap notification to view details
4. Unread notifications show badge count

#### Managing Notifications

- **Mark as Read**: Tap notification
- **Mark All as Read**: Tap "Mark All Read" button
- **Filter**: Show only unread

**Notification Types:**
- **Appointment**: Booking confirmations, reminders
- **Vaccination**: Due date alerts
- **Deworming**: Treatment reminders
- **System**: Important announcements

---

### 6. Profile Management

#### Viewing Profile

1. Go to **"Profile"** tab
2. View your account information:
   - Name, email, phone
   - Profile picture
   - Account created date
   - 2FA status

#### Updating Profile

1. Go to **"Profile"** tab
2. Tap **"Edit Profile"**
3. Update:
   - First Name
   - Last Name
   - Phone Number
4. Tap **"Save Changes"**

**Note:** Email cannot be changed (contact admin if needed)

#### Changing Profile Picture

1. Go to **"Profile"** tab
2. Tap on profile picture or camera icon
3. Choose:
   - **Take Photo**: Use camera
   - **Choose from Gallery**: Select existing photo
4. Crop if needed
5. Photo will be uploaded automatically

#### Changing Password

1. Go to **"Profile"** tab
2. Tap **"Change Password"**
3. Enter:
   - Current password
   - New password (min 8 characters)
   - Confirm new password
4. Tap **"Update Password"**
5. You'll be logged out - login with new password

#### Managing Two-Factor Authentication

1. Go to **"Profile"** tab
2. Find **"Two-Factor Authentication"** section
3. Toggle switch to enable/disable
4. When enabled, you'll receive email codes for login verification

**Recommendation:** Keep 2FA enabled for security

---

### 7. Service Categories

#### Viewing Available Services

1. Tap **"Services"** or when booking appointment
2. Browse service categories:
   - **Vaccination**: Rabies, Distemper, Parvovirus, etc.
   - **Deworming & Preventives**: Anti-parasite treatments
   - **Surgery**: Spay/Neuter, tumor removal, etc.
   - **Grooming & Wellness**: Baths, nail trimming, etc.
   - **Diagnostics & Laboratory**: Blood tests, X-rays
   - **Medications & Treatment**: Prescriptions, therapies
3. Each category shows available subtypes

---

## Web Portal (Staff & Admin)

### 1. Accessing the Portal

1. Open browser and navigate to the clinic URL
2. Click **"Sign In"**
3. Enter your email and password
4. (If using Microsoft Account) Sign in with Azure AD

**Default Admin Credentials:**
- Email: admin@happypaws.com
- Password: Admin@123
- **Change password immediately after first login**

---

### 2. Dashboard (Staff & Admin)

The dashboard provides an overview of clinic operations:

#### Quick Stats
- Total Pets
- Total Owners
- Appointments Today
- Pending Appointments

#### Today's Schedule
- List of appointments for current day
- Status indicators (Pending, Completed, Cancelled)
- Quick actions: View, Complete, Cancel

#### Recent Activity
- Latest appointments
- New pet registrations
- System alerts

#### Navigation Menu
- Dashboard
- Appointments
- Pets
- Owners
- Services
- Notifications
- Reports (Admin only)
- System Logs (Admin only)
- User Management (Admin only)

---

### 3. Appointment Management

#### Viewing Appointments

**List View:**
1. Go to **"Appointments"** in navigation
2. View all appointments in table format
3. Filter by:
   - Status (Pending, Completed, Cancelled)
   - Date range
   - Service category
   - Pet or owner name

**Calendar View:**
1. Switch to **"Calendar"** tab
2. See appointments on calendar
3. Click date to view appointments
4. Color-coded by status

#### Creating an Appointment (Staff)

1. Click **"New Appointment"**
2. Fill in details:
   - **Owner**: Search and select owner
   - **Pet**: Select from owner's pets
   - **Service Category**: Choose category
   - **Service Subtype**: Choose specific service
   - **Date & Time**: Select slot
   - **Notes**: Add any special instructions
   - **Due Date**: For recurring services (optional)
3. Click **"Save"**
4. Owner will receive notification

#### Updating Appointment

1. Open appointment details
2. Click **"Edit"**
3. Modify:
   - Date/Time
   - Service details
   - Notes
   - Status
4. Click **"Update"**

#### Completing an Appointment

1. Open appointment details
2. Add **appointment notes** (diagnosis, treatment, recommendations)
3. Set **Due Date** for next appointment (if recurring)
4. Change status to **"Completed"**
5. Click **"Save"**

#### Cancelling an Appointment

1. Open appointment details
2. Click **"Cancel Appointment"**
3. Confirm cancellation
4. Owner will be notified

---

### 4. Pet Management

#### Viewing Pets

1. Go to **"Pets"** in navigation
2. View all registered pets
3. Filter/Search by:
   - Owner name
   - Pet name
   - Type (Dog/Cat)
   - Breed

#### Adding a Pet (Staff)

1. Click **"Add Pet"**
2. Fill in details:
   - Owner (search and select)
   - Pet name
   - Type, breed, sex, color
   - Birthdate
   - Upload photo
3. Click **"Save"**

#### Viewing Pet Details

1. Click on pet name
2. View:
   - Basic information
   - Owner details
   - Full appointment history
   - Medical records
3. Actions:
   - Edit pet details
   - Add appointment
   - Generate pet card

#### Updating Pet Information

1. Open pet details
2. Click **"Edit"**
3. Modify information
4. Click **"Update"**

---

### 5. Owner Management

#### Viewing Owners

1. Go to **"Owners"** in navigation
2. View all pet owners
3. Search by name, email, or phone

#### Adding an Owner (Staff)

1. Click **"Add Owner"**
2. Fill in:
   - First Name, Last Name
   - Email (must be unique)
   - Phone (11 digits)
   - Address (optional)
   - Password (temporary)
3. Click **"Save"**
4. Owner can login and change password

#### Viewing Owner Details

1. Click on owner name
2. View:
   - Contact information
   - All pets
   - Appointment history
   - Account status
3. Actions:
   - Edit owner details
   - View/add pets
   - View appointments
   - Reset password (Admin only)
   - Activate/Deactivate account (Admin only)

---

### 6. Service Management (Admin)

#### Managing Service Categories

1. Go to **"Services"** > **"Categories"**
2. View all service categories
3. Actions:
   - **Add Category**: Create new category
   - **Edit**: Modify category name/description
   - **Delete**: Remove category (if no appointments)

#### Managing Service Subtypes

1. Go to **"Services"** > **"Subtypes"**
2. View subtypes by category
3. Actions:
   - **Add Subtype**: Add service under category
   - **Edit**: Modify subtype name
   - **Delete**: Remove subtype (if unused)

**Example Structure:**
- Category: Vaccination
  - Subtype: Rabies Vaccine
  - Subtype: Distemper Vaccine
  - Subtype: Parvovirus Vaccine

---

### 7. Notifications (Staff & Admin)

#### Viewing Notifications

1. Click **bell icon** in top right
2. View targeted notifications:
   - Role-specific alerts (Staff, Admin)
   - Personal notifications
3. Badge shows unread count

#### Creating System Notifications (Admin)

1. Go to **"Notifications"** > **"Create"**
2. Fill in:
   - **Type**: Appointment, User, System, etc.
   - **Message**: Notification text
   - **Target**:
     - All Users
     - Specific Role (Owner, Staff, Admin)
     - Specific User
3. Click **"Send"**

---

### 8. Reports (Admin)

#### Available Reports

1. **Appointment Report**
   - Total appointments by date range
   - Breakdown by status
   - By service category

2. **Revenue Report** (if implemented)
   - Income by service type
   - Monthly/Yearly comparisons

3. **Pet Statistics**
   - Total pets by type
   - Breed distribution
   - Age demographics

4. **Owner Statistics**
   - Total active owners
   - New registrations over time

#### Generating a Report

1. Go to **"Reports"**
2. Select report type
3. Choose:
   - Date range
   - Filters (status, category, etc.)
4. Click **"Generate"**
5. View in browser or **"Export to Excel/PDF"**

---

### 9. System Logs (Admin)

#### Viewing Audit Trail

1. Go to **"System Logs"**
2. View all system activities:
   - User logins
   - CRUD operations (Create, Update, Delete)
   - Module (User, Appointment, Pet, etc.)
   - Performed by (UserID or system)
   - Timestamp

#### Filtering Logs

- **By Date**: Select date range
- **By Module**: User, Appointment, Pet, etc.
- **By Action**: Create, Update, Delete
- **By User**: Filter by specific user

**Uses:**
- Security auditing
- Troubleshooting
- Compliance reporting

---

### 10. User Management (Admin)

#### Viewing All Users

1. Go to **"Users"** in navigation
2. View all system users (Admin, Staff, Owner)
3. Filter by:
   - Type (Admin, Staff, Owner)
   - Status (Active, Inactive)

#### Creating a User

1. Click **"Add User"**
2. Fill in:
   - First Name, Last Name
   - Email
   - Phone
   - **Type**: Admin, Staff, or Owner
   - Password (temporary)
   - Status: Active
3. Click **"Save"**
4. User receives email with credentials

#### Managing User Accounts

**Edit User:**
1. Open user details
2. Click **"Edit"**
3. Update information
4. Click **"Save"**

**Reset Password:**
1. Open user details
2. Click **"Reset Password"**
3. Generate new temporary password
4. Send to user via email

**Activate/Deactivate Account:**
1. Open user details
2. Toggle **"Status"** to Active/Inactive
3. Inactive users cannot login

---

## Common Tasks

### For Pet Owners

**Task: Book a vaccination appointment**
1. Open app > "Appointments" tab
2. Tap "Book Appointment"
3. Select pet, Vaccination category
4. Choose vaccine type (e.g., Rabies)
5. Select date & time
6. Tap "Confirm"

**Task: Check when next vaccine is due**
1. Open app > Dashboard
2. Check "Vaccinations Due" section
3. Or: Go to pet details > View appointment history

**Task: Download pet health card for travel**
1. Open app > "Pets" tab
2. Tap on pet
3. Tap "Download Health Card"
4. Save or share PDF

---

### For Staff

**Task: Complete an appointment and schedule next visit**
1. Open appointment from today's schedule
2. Click "Edit"
3. Add notes about visit
4. Set "Due Date" for next appointment (e.g., +1 year for annual vaccine)
5. Change status to "Completed"
6. Click "Save"

**Task: Register a new walk-in client**
1. Go to "Owners" > "Add Owner"
2. Fill in client details
3. Save owner
4. Go to "Pets" > "Add Pet"
5. Select the owner, fill pet details
6. Save pet
7. Create appointment if needed

**Task: Find all appointments for a specific pet**
1. Go to "Pets"
2. Search for pet name
3. Click on pet
4. View appointment history tab

---

### For Admin

**Task: Add a new service type**
1. Go to "Services" > "Subtypes"
2. Click "Add Subtype"
3. Select category
4. Enter subtype name
5. Click "Save"

**Task: Generate monthly appointment report**
1. Go to "Reports" > "Appointment Report"
2. Select date range (e.g., Feb 1 - Feb 28)
3. Choose filters if needed
4. Click "Generate"
5. Review or export to Excel

**Task: Investigate a login issue**
1. Go to "System Logs"
2. Filter by "Module: User" and "Action: Login"
3. Search for user's email
4. Review login attempts and errors
5. Check user status in "Users" section

---

## Troubleshooting

### Mobile App Issues

**Issue: Cannot login - "Invalid email or password"**
- Verify email and password are correct
- Check if Caps Lock is on
- Try "Forgot Password" to reset
- Contact admin if account is inactive

**Issue: 2FA code not received**
- Check spam/junk folder
- Wait a few minutes for email delivery
- Verify email address is correct
- Request new code (logout and login again)
- Contact clinic if issue persists

**Issue: 2FA code expired**
- Codes expire after 10 minutes
- Logout and login again to get new code
- Enter code quickly after receiving

**Issue: App shows "Network Error"**
- Check internet connection (WiFi or mobile data)
- Verify server is online (contact clinic)
- Try closing and reopening app
- Clear app cache (in phone settings)

**Issue: Pet photo not uploading**
- Check photo size (max 5MB)
- Use JPG or PNG format
- Try taking a new photo instead of selecting from gallery
- Check storage permissions in phone settings

**Issue: Appointment times not showing**
- Ensure you selected a date
- Clinic may be closed on that day
- Try a different date
- Contact clinic for availability

---

### Web Portal Issues

**Issue: Cannot access admin features**
- Verify your account type is "Admin"
- Log out and log back in
- Contact system administrator

**Issue: Database connection error**
- Check SQL Server is running
- Verify connection string in appsettings.json
- Check network connectivity
- Review server logs

**Issue: Email notifications not sending**
- Verify SMTP settings in appsettings.json
- Check email service account credentials
- Test email account in email client
- Review application logs for errors

**Issue: Unable to upload pet photo**
- Check upload folder exists (wwwroot/uploads/pets)
- Verify write permissions on folder
- Check file size and format
- Review IIS application pool permissions

**Issue: PDF generation fails**
- Ensure QuestPDF license is valid
- Check system fonts are installed
- Verify pet has appointment records
- Review application logs

---

### Account Issues

**Issue: Account locked after failed login attempts**
- Wait 3 minutes and try again
- Ensure correct password
- Use "Forgot Password" if needed
- Contact admin to unlock

**Issue: Account marked as inactive**
- Account inactive after 100 days of no login
- Contact clinic to reactivate
- Admin can change status to "Active"

**Issue: Cannot change email address**
- Email is permanent identifier
- Contact admin to change in database
- Or create new account and transfer data

---

### Data Issues

**Issue: Appointment not showing on mobile app**
- Wait a few seconds and pull to refresh
- Log out and log back in
- Verify appointment is not cancelled
- Contact clinic if persists

**Issue: Pet information incorrect**
- Owners: Contact clinic to update
- Staff: Edit pet details directly
- Check if recent changes are pending

**Issue: Wrong pet breed in list**
- Breeds loaded from CSV files
- Contact admin to add breed to CSV
- Breeds are in App_Data folder (DogBreeds.csv, CatBreeds.csv)

---

## Getting Help

### For Pet Owners

**In-App Support:**
- Tap "Help" in profile menu
- View FAQ section
- Contact clinic directly

**Contact Clinic:**
- Phone: (123) 456-7890
- Email: support@happypaws.com
- Hours: 9 AM - 5 PM, Mon-Fri

### For Staff & Admin

**Technical Support:**
- Check system logs for errors
- Review documentation
- Contact IT administrator
- Email: tech@happypaws.com

**Documentation:**
- [Setup Guide](./SETUP.md)
- [API Documentation](./API_DOCUMENTATION.md)
- [Architecture Guide](./ARCHITECTURE.md)

---

## Best Practices

### For Pet Owners

1. Keep profile information up to date
2. Enable 2FA for security
3. Book appointments in advance
4. Upload clear pet photos
5. Check notifications regularly
6. Download pet health cards before travel

### For Staff

1. Complete appointments promptly
2. Add detailed notes for medical records
3. Set due dates for recurring services
4. Verify owner contact information
5. Double-check appointment details before confirming

### For Admin

1. Regular database backups
2. Monitor system logs weekly
3. Review user accounts periodically
4. Keep services list up to date
5. Generate monthly reports
6. Update system credentials regularly

---

**Need more help?** Contact Happy Paws Veterinary Clinic support team.

**Last Updated:** February 2026
