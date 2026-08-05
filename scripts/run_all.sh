#!/bin/bash
# Master pipeline script to manage Unity iOS / Android deployment pipelines

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="$SCRIPT_DIR/build_config.sh"

# Ensure we are running from project root or scripts directory
cd "$SCRIPT_DIR/.."

# Load or initialize configurations
if [ -f "$CONFIG_FILE" ]; then
    echo "Loading cached configurations from scripts/build_config.sh..."
    source "$CONFIG_FILE"
else
    echo "========================================="
    echo "Welcome to the TubityX Build & Distribution Pipeline"
    echo "========================================="
    echo "Select target platform:"
    echo "1) iOS (Apple App Store)"
    echo "2) Android (Google Play Store)"
    read -p "Choose option (1 or 2): " PLATFORM_CHOICE
    
    # Common variables
    read -p "Enter App Name [TubityX]: " APP_NAME
    APP_NAME=${APP_NAME:-"TubityX"}

    read -p "Enter Bundle Identifier / Package Name [com.dubchuck.tubityx]: " BUNDLE_ID
    BUNDLE_ID=${BUNDLE_ID:-"com.dubchuck.tubityx"}
    
    if [ "$PLATFORM_CHOICE" == "2" ]; then
        # Android configurations
        TARGET_PLATFORM="android"
        
        read -p "Enter Keystore Path [tubityx-release.keystore]: " KEYSTORE_PATH
        KEYSTORE_PATH=${KEYSTORE_PATH:-"tubityx-release.keystore"}
        
        read -s -p "Enter Keystore Password: " KEYSTORE_PASS
        echo ""
        while [ -z "$KEYSTORE_PASS" ]; do
            read -s -p "Keystore Password is required: " KEYSTORE_PASS
            echo ""
        done
        
        read -p "Enter Key Alias [tubityx-key]: " KEY_ALIAS
        KEY_ALIAS=${KEY_ALIAS:-"tubityx-key"}
        
        read -s -p "Enter Key Password (press Enter if same as Keystore): " KEY_PASS
        echo ""
        KEY_PASS=${KEY_PASS:-"$KEYSTORE_PASS"}
        
        read -p "Enter Path to Google Play Service JSON [google_play_credentials.json]: " PLAY_CREDENTIALS_JSON
        PLAY_CREDENTIALS_JSON=${PLAY_CREDENTIALS_JSON:-"google_play_credentials.json"}
        
        # Write Android config
        cat <<EOF > "$CONFIG_FILE"
# Cached build variables
TARGET_PLATFORM="android"
APP_NAME="$APP_NAME"
BUNDLE_ID="$BUNDLE_ID"
KEYSTORE_PATH="$KEYSTORE_PATH"
KEYSTORE_PASS="$KEYSTORE_PASS"
KEY_ALIAS="$KEY_ALIAS"
KEY_PASS="$KEY_PASS"
PLAY_CREDENTIALS_JSON="$PLAY_CREDENTIALS_JSON"
EOF
    else
        # iOS configurations
        TARGET_PLATFORM="ios"
        
        read -p "Enter Apple Developer Team ID [8W7DKFU852]: " TEAM_ID
        TEAM_ID=${TEAM_ID:-"8W7DKFU852"}
        while [ -z "$TEAM_ID" ]; do
            read -p "Team ID is required. Please enter Team ID: " TEAM_ID
        done

        read -p "Enter SKU [tubityx-sku-001]: " SKU
        SKU=${SKU:-"tubityx-sku-001"}

        read -p "Enter Path to Credentials JSON [appstore_ios_fastland_credentials.json]: " CREDENTIALS_JSON
        CREDENTIALS_JSON=${CREDENTIALS_JSON:-"appstore_ios_fastland_credentials.json"}
        
        # Write iOS config
        cat <<EOF > "$CONFIG_FILE"
# Cached build variables
TARGET_PLATFORM="ios"
APP_NAME="$APP_NAME"
BUNDLE_ID="$BUNDLE_ID"
TEAM_ID="$TEAM_ID"
SKU="$SKU"
CREDENTIALS_JSON="$CREDENTIALS_JSON"
EOF
    fi
    
    chmod +x "$CONFIG_FILE"
    echo "Saved configuration to scripts/build_config.sh"
    echo "-----------------------------------------"
    # Reload config
    source "$CONFIG_FILE"
fi

show_menu() {
    if [ "$TARGET_PLATFORM" == "android" ]; then
        echo "========================================="
        echo "TubityX Android Build & Distribution Pipeline"
        echo "========================================="
        echo "1) Step 1: Export Android App Bundle from Unity"
        echo "2) Step 2: Upload AAB to Google Play Store"
        echo "3) Run ALL Steps sequentially (Full Pipeline)"
        echo "c) Clear cached configurations"
        echo "q) Quit"
    else
        echo "========================================="
        echo "TubityX iOS Build & Distribution Pipeline"
        echo "========================================="
        echo "1) Step 1: Create App Store Connect Record (produce)"
        echo "2) Step 2: Export Xcode Project from Unity (batchmode)"
        echo "3) Step 3: Compile Xcode Project to IPA (xcodebuild)"
        echo "4) Step 4: Upload IPA to TestFlight (pilot/transporter)"
        echo "5) Run ALL Steps sequentially (Full Pipeline)"
        echo "c) Clear cached configurations"
        echo "q) Quit"
    fi
    echo "-----------------------------------------"
}

while true; do
    show_menu
    read -p "Select an option: " OPTION
    if [ "$TARGET_PLATFORM" == "android" ]; then
        case $OPTION in
            1)
                bash "$SCRIPT_DIR/05_unity_android_export.sh"
                ;;
            2)
                bash "$SCRIPT_DIR/06_upload_google_play.sh"
                ;;
            3)
                echo "Running full Android pipeline..."
                bash "$SCRIPT_DIR/05_unity_android_export.sh"
                bash "$SCRIPT_DIR/06_upload_google_play.sh"
                echo "Android Pipeline Completed Successfully!"
                ;;
            c)
                rm -f "$CONFIG_FILE"
                echo "Cleared cached configurations. Restart script to re-configure."
                exit 0
                ;;
            q)
                echo "Exiting."
                exit 0
                ;;
            *)
                echo "Invalid option. Please try again."
                ;;
        esac
    else
        case $OPTION in
            1)
                bash "$SCRIPT_DIR/01_create_app_record.sh"
                ;;
            2)
                bash "$SCRIPT_DIR/02_unity_export.sh"
                ;;
            3)
                bash "$SCRIPT_DIR/03_build_ipa.sh"
                ;;
            4)
                bash "$SCRIPT_DIR/04_upload_testflight.sh"
                ;;
            5)
                echo "Running full iOS pipeline..."
                bash "$SCRIPT_DIR/01_create_app_record.sh"
                bash "$SCRIPT_DIR/02_unity_export.sh"
                bash "$SCRIPT_DIR/03_build_ipa.sh"
                bash "$SCRIPT_DIR/04_upload_testflight.sh"
                echo "iOS Pipeline Completed Successfully!"
                ;;
            c)
                rm -f "$CONFIG_FILE"
                echo "Cleared cached configurations. Restart script to re-configure."
                exit 0
                ;;
            q)
                echo "Exiting."
                exit 0
                ;;
            *)
                echo "Invalid option. Please try again."
                ;;
        esac
    fi
    echo ""
done
