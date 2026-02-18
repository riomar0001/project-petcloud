@echo off
setlocal EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "DOCKER_DIR=%SCRIPT_DIR%docker"
set "TEMPLATE=%DOCKER_DIR%\seed-template.sql"
set "OUTPUT=%DOCKER_DIR%\seed-data.sql"
set "ENV_FILE=%DOCKER_DIR%\.env"
set "DATABASE_DDL=%DOCKER_DIR%\database.sql"
set "DATABASE_DDL_CLEAN=%DOCKER_DIR%\database-clean.sql"

:: NOTE: The ^! escapes the ! so EnableDelayedExpansion does not strip it
set "SA_PASSWORD=Password123"
set "SQL_CONTAINER=projectpurr-sql"
set "SQL_PORT=1433"

:: ============================================
:: Check prerequisites
:: ============================================

where docker >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not installed or not in PATH.
    echo Please install Docker and try again.
    exit /b 1
)

if not exist "%TEMPLATE%" (
    echo ERROR: Seed template not found at %TEMPLATE%
    exit /b 1
)

if not exist "%DATABASE_DDL%" (
    echo ERROR: Database DDL not found at %DATABASE_DDL%
    exit /b 1
)

:: ============================================
:: Header
:: ============================================

echo ============================================
echo   PurrVet Docker Setup
echo ============================================
echo.
echo Password for all accounts: %SA_PASSWORD%
echo.

:: ============================================
:: STEP 1: Pull and start SQL Server container
:: ============================================

echo --------------------------------------------
echo   Step 1: Starting SQL Server...
echo --------------------------------------------
echo.

docker pull mcr.microsoft.com/mssql/server:2022-latest

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=%SA_PASSWORD%" -p "%SQL_PORT%:1433" --name "%SQL_CONTAINER%" -d mcr.microsoft.com/mssql/server:2022-latest

echo.
echo SQL Server container started. Waiting for it to be ready...

set ATTEMPT=0
set MAX_ATTEMPTS=30

:wait_loop
docker exec "%SQL_CONTAINER%" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SA_PASSWORD%" -No -Q "SELECT 1" >nul 2>&1
if not errorlevel 1 goto sql_ready

set /a ATTEMPT+=1
if %ATTEMPT% geq %MAX_ATTEMPTS% (
    echo ERROR: SQL Server did not become ready in time.
    docker stop "%SQL_CONTAINER%"
    exit /b 1
)
echo   Waiting... (%ATTEMPT%/%MAX_ATTEMPTS%)
timeout /t 3 /nobreak >nul
goto wait_loop

:sql_ready
echo SQL Server is ready.
echo.

:: ============================================
:: STEP 2: Create database and tables
:: ============================================

echo --------------------------------------------
echo   Step 2: Creating database and tables...
echo --------------------------------------------
echo.

docker exec "%SQL_CONTAINER%" /opt/mssql-tools18/bin/sqlcmd ^
    -S localhost -U sa -P "%SA_PASSWORD%" ^
    -No -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ProjectPurrDB') CREATE DATABASE ProjectPurrDB;"

echo Database 'ProjectPurrDB' ready.

:: Strip 'CREATE SCHEMA dbo;' line since dbo already exists in SQL Server by default
powershell -Command "(Get-Content '%DATABASE_DDL%') | Where-Object { $_ -notmatch 'CREATE SCHEMA dbo' } | Set-Content '%DATABASE_DDL_CLEAN%'"

docker cp "%DATABASE_DDL_CLEAN%" "%SQL_CONTAINER%:/tmp/database.sql"

docker exec "%SQL_CONTAINER%" /opt/mssql-tools18/bin/sqlcmd ^
    -S localhost -U sa -P "%SA_PASSWORD%" ^
    -No -d ProjectPurrDB ^
    -i /tmp/database.sql

echo Tables created successfully.
echo.

:: ============================================
:: STEP 3: Collect credentials
:: ============================================

echo --------------------------------------------
echo   Step 3: Enter Account Details
echo --------------------------------------------
echo.

echo   Gmail SMTP Configuration
echo.

echo   Admin Account
:input_admin_first
set /p ADMIN_FIRST_NAME="Admin first name: "
if "!ADMIN_FIRST_NAME!"=="" ( echo   This field cannot be empty. Please try again. & goto input_admin_first )
:input_admin_last
set /p ADMIN_LAST_NAME="Admin last name: "
if "!ADMIN_LAST_NAME!"=="" ( echo   This field cannot be empty. Please try again. & goto input_admin_last )
:input_admin_email
set /p ADMIN_EMAIL="Admin email: "
powershell -Command "if ('!ADMIN_EMAIL!' -match '^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$') { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 ( echo   Invalid email address. Please try again. & goto input_admin_email )
echo.

