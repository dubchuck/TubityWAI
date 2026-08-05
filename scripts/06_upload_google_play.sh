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
echo "Step 6: Uploading AAB to Google Play Store"
echo "========================================="

# Ensure play credentials path is absolute
if [[ "$PLAY_CREDENTIALS_JSON" != /* ]]; then
    PLAY_CREDENTIALS_JSON="$PROJECT_ROOT/$PLAY_CREDENTIALS_JSON"
fi

if [ ! -f "$PLAY_CREDENTIALS_JSON" ]; then
    echo "ERROR: Google Play credentials JSON not found at: $PLAY_CREDENTIALS_JSON"
    echo "Please create a Google Play Console service account key and save it there."
    exit 1
fi

AAB_PATH="$PROJECT_ROOT/Build-Android/TubityX.aab"
if [ ! -f "$AAB_PATH" ]; then
    echo "ERROR: Android App Bundle (.aab) not found at: $AAB_PATH"
    echo "Did you run Step 5 (Unity Android Export) first?"
    exit 1
fi

# Navigate to project root to run our custom Fastfile
cd "$PROJECT_ROOT"

export PLAY_CREDENTIALS_JSON="$PLAY_CREDENTIALS_JSON"
export BUNDLE_ID="$BUNDLE_ID"

echo "Uploading signed Android App Bundle (.aab) to Google Play Console (internal track)..."
if ! fastlane android upload_to_play_store; then
    echo "========================================="
    echo "ERROR: Google Play upload failed!"
    echo "Common Issues:"
    echo "1. The package name '$BUNDLE_ID' has not been created manually in the Play Console yet."
    echo "2. The service account JSON credentials have expired or lack Google Play developer permissions."
    echo "3. The package version or version code matches an already uploaded release."
    echo "========================================="
    exit 1
fi

echo "Success: Build successfully uploaded to Google Play Internal track!"
