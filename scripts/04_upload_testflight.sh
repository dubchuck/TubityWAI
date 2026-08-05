#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [ -f "$SCRIPT_DIR/build_config.sh" ]; then
    source "$SCRIPT_DIR/build_config.sh"
else
    echo "ERROR: Config file not found. Run the master script run_all.sh first."
    exit 1
fi

echo "========================================="
echo "Step 4: Uploading IPA to TestFlight"
echo "========================================="

echo "Select Deployment Action:"
echo "1) Full Release (Upload Build + Push Metadata & Screenshots) [Recommended]"
echo "2) Upload Build Only (No Metadata/Screenshots)"
echo "3) Push Metadata & Screenshots Only (No Build)"
read -p "Choose option (1, 2, or 3) [1]: " ACTION_CHOICE
ACTION_CHOICE=${ACTION_CHOICE:-"1"}

IPA_PATH="$PROJECT_ROOT/Build-iOS/build/Unity-iPhone.ipa"

if [ "$ACTION_CHOICE" == "1" ] || [ "$ACTION_CHOICE" == "2" ]; then
    if [ ! -f "$IPA_PATH" ]; then
        echo "ERROR: IPA file not found at: $IPA_PATH"
        echo "Did you run Step 3 (Build IPA) first?"
        exit 1
    fi
fi

# Ensure credentials JSON path is absolute
if [[ "$CREDENTIALS_JSON" != /* ]]; then
    CREDENTIALS_JSON="$PROJECT_ROOT/$CREDENTIALS_JSON"
fi

# Parse credentials
KEY_ID=$(grep -o '"key_id": *"[^"]*"' "$CREDENTIALS_JSON" | cut -d'"' -f4)
ISSUER_ID=$(grep -o '"issuer_id": *"[^"]*"' "$CREDENTIALS_JSON" | cut -d'"' -f4)
KEY_CONTENT_ESCAPED=$(grep -o '"key": *"[^"]*"' "$CREDENTIALS_JSON" | cut -d'"' -f4)

if [ -z "$KEY_ID" ] || [ -z "$ISSUER_ID" ] || [ -z "$KEY_CONTENT_ESCAPED" ]; then
    echo "ERROR: Failed to parse credentials from $CREDENTIALS_JSON"
    exit 1
fi

# Create a secure temporary .p8 file for Fastlane to read directly
TEMP_KEY_PATH="$SCRIPT_DIR/temp_key.p8"
printf '%b' "$KEY_CONTENT_ESCAPED" > "$TEMP_KEY_PATH"
chmod 600 "$TEMP_KEY_PATH"

# Ensure we clean up the temporary key file on script exit
trap 'rm -f "$TEMP_KEY_PATH" 2>/dev/null || true' EXIT

export KEY_ID="$KEY_ID"
export ISSUER_ID="$ISSUER_ID"
export KEY_FILEPATH="$TEMP_KEY_PATH"

if [ "$ACTION_CHOICE" == "3" ]; then
    echo "Uploading local metadata and screenshots to App Store Connect..."
    fastlane upload_metadata
    echo "Success: Metadata and screenshots successfully updated!"
elif [ "$ACTION_CHOICE" == "2" ]; then
    echo "Select Upload Tool for Build:"
    echo "1) Fastlane Pilot (Recommended)"
    echo "2) Apple Native Transporter CLI"
    read -p "Choose option (1 or 2) [1]: " UPLOAD_CHOICE
    UPLOAD_CHOICE=${UPLOAD_CHOICE:-"1"}
    
    if [ "$UPLOAD_CHOICE" == "2" ]; then
        echo "Uploading build via native xcrun transporter..."
        xcrun transporter -m upload \
          -f "$IPA_PATH" \
          -apiKey "$KEY_ID" \
          -apiIssuer "$ISSUER_ID"
    else
        echo "Uploading build via Fastlane..."
        fastlane upload_build_only
    fi
    echo "Success: Build successfully uploaded to TestFlight!"
else
    echo "Uploading build and metadata via Fastlane..."
    fastlane upload_to_testflight
    echo "Success: Build, metadata, and screenshots successfully uploaded/updated!"
fi
