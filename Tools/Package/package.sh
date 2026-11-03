#!/bin/sh
# Builds TimberPerf as its own mod and zips the installed mod folder to ~/Downloads,
# named with the time it was built to the minute: TimberPerf-2026-11-03-10h20.zip.
# Pass another folder to zip into it. The older TimberPerf zips there are deleted.
set -e
repo="$(cd "$(dirname "$0")/../.." && pwd)"
mods="$HOME/Documents/Timberborn/Mods"
out="$(cd "${1:-$HOME/Downloads}" && pwd)"
(cd "$repo/TimberPerf" && dotnet build TimberPerf.csproj -c Release -nologo -v q)
zip="$out/TimberPerf-$(date +%Y-%m-%d-%Hh%M).zip"
rm -f "$zip"
(cd "$mods" && zip -qr "$zip" TimberPerf -x "*.DS_Store")
for old in "$out"/TimberPerf-[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-[0-9][0-9]h[0-9][0-9].zip; do
    if [ -e "$old" ] && [ "$old" != "$zip" ]; then rm -f "$old"; fi
done
echo "$zip"
