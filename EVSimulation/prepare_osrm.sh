#!/bin/bash
set -e

DATA_DIR="data"
PROFILES_DIR="$DATA_DIR/profiles"
PBF_URL="https://download.geofabrik.de/europe/denmark-latest.osm.pbf"
PBF_FILE="$DATA_DIR/denmark-latest.osm.pbf"

mkdir -p "$DATA_DIR"

if [ ! -d "$PROFILES_DIR" ]; then
    echo "Fetching OSRM profiles..."
    wget -q -O "$DATA_DIR/osrm.zip" "https://github.com/Project-OSRM/osrm-backend/archive/refs/heads/master.zip"
    unzip -q "$DATA_DIR/osrm.zip" "osrm-backend-master/profiles/*" -d "$DATA_DIR"
    mv "$DATA_DIR/osrm-backend-master/profiles" "$PROFILES_DIR"
    rm -rf "$DATA_DIR/osrm-backend-master" "$DATA_DIR/osrm.zip"
fi

if [ ! -f "$PBF_FILE" ]; then
    echo "Downloading PBF file..."
    wget -q -O "$PBF_FILE" "$PBF_URL"
else
    echo "PBF file already exists, skipping download."
fi

echo "Changing working directory to data..."
cd "$DATA_DIR"

echo "Running OSRM extract..."
osrm-extract -p "profiles/car.lua" "denmark-latest.osm.pbf"

echo "Running OSRM contract (CH pipeline)..."
osrm-contract "denmark-latest.osm.pbf"

echo "Returning to project root..."
cd ..
echo "OSRM dataset preparation complete."
