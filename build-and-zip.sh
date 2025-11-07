#!/bin/bash
set -e

# ===== CONFIG =====
ROOT_DIR="$(pwd)"
PROJECT_PATH="QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain.csproj"
PLUGIN_NAME="QuickBrain"
PUBLISH_DIR="QuickBrain/Publish"

# ===== CLEAN UP =====
rm -rf "$PUBLISH_DIR"
rm -rf "QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain/bin"
rm -rf "QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain/obj"
rm -f "${ROOT_DIR}/${PLUGIN_NAME}-"*.zip

# ===== GET VERSION =====
VERSION=$(grep '"Version"' QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain/plugin.json | sed 's/.*"Version": "\([^"]*\)".*/\1/')
echo "📋 Plugin: $PLUGIN_NAME"
echo "📋 Version: $VERSION"

# ===== DEPENDENCIES TO EXCLUDE =====
DEPENDENCIES_TO_EXCLUDE="PowerToys.Common.UI.* PowerToys.ManagedCommon.* PowerToys.Settings.UI.Lib.* Wox.Infrastructure.* Wox.Plugin.*"

# ===== BUILD X64 =====
echo "🛠️  Building for x64..."
dotnet publish "$PROJECT_PATH" -c Release -r win-x64 --self-contained false

# ===== BUILD ARM64 =====
echo "🛠️  Building for ARM64..."
dotnet publish "$PROJECT_PATH" -c Release -r win-arm64 --self-contained false

# ===== PACKAGE FUNCTION =====
package_build() {
    ARCH=$1
    CLEAN_ARCH="${ARCH#win-}" # remove 'win-' prefix
    echo "📦 Packaging $CLEAN_ARCH..."

    PUBLISH_PATH="./QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain/bin/Release/net9.0-windows10.0.22621.0/$ARCH/publish"
    DEST="./QuickBrain/Publish/$CLEAN_ARCH"
    ZIP_PATH="${ROOT_DIR}/${PLUGIN_NAME}-${VERSION}-${CLEAN_ARCH}.zip"

    rm -rf "$DEST"
    mkdir -p "$DEST"
    cp -r "$PUBLISH_PATH"/* "$DEST/"

    # Remove PowerToys dependencies
    for dep in $DEPENDENCIES_TO_EXCLUDE; do
        find "$DEST" -name "$dep" -delete 2>/dev/null || true
    done

    # Create zip in root folder
    (cd "$DEST" && zip -r "$ZIP_PATH" .)
    echo "✅ Created: $(basename "$ZIP_PATH")"
}

# ===== PACKAGE BUILDS =====
package_build "win-x64"
package_build "win-arm64"

# ===== CHECKSUMS =====
echo "🔐 Generating checksums..."
for file in "${PLUGIN_NAME}-${VERSION}-"*.zip; do
    echo "$(basename "$file"): $(sha256sum "$file" | cut -d' ' -f1)"
done

echo "🎉 Done! ZIP files saved in root directory:"
ls -lh "${PLUGIN_NAME}-${VERSION}-"*.zip
