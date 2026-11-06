#!/usr/bin/env bash
# Builds one portable Toridame folder (server + web client + ffmpeg) and archives it.
# usage: package.sh <rid> <version> <web-dist-dir> <out-dir>
# rid: win-x64 | linux-x64 | linux-arm64 | osx-x64 | osx-arm64
# Leaves the unpacked folder in <out>/stage/toridame (the installer scripts start from it) and the archive in <out>/files.
set -euo pipefail
rid=$1 version=$2 web=$3 out=$4
root=$(cd "$(dirname "$0")/../.." && pwd)
stage=$out/stage/toridame

rm -rf "$out/stage" "$out/ff"
mkdir -p "$stage/ffmpeg" "$out/files" "$out/ff"

dotnet publish "$root/Jellyfin.Server/Jellyfin.Server.csproj" -c Release -r "$rid" --self-contained \
    -p:DebugSymbols=false -p:DebugType=None -o "$stage/jellyfin"
cp -r "$web" "$stage/jellyfin-web"

# Jellyfin's own ffmpeg build, so transcoding works without installing anything else.
case $rid in
    win-x64) ff=windows/latest-7.x/win64 ;;
    osx-x64) ff=macos/latest-7.x/x86_64 ;;
    osx-arm64) ff=macos/latest-7.x/arm64 ;;
    linux-x64) ff=linux/latest-7.x/amd64 ;;
    linux-arm64) ff=linux/latest-7.x/arm64 ;;
    *) echo "unknown rid $rid" >&2; exit 1 ;;
esac
base=https://repo.jellyfin.org/files/ffmpeg/$ff
name=$(curl -fsSL "$base/" | grep -o 'jellyfin-ffmpeg_[^"]*portable[^"]*\.\(zip\|tar\.xz\)' | head -1)
curl -fsSL -o "$out/ff/$name" "$base/$name"
case $name in
    *.zip) 7z x -y -o"$out/ff/x" "$out/ff/$name" >/dev/null ;;
    *) mkdir -p "$out/ff/x" && tar -xf "$out/ff/$name" -C "$out/ff/x" ;;
esac
find "$out/ff/x" \( -name 'ffmpeg' -o -name 'ffprobe' -o -name 'ffmpeg.exe' -o -name 'ffprobe.exe' \) -type f -exec cp {} "$stage/ffmpeg/" \;
ls "$stage/ffmpeg" | grep -q ffmpeg || { echo "ffmpeg not found in $name" >&2; exit 1; }

if [ "$rid" = win-x64 ]; then
    cat > "$stage/run.cmd" <<'CMD'
@echo off
rem Portable: everything Toridame writes stays in this folder.
set here=%~dp0
"%here%jellyfin\jellyfin.exe" --datadir "%here%data" --configdir "%here%config" --cachedir "%here%cache" --logdir "%here%log" --webdir "%here%jellyfin-web" --ffmpeg "%here%ffmpeg\ffmpeg.exe" %*
CMD
    sed -i 's/$/\r/' "$stage/run.cmd"
    (cd "$out/stage" && 7z a -tzip -mx=5 "$out/files/toridame-$version-$rid-portable.zip" toridame >/dev/null)
else
    cat > "$stage/run.sh" <<'SH'
#!/bin/sh
# Portable: everything Toridame writes stays in this folder.
here="$(cd "$(dirname "$0")" && pwd)"
exec "$here/jellyfin/jellyfin" --datadir "$here/data" --configdir "$here/config" --cachedir "$here/cache" \
    --logdir "$here/log" --webdir "$here/jellyfin-web" --ffmpeg "$here/ffmpeg/ffmpeg" "$@"
SH
    chmod +x "$stage/run.sh" "$stage/jellyfin/jellyfin" "$stage"/ffmpeg/*
    tar -czf "$out/files/toridame-$version-$rid-portable.tar.gz" -C "$out/stage" toridame
fi
ls -la "$out/files"
