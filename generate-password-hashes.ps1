# ============================================
# Password Hash Generator for Database Seeder
# ============================================
# This script generates ASP.NET Identity password hashes
# for use in the database seeder script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Password Hash Generator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK detected: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 9.0 SDK." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "This script will create a temporary C# console app to generate" -ForegroundColor Yellow
Write-Host "password hashes using ASP.NET Identity's IPasswordHasher." -ForegroundColor Yellow
Write-Host ""

# Create temporary directory
$tempDir = Join-Path $env:TEMP "PetCloudHashGenerator"
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

Write-Host "Creating temporary project..." -ForegroundColor Cyan

# Create console app
Push-Location $tempDir
dotnet new console -n HashGenerator --force | Out-Null

Set-Location HashGenerator

# Add required package
Write-Host "Adding Microsoft.AspNetCore.Identity package..." -ForegroundColor Cyan
dotnet add package Microsoft.AspNetCore.Identity --version 9.0.0 | Out-Null

# Create the hash generator program
$programCode = @'
using Microsoft.AspNetCore.Identity;

var passwordHasher = new PasswordHasher<object>();
var dummyUser = new object();

// Default password for all users
string defaultPassword = "Password123!";

Console.WriteLine("========================================");
Console.WriteLine("Generating Password Hashes");
Console.WriteLine("========================================");
Console.WriteLine($"Default Password: {defaultPassword}");
Console.WriteLine("");

// Generate one hash (same hash can be used for all users)
string hash = passwordHasher.HashPassword(dummyUser, defaultPassword);

Console.WriteLine("Generated Hash:");
Console.WriteLine(hash);
Console.WriteLine("");
Console.WriteLine("Copy this hash and replace all instances of:");
Console.WriteLine("'AQAAAAIAAYagAAAAELxxxHashedPasswordHerexxx'");
Console.WriteLine("in the database-seeder.sql file.");
Console.WriteLine("");
Console.WriteLine("Note: All users will have the same password: Password123!");
Console.WriteLine("Users should change their passwords after first login.");
Console.WriteLine("========================================");
'@

# Write the program
Set-Content -Path "Program.cs" -Value $programCode

Write-Host "Building and running hash generator..." -ForegroundColor Cyan
Write-Host ""

# Run the program
dotnet run

# Cleanup
Pop-Location
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "✓ Done!" -ForegroundColor Green
Write-Host ""
