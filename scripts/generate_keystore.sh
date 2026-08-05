#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "========================================="
echo "Android Release Keystore Generator"
echo "========================================="

read -p "Enter Keystore Filename [tubityx-release.keystore]: " KEYSTORE_NAME
KEYSTORE_NAME=${KEYSTORE_NAME:-"tubityx-release.keystore"}
KEYSTORE_PATH="$PROJECT_ROOT/$KEYSTORE_NAME"

if [ -f "$KEYSTORE_PATH" ]; then
    echo "WARNING: Keystore file already exists at: $KEYSTORE_PATH"
    read -p "Do you want to overwrite it? (y/n): " OVERWRITE
    if [ "$OVERWRITE" != "y" ]; then
        echo "Aborted."
        exit 0
    fi
    rm -f "$KEYSTORE_PATH"
fi

read -s -p "Enter Keystore Password (min 6 characters): " KEYSTORE_PASS
echo ""
while [ ${#KEYSTORE_PASS} -lt 6 ]; do
    echo "Password must be at least 6 characters."
    read -s -p "Enter Keystore Password: " KEYSTORE_PASS
    echo ""
done

read -p "Enter Key Alias [tubityx-key]: " KEY_ALIAS
KEY_ALIAS=${KEY_ALIAS:-"tubityx-key"}

read -s -p "Enter Key Password (press Enter if same as Keystore): " KEY_PASS
echo ""
KEY_PASS=${KEY_PASS:-"$KEYSTORE_PASS"}
while [ ${#KEY_PASS} -lt 6 ]; do
    echo "Password must be at least 6 characters."
    read -s -p "Enter Key Password: " KEY_PASS
    echo ""
done

read -p "Enter Your Name (Common Name) [Jeremy]: " CN
CN=${CN:-"Jeremy"}

read -p "Enter Organization Name [Dubchuck]: " O
O=${O:-"Dubchuck"}

echo "Generating keystore at: $KEYSTORE_PATH..."
keytool -genkeypair -v \
  -keystore "$KEYSTORE_PATH" \
  -alias "$KEY_ALIAS" \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000 \
  -storepass "$KEYSTORE_PASS" \
  -keypass "$KEY_PASS" \
  -dname "CN=$CN, OU=Development, O=$O, C=US"

echo "-----------------------------------------"
echo "SUCCESS! Keystore generated successfully."
echo "Path: $KEYSTORE_PATH"
echo "Alias: $KEY_ALIAS"
echo "========================================="
echo "You can now enter these parameters when configuring the Android pipeline!"
