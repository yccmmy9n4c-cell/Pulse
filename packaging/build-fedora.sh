#!/usr/bin/env bash
set -euo pipefail

runtime_id="${1:-linux-x64}"
package_version="${2:-8.0.1.2}"

if [[ ! "$package_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Invalid Pulse version: $package_version. Expected four numeric components such as 8.0.1.2." >&2
  exit 2
fi

case "$runtime_id" in
  linux-x64) rpm_arch="x86_64" ;;
  linux-arm64) rpm_arch="aarch64" ;;
  *) echo "Unsupported runtime: $runtime_id. Use linux-x64 or linux-arm64." >&2; exit 2 ;;
esac

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj"
artifact_root="$repo_root/artifacts/$runtime_id"
publish_dir="$artifact_root/publish"
payload_dir="$artifact_root/payload"
rpm_root="$artifact_root/rpmbuild"

rm -rf "$artifact_root"
mkdir -p "$publish_dir" "$payload_dir/opt/pulse-platform" "$payload_dir/usr/bin" \
  "$payload_dir/usr/share/applications" "$payload_dir/usr/share/icons/hicolor/scalable/apps" \
  "$rpm_root/BUILD" "$rpm_root/BUILDROOT" "$rpm_root/RPMS" "$rpm_root/SOURCES" "$rpm_root/SPECS" "$rpm_root/SRPMS"

dotnet msbuild "$project" -target:Publish \
  -property:Configuration=Release \
  -property:RuntimeIdentifier="$runtime_id" \
  -property:SelfContained=true \
  -property:Restore=false \
  -property:Version="$package_version" \
  -property:AssemblyVersion="$package_version" \
  -property:FileVersion="$package_version" \
  -property:InformationalVersion="Pulse Linux ${package_version}FE" \
  -property:Product="Pulse Supernova Linux" \
  -property:PublishSingleFile=false \
  -property:PublishDir="$publish_dir/" \
  -nodeReuse:false

cp -a "$publish_dir/." "$payload_dir/opt/pulse-platform/"
install -m 0755 "$repo_root/packaging/pulse-platform-launcher" "$payload_dir/usr/bin/pulse-platform"
install -m 0644 "$repo_root/packaging/pulse-platform.desktop" "$payload_dir/usr/share/applications/pulse-platform.desktop"
install -m 0644 "$repo_root/src/Pulse.Platform.Linux/Assets/pulse-platform.svg" \
  "$payload_dir/usr/share/icons/hicolor/scalable/apps/pulse-platform.svg"

tar -C "$payload_dir" -czf "$artifact_root/pulse-platform-${package_version}FE-$runtime_id.tar.gz" .
cp "$artifact_root/pulse-platform-${package_version}FE-$runtime_id.tar.gz" "$rpm_root/SOURCES/pulse-platform-payload.tar.gz"
sed -e "s/@VERSION@/$package_version/g" -e "s/@ARCH@/$rpm_arch/g" \
  "$repo_root/packaging/pulse-platform.spec.in" > "$rpm_root/SPECS/pulse-platform.spec"

rpmbuild --define "_topdir $rpm_root" -bb "$rpm_root/SPECS/pulse-platform.spec"
cp "$rpm_root/RPMS/$rpm_arch/pulse-platform-${package_version}-1.$rpm_arch.rpm" "$artifact_root/"

(
  cd "$artifact_root"
  sha256sum ./*.tar.gz ./*.rpm | sed 's#  \./#  #'
) > "$artifact_root/SHA256SUMS"
echo "Fedora packages created in $artifact_root"
