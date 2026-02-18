#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$SCRIPT_DIR/docker"
TEMPLATE="$DOCKER_DIR/seed-template.sql"
OUTPUT="$DOCKER_DIR/seed-data.sql"
ENV_FILE="$DOCKER_DIR/.env"
DATABASE_DDL="$DOCKER_DIR/database.sql"

SA_PASSWORD="Password123!"
SQL_CONTAINER="purrvet-sqlserver"
SQL_PORT=1433

# --- Validation helpers ---

validate_email() {
    local email="$1"
    if [[ "$email" =~ ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$ ]]; then
        return 0
    else
        return 1
    fi
}

validate_not_empty() {
    if [ -z "$1" ]; then
        return 1
    else
        return 0
    fi
}

validate_phone() {
    local phone="$1"
    local pattern='^[0-9+()" "-]{7,20}$'
    if [[ "$phone" =~ $pattern ]]; then
        return 0
    else
        return 1
    fi
}

prompt_value() {
    local prompt_text="$1"
    local var_name="$2"
    local validator="$3"
    local value=""

    while true; do
        read -rp "$prompt_text" value
        if [ "$validator" = "email" ]; then
            if validate_email "$value"; then
                break
            else
                echo "  Invalid email address. Please try again."
            fi
        elif [ "$validator" = "phone" ]; then
            if validate_phone "$value"; then
                break
            else
                echo "  Invalid phone number. Please try again."
            fi
        else
            if validate_not_empty "$value"; then
                break
            else
                echo "  This field cannot be empty. Please try again."
            fi
        fi
    done

    eval "$var_name='$value'"
}

# --- Check prerequisites ---

if ! command -v docker &> /dev/null; then
    echo "ERROR: Docker is not installed or not in PATH."
    echo "Please install Docker and try again."
    exit 1
fi

if [ ! -f "$TEMPLATE" ]; then
    echo "ERROR: Seed template not found at $TEMPLATE"
    exit 1
fi

if [ ! -f "$DATABASE_DDL" ]; then
    echo "ERROR: Database DDL not found at $DATABASE_DDL"
    exit 1
fi

# --- Header ---

echo "============================================"
echo "  PurrVet Docker Setup"
echo "============================================"
echo ""
echo "Password for all accounts: $SA_PASSWORD"
echo ""

# ============================================
# STEP 1: Pull and start SQL Server container
# ============================================

echo "--------------------------------------------"
echo "  Step 1: Starting SQL Server..."
echo "--------------------------------------------"
echo ""

docker pull mcr.microsoft.com/mssql/server:2022-latest

docker run -d \
    --name "$SQL_CONTAINER" \
    -e "ACCEPT_EULA=Y" \
    -e "SA_PASSWORD=$SA_PASSWORD" \
    -p "$SQL_PORT:1433" \
    mcr.microsoft.com/mssql/server:2022-latest

echo ""
echo "SQL Server container started. Waiting for it to be ready..."
until docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" \
    -Q "SELECT 1" &> /dev/null; do

done

echo "SQL Server is ready."
echo ""

# ============================================
# STEP 2: Create database and tables
# ============================================

echo "--------------------------------------------"
echo "  Step 2: Creating database and tables..."
echo "--------------------------------------------"
echo ""

docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" \
    -No -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ProjectPurrDB') CREATE DATABASE ProjectPurrDB;"

echo "Database 'ProjectPurrDB' ready."

docker cp "$DATABASE_DDL" "$SQL_CONTAINER:/tmp/database.sql"

docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" \
    -No -d ProjectPurrDB \
    -i /tmp/database.sql

echo "Tables created successfully."
echo ""

# ============================================
# STEP 3: Collect credentials
# ============================================

echo "--------------------------------------------"
echo "  Step 3: Enter Account Details"
echo "--------------------------------------------"
echo ""

echo "  Gmail SMTP Configuration"
echo ""
prompt_value "Gmail address: " GMAIL_EMAIL "email"
prompt_value "Gmail app password: " GMAIL_APP_PASSWORD "required"
echo ""

echo "  Admin Account"
prompt_value "Admin first name: " ADMIN_FIRST_NAME "required"
prompt_value "Admin last name: " ADMIN_LAST_NAME "required"
prompt_value "Admin email: " ADMIN_EMAIL "email"
echo ""

