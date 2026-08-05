#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "========================================="
echo "Step 2: Exporting iOS Project from Unity"
echo "========================================="

# Helper function to find Unity executable on macOS
find_unity_executable() {
    local target_version="$1"
    local default_path="/Applications/Unity/Hub/Editor/$target_version/Unity.app/Contents/MacOS/Unity"
    
    # 1. Check if the exact version from project settings exists
    if [ -f "$default_path" ]; then
        echo "$default_path"
        return 0
    fi
    
    # 2. Find all installed editor executables in the Unity Hub directory
    local installed_editors=()
    while IFS= read -r -d '' file; do
        installed_editors+=("$file")
    done < <(find /Applications/Unity/Hub/Editor -maxdepth 4 -name "Unity" -type f -path "*/Unity.app/Contents/MacOS/Unity" -print0 2>/dev/null)
    
    # 3. Check standard single-installation path
    if [ -f "/Applications/Unity/Unity.app/Contents/MacOS/Unity" ]; then
        installed_editors+=("/Applications/Unity/Unity.app/Contents/MacOS/Unity")
    fi
    
    # 4. Handle search results
    if [ ${#installed_editors[@]} -eq 0 ]; then
        echo ""
        return 1
    elif [ ${#installed_editors[@]} -eq 1 ]; then
        echo "${installed_editors[0]}"
        return 0
    else
        # Multiple versions: prompt for selection (redirect menu info to stderr to preserve stdout assignment)
        echo "WARNING: Target Unity version '$target_version' was not found." >&2
        echo "Multiple other Unity installations were detected. Please select one:" >&2
        for i in "${!installed_editors[@]}"; do
            local path_part="${installed_editors[$i]}"
            local ver_name=$(echo "$path_part" | awk -F'/Editor/' '{print $2}' | awk -F'/' '{print $1}')
            if [ -z "$ver_name" ]; then
                ver_name="Standard Application"
            fi
            echo "$((i+1))) Unity $ver_name (Path: $path_part)" >&2
        done
        
        while true; do
            read -p "Select Editor [1-${#installed_editors[@]}]: " CHOICE < /dev/tty
            if [[ "$CHOICE" =~ ^[0-9]+$ ]] && [ "$CHOICE" -ge 1 ] && [ "$CHOICE" -le "${#installed_editors[@]}" ]; then
                local selected_index=$((CHOICE - 1))
                echo "${installed_editors[$selected_index]}"
                return 0
            else
                echo "Invalid choice. Please select a number between 1 and ${#installed_editors[@]}." >&2
            fi
        done
    fi
}

# Auto-detect Unity Editor Version from ProjectSettings
VERSION_FILE="$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"
if [ -f "$VERSION_FILE" ]; then
    VERSION=$(grep "^m_EditorVersion:" "$VERSION_FILE" | awk '{print $2}')
    if [ -z "$VERSION" ]; then
        VERSION=$(cat "$VERSION_FILE" | awk '{print $2}')
    fi
else
    VERSION="6000.3.8f1" # default fallback
fi

# Call helper function to resolve Unity path
UNITY_PATH=$(find_unity_executable "$VERSION")

# Check if Unity binary was resolved
if [ -z "$UNITY_PATH" ]; then
    echo "Unity Editor not found at default location or Hub paths."
    read -p "Please enter the absolute path to your Unity Editor executable: " USER_UNITY_PATH
    if [ -f "$USER_UNITY_PATH" ]; then
        UNITY_PATH="$USER_UNITY_PATH"
    else
        echo "ERROR: Invalid Unity path. Aborting."
        exit 1
    fi
fi

# Load build configurations
if [ -f "$SCRIPT_DIR/build_config.sh" ]; then
    source "$SCRIPT_DIR/build_config.sh"
fi

export BUNDLE_ID="$BUNDLE_ID"
export APP_NAME="$APP_NAME"

echo "Using Unity Editor: $UNITY_PATH"
echo "Exporting Xcode project... Tailing logs in real-time:"
echo "------------------------------------------------------------"

# Clear old log file and initialize a new one
LOG_FILE="$PROJECT_ROOT/build_log.txt"
rm -f "$LOG_FILE"
touch "$LOG_FILE"

# Run batchmode build in the background
"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_ROOT" \
  -executeMethod TubityWAI.BuildPipeline.BuildiOSProject \
  -logFile "$LOG_FILE" &
UNITY_PID=$!

# Tail log file in the background (tail -n 0 shows only new content)
tail -n 0 -f "$LOG_FILE" &
TAIL_PID=$!

# Ensure we clean up the tail process if the script is interrupted
trap 'kill $TAIL_PID 2>/dev/null || true' EXIT

# Wait for Unity build process to complete
wait $UNITY_PID

# Kill the tail process upon completion
kill $TAIL_PID 2>/dev/null || true
trap - EXIT

echo "------------------------------------------------------------"

# Check if Build-iOS folder was generated
if [ -d "$PROJECT_ROOT/Build-iOS" ]; then
    echo "Success: Unity exported Xcode project to Build-iOS/"
else
    echo "ERROR: Unity export failed. Check log details in build_log.txt"
    exit 1
fi
