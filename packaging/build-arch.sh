#!/usr/bin/env bash
set -euo pipefail

runtime_id="${1:-linux-x64}"
package_version="${2:-8.0.1.2}"

if [[ ! "$package_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Invalid Pulse version: $package_version. Expected four numeric components such as 8.0.1.2." >&2
  exit 2
fi

if [[ "$(id -u)" -eq 0 ]]; then
  echo "Arch packages must be created by an unprivileged build user." >&2
  exit 2
fi

case "$runtime_id" in
  linux-x64) package_arch="x86_64" ;;
  linux-arm64) package_arch="aarch64" ;;
  *) echo "Unsupported runtime: $runtime_id. Use linux-x64 or linux-arm64." >&2; exit 2 ;;
esac

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj"
artifact_root="$repo_root/artifacts/$runtime_id"
publish_dir="$artifact_root/publish"
payload_dir="$artifact_root/payload"
makepkg_dir="$artifact_root/makepkg"

rm -rf "$artifact_root"
mkdir -p "$publish_dir" "$payload_dir/opt/pulse-platform" "$payload_dir/usr/bin" \
  "$payload_dir/usr/share/applications" "$payload_dir/usr/share/icons/hicolor/scalable/apps" "$makepkg_dir"

dotnet msbuild "$project" -target:Publish \
  -property:Configuration=Release \
  -property:RuntimeIdentifier="$runtime_id" \
  -property:SelfContained=true \
  -property:Restore=false \
  -property:Version="$package_version" \
  -property:AssemblyVersion="$package_version" \
  -property:FileVersion="$package_version" \
  -property:InformationalVersion="Pulse Linux ${package_version}AE" \
  -property:Product="Pulse Supernova Linux" \
  -property:PublishSingleFile=false \
  -property:PublishDir="$publish_dir/" \
  -nodeReuse:false

cp -a "$publish_dir/." "$payload_dir/opt/pulse-platform/"
install -m 0755 "$repo_root/packaging/pulse-platform-launcher" "$payload_dir/usr/bin/pulse-platform"
install -m 0644 "$repo_root/packaging/pulse-platform.desktop" "$payload_dir/usr/share/applications/pulse-platform.desktop"
install -m 0644 "$repo_root/src/Pulse.Platform.Linux/Assets/pulse-platform.svg" \
  "$payload_dir/usr/share/icons/hicolor/scalable/apps/pulse-platform.svg"

tar -C "$payload_dir" -czf "$artifact_root/pulse-platform-${package_version}AE-$runtime_id.tar.gz" .
cp "$artifact_root/pulse-platform-${package_version}AE-$runtime_id.tar.gz" "$makepkg_dir/pulse-platform-payload.tar.gz"
payload_sha256="$(sha256sum "$makepkg_dir/pulse-platform-payload.tar.gz" | cut -d ' ' -f1)"
sed -e "s/@VERSION@/$package_version/g" -e "s/@ARCH@/$package_arch/g" \
  -e "s/@PAYLOAD_SHA256@/$payload_sha256/g" "$repo_root/packaging/PKGBUILD.in" > "$makepkg_dir/PKGBUILD"

(
  cd "$makepkg_dir"
  makepkg --force --cleanbuild --noconfirm --nodeps
)
cp "$makepkg_dir/pulse-platform-${package_version}-1-$package_arch.pkg.tar.zst" "$artifact_root/"

(
  cd "$artifact_root"
  sha256sum ./*.tar.gz ./*.pkg.tar.zst | sed 's#  \./#  #'
) > "$artifact_root/SHA256SUMS"
echo "Arch packages created in $artifact_root"
