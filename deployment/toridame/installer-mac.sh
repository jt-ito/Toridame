#!/usr/bin/env bash
# Builds Toridame.app and a .dmg from the folder package.sh left behind.
# usage: installer-mac.sh <rid> <version> <out-dir> <icon-png-512>
set -euo pipefail
rid=$1 version=$2 out=$3 icon=$4
dmg=$out/dmg
app=$dmg/Toridame.app
rm -rf "$dmg"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp -a "$out/stage/toridame" "$app/Contents/Resources/toridame"
rm -f "$app/Contents/Resources/toridame/run.sh"

cat > "$app/Contents/MacOS/Toridame" <<'SH'
#!/bin/sh
# Starts the server, keeps its data in ~/Library/Application Support/Toridame and opens the web page.
res="$(cd "$(dirname "$0")/../Resources/toridame" && pwd)"
data="$HOME/Library/Application Support/Toridame"
mkdir -p "$data"
(sleep 8; open "http://localhost:8096") &
exec "$res/jellyfin/jellyfin" --datadir "$data/data" --configdir "$data/config" --cachedir "$data/cache" \
    --logdir "$data/log" --webdir "$res/jellyfin-web" --ffmpeg "$res/ffmpeg/ffmpeg"
SH
chmod +x "$app/Contents/MacOS/Toridame" "$app/Contents/Resources/toridame/jellyfin/jellyfin" "$app"/Contents/Resources/toridame/ffmpeg/*

iconset=$dmg/Toridame.iconset
mkdir -p "$iconset"
for s in 16 32 64 128 256 512; do
    sips -z $s $s "$icon" --out "$iconset/icon_${s}x${s}.png" >/dev/null
done
cp "$iconset/icon_32x32.png" "$iconset/icon_16x16@2x.png"
cp "$iconset/icon_64x64.png" "$iconset/icon_32x32@2x.png"
cp "$iconset/icon_256x256.png" "$iconset/icon_128x128@2x.png"
cp "$iconset/icon_512x512.png" "$iconset/icon_256x256@2x.png"
rm "$iconset/icon_64x64.png"
iconutil -c icns "$iconset" -o "$app/Contents/Resources/Toridame.icns"
rm -rf "$iconset"

cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>Toridame</string>
<key>CFBundleDisplayName</key><string>Toridame</string>
<key>CFBundleIdentifier</key><string>io.github.jt-ito.toridame</string>
<key>CFBundleExecutable</key><string>Toridame</string>
<key>CFBundleIconFile</key><string>Toridame</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$version</string>
<key>CFBundleVersion</key><string>$version</string>
<key>LSMinimumSystemVersion</key><string>12.0</string>
</dict></plist>
PLIST

# Not notarized, but an ad-hoc signature is enough for Apple Silicon to run it.
codesign --force --deep -s - "$app"

ln -s /Applications "$dmg/Applications"
mkdir -p "$out/files"
hdiutil create -volname "Toridame" -srcfolder "$dmg" -ov -format UDZO "$out/files/toridame-$version-$rid.dmg"