echo "  Staff Account"
prompt_value "Staff first name: " STAFF_FIRST_NAME "required"
prompt_value "Staff last name: " STAFF_LAST_NAME "required"
prompt_value "Staff email: " STAFF_EMAIL "email"
echo ""

echo "  Owner Account"
prompt_value "Owner first name: " OWNER_FIRST_NAME "required"
prompt_value "Owner last name: " OWNER_LAST_NAME "required"
prompt_value "Owner email: " OWNER_EMAIL "email"
prompt_value "Owner phone: " OWNER_PHONE "phone"
echo ""

OWNER_FULL_NAME="$OWNER_FIRST_NAME $OWNER_LAST_NAME"

# ============================================
# STEP 4: Generate seed files and seed the DB
# ============================================

echo "--------------------------------------------"
echo "  Step 4: Seeding database..."
echo "--------------------------------------------"
echo ""

cp "$TEMPLATE" "$OUTPUT"

sed -i "s/{{ADMIN_FIRST_NAME}}/$ADMIN_FIRST_NAME/g" "$OUTPUT"
sed -i "s/{{ADMIN_LAST_NAME}}/$ADMIN_LAST_NAME/g" "$OUTPUT"
sed -i "s/{{ADMIN_EMAIL}}/$ADMIN_EMAIL/g" "$OUTPUT"
sed -i "s/{{STAFF_FIRST_NAME}}/$STAFF_FIRST_NAME/g" "$OUTPUT"
sed -i "s/{{STAFF_LAST_NAME}}/$STAFF_LAST_NAME/g" "$OUTPUT"
sed -i "s/{{STAFF_EMAIL}}/$STAFF_EMAIL/g" "$OUTPUT"
sed -i "s/{{OWNER_FIRST_NAME}}/$OWNER_FIRST_NAME/g" "$OUTPUT"
sed -i "s/{{OWNER_LAST_NAME}}/$OWNER_LAST_NAME/g" "$OUTPUT"
sed -i "s/{{OWNER_EMAIL}}/$OWNER_EMAIL/g" "$OUTPUT"
sed -i "s/{{OWNER_PHONE}}/$OWNER_PHONE/g" "$OUTPUT"
sed -i "s/{{OWNER_FULL_NAME}}/$OWNER_FULL_NAME/g" "$OUTPUT"

echo "Seed data generated: $OUTPUT"

# Generate .env file
cat > "$ENV_FILE" << EOF
GMAIL_EMAIL=$GMAIL_EMAIL
GMAIL_APP_PASSWORD=$GMAIL_APP_PASSWORD
SA_PASSWORD=$SA_PASSWORD
SQL_CONTAINER=$SQL_CONTAINER
EOF

echo "Environment file generated: $ENV_FILE"

docker cp "$OUTPUT" "$SQL_CONTAINER:/tmp/seed-data.sql"

docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" \
    -No -d ProjectPurrDB \
    -i /tmp/seed-data.sql

echo "Database seeded successfully."
echo ""

# ============================================
# STEP 5: Build and start the application
# ============================================

echo "--------------------------------------------"
echo "  Step 5: Building and starting PurrVet app..."
echo "--------------------------------------------"
echo ""

docker build \
    -t purrvet-app \
    -f "$DOCKER_DIR/Dockerfile" \
    "$SCRIPT_DIR"

docker run -d \
    --name purrvet-app \
    --link "$SQL_CONTAINER:sqlserver" \
    -p 5090:5090 \
    --env-file "$ENV_FILE" \
    -e "ConnectionStrings__DefaultConnection=Server=sqlserver,$SQL_PORT;Database=ProjectPurrDB;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True;" \
    purrvet-app

echo "PurrVet application started."
echo ""

# ============================================
# Summary
# ============================================

echo "============================================"
echo "  Setup Complete!"
echo "============================================"
echo ""
echo "Admin:  $ADMIN_FIRST_NAME $ADMIN_LAST_NAME ($ADMIN_EMAIL)"
echo "Staff:  $STAFF_FIRST_NAME $STAFF_LAST_NAME ($STAFF_EMAIL)"
echo "Owner:  $OWNER_FULL_NAME ($OWNER_EMAIL)"
echo ""
echo "Password for all accounts: $SA_PASSWORD"
echo ""
echo "  SQL Server  →  localhost:$SQL_PORT"
echo "  PurrVet App →  http://localhost:5090"
echo ""