#!/usr/bin/env bash
set -euo pipefail

runtime_id="${1:-linux-x64}"
package_version="${2:-0.0.0.14}"

if [[ ! "$package_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Invalid Pulse version: $package_version. Expected four numeric components such as 0.0.0.14." >&2
  exit 2
fi

case "$runtime_id" in
  linux-x64) deb_arch="amd64" ;;
  linux-arm64) deb_arch="arm64" ;;
  *) echo "Unsupported runtime: $runtime_id. Use linux-x64 or linux-arm64." >&2; exit 2 ;;
esac

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj"
artifact_root="$repo_root/artifacts/$runtime_id"
publish_dir="$artifact_root/publish"
stage_dir="$artifact_root/deb-root"

rm -rf "$artifact_root"
mkdir -p "$publish_dir" "$stage_dir/opt/pulse-platform" "$stage_dir/usr/bin" \
  "$stage_dir/usr/share/applications" "$stage_dir/usr/share/icons/hicolor/scalable/apps" "$stage_dir/DEBIAN"

dotnet publish "$project" -c Release -r "$runtime_id" --self-contained true --no-restore \
  -p:Version="$package_version" \
  -p:AssemblyVersion="$package_version" \
  -p:FileVersion="$package_version" \
  -p:InformationalVersion="Pulse Linux Beta $package_version" \
  -p:Product="Pulse Supernova Linux" \
  -p:PublishSingleFile=false -o "$publish_dir"

cp -a "$publish_dir/." "$stage_dir/opt/pulse-platform/"
install -m 0755 "$repo_root/packaging/pulse-platform-launcher" "$stage_dir/usr/bin/pulse-platform"
install -m 0644 "$repo_root/packaging/pulse-platform.desktop" "$stage_dir/usr/share/applications/pulse-platform.desktop"
install -m 0644 "$repo_root/src/Pulse.Platform.Linux/Assets/pulse-platform.svg" \
  "$stage_dir/usr/share/icons/hicolor/scalable/apps/pulse-platform.svg"

installed_size="$(du -sk "$stage_dir/opt/pulse-platform" | cut -f1)"
sed -e "s/@VERSION@/$package_version/g" -e "s/@ARCH@/$deb_arch/g" -e "s/@INSTALLED_SIZE@/$installed_size/g" \
  "$repo_root/packaging/control.in" > "$stage_dir/DEBIAN/control"

# Archive the immutable staged application copy by enumerating its top-level
# entries. Avoid archiving the staging directory's own "." metadata, which can
# change while its sibling Debian artifact is assembled and make GNU tar fail.
find "$stage_dir/opt/pulse-platform" -mindepth 1 -maxdepth 1 -printf '%f\0' \
  | sort -z \
  | tar -C "$stage_dir/opt/pulse-platform" --null --files-from=- \
      -czf "$artifact_root/pulse-platform-$package_version-$runtime_id.tar.gz"
dpkg-deb --build --root-owner-group "$stage_dir" "$artifact_root/pulse-platform_${package_version}_${deb_arch}.deb"

sha256sum "$artifact_root"/*.tar.gz "$artifact_root"/*.deb > "$artifact_root/SHA256SUMS"
echo "Packages created in $artifact_root"
