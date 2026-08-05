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
echo "Step 3: Compiling Xcode Project to IPA"
echo "========================================="

# Ensure credentials JSON path is absolute
if [[ "$CREDENTIALS_JSON" != /* ]]; then
    CREDENTIALS_JSON="$PROJECT_ROOT/$CREDENTIALS_JSON"
fi

# Verify Xcode project exists
if [ ! -d "$PROJECT_ROOT/Build-iOS" ]; then
    echo "ERROR: Xcode project directory not found at: Build-iOS/"
    echo "Did you run Step 2 (Unity Export) first?"
    exit 1
fi

# Navigate to project root to run our custom Fastfile
cd "$PROJECT_ROOT"

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
export TEAM_ID="$TEAM_ID"

# Run fastlane lane with strict error logging
echo "Running fastlane build_ipa lane..."
if ! fastlane build_ipa; then
    echo "========================================="
    echo "ERROR: fastlane build_ipa lane failed!"
    echo "========================================="
    exit 1
fi

if [ -f "Build-iOS/build/Unity-iPhone.ipa" ]; then
    echo "Success: IPA compiled and signed at Build-iOS/build/Unity-iPhone.ipa"
else
    echo "ERROR: Export failed. IPA file not found."
    exit 1
fi
