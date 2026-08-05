#!/bin/bash
set -e

# Load build configurations
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/build_config.sh" ]; then
    source "$SCRIPT_DIR/build_config.sh"
else
    echo "ERROR: Config file not found. Run the master script run_all.sh first."
    exit 1
fi

echo "========================================="
echo "Step 1: Creating App Record on App Store"
echo "========================================="

# Verify credentials JSON exists
if [ ! -f "$CREDENTIALS_JSON" ]; then
    ROOT_CREDENTIALS="$SCRIPT_DIR/../$CREDENTIALS_JSON"
    if [ -f "$ROOT_CREDENTIALS" ]; then
        CREDENTIALS_JSON="$ROOT_CREDENTIALS"
    else
        echo "ERROR: Credentials JSON file not found at: $CREDENTIALS_JSON"
        exit 1
    fi
fi

# Parse credentials
KEY_ID=$(grep -o '"key_id": *"[^"]*"' "$CREDENTIALS_JSON" | cut -d'"' -f4)
ISSUER_ID=$(grep -o '"issuer_id": *"[^"]*"' "$CREDENTIALS_JSON" | cut -d'"' -f4)
KEY_CONTENT_ESCAPED=$(grep -o '"key": *"[^"]*"' "$CREDENTIALS_JSON" | cut -d'"' -f4)

# Create a secure temporary .p8 file for Fastlane to read directly
TEMP_KEY_PATH="$SCRIPT_DIR/temp_key.p8"
printf '%b' "$KEY_CONTENT_ESCAPED" > "$TEMP_KEY_PATH"
chmod 600 "$TEMP_KEY_PATH"

# Ensure we clean up the temporary key file on script exit
trap 'rm -f "$TEMP_KEY_PATH" 2>/dev/null || true' EXIT

export KEY_ID="$KEY_ID"
export ISSUER_ID="$ISSUER_ID"
export KEY_FILEPATH="$TEMP_KEY_PATH"
export BUNDLE_ID="$BUNDLE_ID"
export APP_NAME="$APP_NAME"
export SKU="$SKU"

# Run fastlane lane with strict error logging
echo "Running fastlane create_app lane..."
if ! fastlane create_app; then
    echo ""
    echo "========================================="
    echo "ERROR: fastlane create_app lane failed!"
    echo "Common Issues:"
    echo "1. The App Name '$APP_NAME' is already in use by another developer."
    echo "2. The Bundle ID '$BUNDLE_ID' has not been registered or is locked."
    echo "3. The API Key credentials in '$CREDENTIALS_JSON' are incorrect."
    echo "========================================="
    exit 1
fi

echo "Success: App Record registered successfully!"
