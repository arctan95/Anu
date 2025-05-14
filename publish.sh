#!/bin/sh

# Initialize variables
VERSION=""
OS=""

# Parse arguments
while [ "$#" -gt 0 ]; do
  case "$1" in
    -v|--version)
      VERSION="$2"
      shift 2
      ;;
    -o|--os)
      OS="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1"
      echo "Usage: $0 -o <os> -v <version>"
      exit 1
      ;;
  esac
done

# Validate input
if [ -z "$VERSION" ] || [ -z "$OS" ]; then
  echo "Error: missing required arguments"
  echo "Usage: $0 -o <os> -v <version>"
  exit 1
fi

# Determine file extension and output directory
if echo "$OS" | grep -qi "macos"; then
  EXT="dmg"
  OUTPUT_DIR="publish/macos"
elif echo "$OS" | grep -qi "windows"; then
  EXT="exe"
  OUTPUT_DIR="publish/windows"
else
  echo "Unsupported OS: $OS"
  exit 1
fi

FILENAME="Anu-${VERSION}-${OS}.${EXT}"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Run netsparkle-generate-appcast
~/.dotnet/tools/netsparkle-generate-appcast \
  --single-file "$FILENAME" \
  -n "Anu" \
  --file-version "$VERSION" \
  -o "$OS" \
  -p changelogs \
  -a "$OUTPUT_DIR" \
  -u "https://github.com/arctan95/Anu/releases/download/v${VERSION}/" \
  -l "https://arctan95.github.io/Anu/changelogs/"
