# Sample Login Credentials

Quick reference for test accounts created by the database seeder.

**Default Password for All Accounts:** `Password123!`

## Admin Portal Access

### Administrator
```
Email: admin@happypaws.com
Password: Password123!
Role: Admin
Access: Full system access
```

## Staff Portal Access

### Staff Member 1
```
Email: sarah.johnson@happypaws.com
Password: Password123!
Role: Staff
Name: Sarah Johnson
```

### Staff Member 2
```
Email: michael.chen@happypaws.com
Password: Password123!
Role: Staff
Name: Michael Chen
```

### Staff Member 3
```
Email: emily.rodriguez@happypaws.com
Password: Password123!
Role: Staff
Name: Emily Rodriguez
```

### Staff Member 4
```
Email: david.williams@happypaws.com
Password: Password123!
Role: Staff
Name: David Williams
```

## Mobile App / Owner Access

### Owner 1 - John Smith
```
Email: john.smith@email.com
Password: Password123!
Pets: Max (Golden Retriever), Bella (Labrador)
Phone: 09181234567
```

### Owner 2 - Maria Garcia
```
Email: maria.garcia@email.com
Password: Password123!
Pets: Luna (Persian), Simba (Siamese)
Phone: 09181234568
```

### Owner 3 - James Brown
```
Email: james.brown@email.com
Password: Password123!
Pets: Rocky (German Shepherd), Daisy (Beagle)
Phone: 09181234569
```

### Owner 4 - Lisa Anderson
```
Email: lisa.anderson@email.com
Password: Password123!
Pets: Charlie (Poodle), Milo (Maine Coon)
Phone: 09181234570
```

### Owner 5 - Robert Taylor
```
Email: robert.taylor@email.com
Password: Password123!
Pets: Buddy (Bulldog), Molly (Shih Tzu)
Phone: 09181234571
```

### Owner 6 - Jennifer Martinez
```
Email: jennifer.martinez@email.com
Password: Password123!
Pets: Cooper (Cocker Spaniel), Chloe (Ragdoll)
Phone: 09181234572
```

### Owner 7 - William Lee
```
Email: william.lee@email.com
Password: Password123!
Pets: Duke (Boxer), Lucy (Dachshund)
Phone: 09181234573
```

### Owner 8 - Jessica White
```
Email: jessica.white@email.com
Password: Password123!
Pets: Bailey (Australian Shepherd), Oscar (British Shorthair)
Phone: 09181234574
```

### Owner 9 - Daniel Harris
```
Email: daniel.harris@email.com
Password: Password123!
Pets: Sadie (Pomeranian), Toby (Chihuahua)
Phone: 09181234575
```

### Owner 10 - Amanda Clark
```
Email: amanda.clark@email.com
Password: Password123!
Pets: Maggie (Border Collie), Whiskers (Domestic Shorthair)
Phone: 09181234576
```

## API Testing

### Example cURL Login (Owner)
```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.smith@email.com",
    "password": "Password123!"
  }'
```

### Postman Collection Variables
```
base_url: http://localhost:5000
admin_email: admin@happypaws.com
admin_password: Password123!
owner_email: john.smith@email.com
owner_password: Password123!
```

## Notes

- All passwords are set to `Password123!` for testing convenience
- Users should change passwords after first login in production
- Two-Factor Authentication is enabled for most accounts
- Some owners have 2FA disabled for testing purposes
- Admin and Staff accounts cannot login via mobile API
- Owner accounts cannot login via web portal

## Security Reminder

These credentials are for **development and testing only**.

**Never use these credentials in production:**
- Change all default passwords
- Use strong, unique passwords per user
- Enable 2FA for all accounts
- Rotate credentials regularly
- Monitor failed login attempts

---

Last Updated: February 2026