echo   Staff Account
:input_staff_first
set /p STAFF_FIRST_NAME="Staff first name: "
if "!STAFF_FIRST_NAME!"=="" ( echo   This field cannot be empty. Please try again. & goto input_staff_first )
:input_staff_last
set /p STAFF_LAST_NAME="Staff last name: "
if "!STAFF_LAST_NAME!"=="" ( echo   This field cannot be empty. Please try again. & goto input_staff_last )
:input_staff_email
set /p STAFF_EMAIL="Staff email: "
powershell -Command "if ('!STAFF_EMAIL!' -match '^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$') { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 ( echo   Invalid email address. Please try again. & goto input_staff_email )
echo.

echo   Owner Account
:input_owner_first
set /p OWNER_FIRST_NAME="Owner first name: "
if "!OWNER_FIRST_NAME!"=="" ( echo   This field cannot be empty. Please try again. & goto input_owner_first )
:input_owner_last
set /p OWNER_LAST_NAME="Owner last name: "
if "!OWNER_LAST_NAME!"=="" ( echo   This field cannot be empty. Please try again. & goto input_owner_last )
:input_owner_email
set /p OWNER_EMAIL="Owner email: "
powershell -Command "if ('!OWNER_EMAIL!' -match '^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$') { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 ( echo   Invalid email address. Please try again. & goto input_owner_email )
:input_owner_phone
set /p OWNER_PHONE="Owner phone: "
if "!OWNER_PHONE!"=="" ( echo   This field cannot be empty. Please try again. & goto input_owner_phone )
echo.

set "OWNER_FULL_NAME=%OWNER_FIRST_NAME% %OWNER_LAST_NAME%"

:: ============================================
:: STEP 4: Generate seed files and seed the DB
:: ============================================

echo --------------------------------------------
echo   Step 4: Seeding database...
echo --------------------------------------------
echo.

copy /Y "%TEMPLATE%" "%OUTPUT%" >nul

powershell -Command "(Get-Content '%OUTPUT%') -replace '{{ADMIN_FIRST_NAME}}', '%ADMIN_FIRST_NAME%' -replace '{{ADMIN_LAST_NAME}}', '%ADMIN_LAST_NAME%' -replace '{{ADMIN_EMAIL}}', '%ADMIN_EMAIL%' -replace '{{STAFF_FIRST_NAME}}', '%STAFF_FIRST_NAME%' -replace '{{STAFF_LAST_NAME}}', '%STAFF_LAST_NAME%' -replace '{{STAFF_EMAIL}}', '%STAFF_EMAIL%' -replace '{{OWNER_FIRST_NAME}}', '%OWNER_FIRST_NAME%' -replace '{{OWNER_LAST_NAME}}', '%OWNER_LAST_NAME%' -replace '{{OWNER_EMAIL}}', '%OWNER_EMAIL%' -replace '{{OWNER_PHONE}}', '%OWNER_PHONE%' -replace '{{OWNER_FULL_NAME}}', '%OWNER_FULL_NAME%' | Set-Content '%OUTPUT%'"

echo Seed data generated: %OUTPUT%

(
    echo SA_PASSWORD=%SA_PASSWORD%
    echo SQL_CONTAINER=%SQL_CONTAINER%
) > "%ENV_FILE%"

echo Environment file generated: %ENV_FILE%

docker cp "%OUTPUT%" "%SQL_CONTAINER%:/tmp/seed-data.sql"

docker exec "%SQL_CONTAINER%" /opt/mssql-tools18/bin/sqlcmd ^
    -S localhost -U sa -P "%SA_PASSWORD%" ^
    -No -d ProjectPurrDB ^
    -i /tmp/seed-data.sql

echo Database seeded successfully.
echo.

:: ============================================
:: Summary
:: ============================================

echo ============================================
echo   Setup Complete!
echo ============================================
echo.
echo Admin:  %ADMIN_FIRST_NAME% %ADMIN_LAST_NAME% (%ADMIN_EMAIL%)
echo Staff:  %STAFF_FIRST_NAME% %STAFF_LAST_NAME% (%STAFF_EMAIL%)
echo Owner:  %OWNER_FULL_NAME% (%OWNER_EMAIL%)
echo.
echo Password for all accounts: Password123!
echo.
echo   SQL Server  -^>  localhost:%SQL_PORT%
echo.

endlocal