#!/usr/bin/env bash
# Builds a .deb from the folder package.sh left behind. usage: installer-linux.sh <rid> <version> <out-dir>
set -euo pipefail
rid=$1 version=$2 out=$3
case $rid in linux-x64) arch=amd64 ;; linux-arm64) arch=arm64 ;; *) echo "bad rid" >&2; exit 1 ;; esac
pkg=$out/deb
rm -rf "$pkg"
mkdir -p "$pkg/DEBIAN" "$pkg/opt" "$pkg/usr/bin" "$pkg/lib/systemd/system"
cp -a "$out/stage/toridame" "$pkg/opt/toridame"
rm -f "$pkg/opt/toridame/run.sh"

cat > "$pkg/usr/bin/toridame" <<'SH'
#!/bin/sh
# Runs Toridame in the foreground with the same folders the service uses.
exec /opt/toridame/jellyfin/jellyfin --datadir /var/lib/toridame --configdir /etc/toridame --cachedir /var/cache/toridame \
    --logdir /var/log/toridame --webdir /opt/toridame/jellyfin-web --ffmpeg /opt/toridame/ffmpeg/ffmpeg "$@"
SH
chmod 755 "$pkg/usr/bin/toridame"

cat > "$pkg/lib/systemd/system/toridame.service" <<'UNIT'
[Unit]
Description=Toridame media server
After=network-online.target
Wants=network-online.target

[Service]
User=toridame
Group=toridame
StateDirectory=toridame
CacheDirectory=toridame
LogsDirectory=toridame
ConfigurationDirectory=toridame
ExecStart=/usr/bin/toridame
Restart=on-failure
TimeoutSec=120

[Install]
WantedBy=multi-user.target
UNIT

cat > "$pkg/DEBIAN/control" <<CONTROL
Package: toridame
Version: $version
Section: video
Priority: optional
Architecture: $arch
Depends: libfontconfig1, libfreetype6, libssl3 | libssl1.1, libicu74 | libicu72 | libicu70 | libicu-dev, adduser
Maintainer: jt-ito <jtbjr07@gmail.com>
Homepage: https://github.com/jt-ito/Toridame
Description: Toridame media server
 A personal fork of the Jellyfin media server, with its web client and
 Jellyfin's ffmpeg build. Runs as the toridame service on port 8096.
CONTROL

cat > "$pkg/DEBIAN/postinst" <<'SH'
#!/bin/sh
set -e
if [ "$1" = configure ]; then
    adduser --system --group --no-create-home --home /var/lib/toridame toridame >/dev/null 2>&1 || true
    systemctl daemon-reload >/dev/null 2>&1 || true
    systemctl enable --now toridame.service >/dev/null 2>&1 || true
fi
SH
cat > "$pkg/DEBIAN/prerm" <<'SH'
#!/bin/sh
set -e
if [ "$1" = remove ] || [ "$1" = upgrade ]; then
    systemctl stop toridame.service >/dev/null 2>&1 || true
fi
if [ "$1" = remove ]; then
    systemctl disable toridame.service >/dev/null 2>&1 || true
fi
SH
chmod 755 "$pkg/DEBIAN/postinst" "$pkg/DEBIAN/prerm"
mkdir -p "$out/files"
dpkg-deb --root-owner-group --build "$pkg" "$out/files/toridame_${version}_${arch}.deb"
