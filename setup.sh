#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$SCRIPT_DIR/docker"
TEMPLATE="$DOCKER_DIR/seed-template.sql"
OUTPUT="$DOCKER_DIR/seed-data.sql"
ENV_FILE="$DOCKER_DIR/.env"

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
    local error_msg="$4"
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

# --- Header ---

echo "============================================"
echo "  PurrVet Docker Setup"
echo "============================================"
echo ""
echo "This script will set up the PurrVet application"
echo "with a SQL Server database in Docker containers."
echo ""
echo "Password for all accounts: Password123!"
echo ""

# --- Gmail SMTP credentials ---

echo "--------------------------------------------"
echo "  Gmail SMTP Configuration"
echo "--------------------------------------------"
echo "A Gmail account is required for sending"
echo "verification codes and notifications."
echo ""

prompt_value "Gmail address: " GMAIL_EMAIL "email"
prompt_value "Gmail app password: " GMAIL_APP_PASSWORD "required"
echo ""

# --- Admin credentials ---

echo "--------------------------------------------"
echo "  Admin Account"
echo "--------------------------------------------"

prompt_value "Admin first name: " ADMIN_FIRST_NAME "required"
prompt_value "Admin last name: " ADMIN_LAST_NAME "required"
prompt_value "Admin email: " ADMIN_EMAIL "email"
echo ""

# --- Staff credentials ---

echo "--------------------------------------------"
echo "  Staff Account"
echo "--------------------------------------------"

prompt_value "Staff first name: " STAFF_FIRST_NAME "required"
prompt_value "Staff last name: " STAFF_LAST_NAME "required"
prompt_value "Staff email: " STAFF_EMAIL "email"
echo ""

# --- Owner credentials ---

echo "--------------------------------------------"
echo "  Owner Account"
echo "--------------------------------------------"

prompt_value "Owner first name: " OWNER_FIRST_NAME "required"
prompt_value "Owner last name: " OWNER_LAST_NAME "required"
prompt_value "Owner email: " OWNER_EMAIL "email"
prompt_value "Owner phone: " OWNER_PHONE "phone"
echo ""

# --- Derived values ---

OWNER_FULL_NAME="$OWNER_FIRST_NAME $OWNER_LAST_NAME"

# --- Generate seed-data.sql from template ---

echo "--------------------------------------------"
echo "  Generating configuration..."
echo "--------------------------------------------"

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

# --- Generate .env file ---

cat > "$ENV_FILE" << EOF
GMAIL_EMAIL=$GMAIL_EMAIL
GMAIL_APP_PASSWORD=$GMAIL_APP_PASSWORD
EOF

echo "Environment file generated: $ENV_FILE"
echo ""

# --- Summary ---

echo "============================================"
echo "  Account Summary"
echo "============================================"
echo ""
echo "Admin:  $ADMIN_FIRST_NAME $ADMIN_LAST_NAME ($ADMIN_EMAIL)"
echo "Staff:  $STAFF_FIRST_NAME $STAFF_LAST_NAME ($STAFF_EMAIL)"
echo "Owner:  $OWNER_FULL_NAME ($OWNER_EMAIL)"
echo ""
echo "Password for all accounts: Password123!"
echo ""

# --- Start Docker ---

echo "--------------------------------------------"
echo "  Starting Docker containers..."
echo "--------------------------------------------"
echo ""

docker compose -f "$DOCKER_DIR/docker-compose.yml" --env-file "$ENV_FILE" up --build

