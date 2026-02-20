#!/bin/bash
set -e

DATA_DIR="data"
PROFILES_DIR="$DATA_DIR/profiles"



PBF_FILE="$DATA_DIR/output.osm.pbf"

gh repo clone Project-OSRM/osrm-backend
cd osrm-backend 
mkdir -p build
cd build
cmake ..
make -j4
sudo make install

cd ..
cd ..

if [ ! -d "$PROFILES_DIR" ]; then
    echo "Fetching OSRM profiles..."
    wget -q -O "$DATA_DIR/osrm.zip" "https://github.com/Project-OSRM/osrm-backend/archive/refs/heads/master.zip"
    unzip -q "$DATA_DIR/osrm.zip" "osrm-backend-master/profiles/*" -d "$DATA_DIR"
    mv "$DATA_DIR/osrm-backend-master/profiles" "$PROFILES_DIR"
    rm -rf "$DATA_DIR/osrm-backend-master" "$DATA_DIR/osrm.zip"
fi

echo "Changing working directory to data..."
cd "$DATA_DIR"

wget http://download.geofabrik.de/europe/denmark-latest.osm.pbf

osmium cat denmark-latest.osm.pbf -o denmark.osm

osmosis \
  --read-xml denmark.osm \
  --tf reject-nodes "3dr:type=*" \
  --tf reject-nodes "abandoned=*" \
  --tf reject-nodes "abandoned:*=*" \
  --tf reject-nodes "aerialway=*" \
  --tf reject-nodes "aerodrome=*" \
  --tf reject-nodes "aeroway=*" \
  --tf reject-nodes "aircraft:type=*" \
  --tf reject-nodes "archaeological_site=*" \
  --tf reject-nodes "artwork_type=*" \
  --tf reject-nodes "attraction=*" \
  --tf reject-nodes "bunker_type=*" \
  --tf reject-nodes "castle=*" \
  --tf reject-nodes "cemetery=*" \
  --tf reject-nodes "circuit=*" \
  --tf reject-nodes "climbing=*" \
  --tf reject-nodes "communication=*" \
  --tf reject-nodes "communication:*=*" \
  --tf reject-nodes "craft=*" \
  --tf reject-nodes "emergency=*" \
  --tf reject-nodes "golf=*" \
  --tf reject-nodes "harbour=*" \
  --tf reject-nodes "historic=*" \
  --tf reject-nodes "leisure=*" \
  --tf reject-nodes "military=*" \
  --tf reject-nodes "natural=*" \
  --tf reject-nodes "railway=*" \
  --tf reject-nodes "recycling:*=*" \
  --tf reject-nodes "seamark=*" \
  --tf reject-nodes "seamark:*=*" \
  --tf reject-nodes "sport=*" \
  --tf reject-nodes "tourism=*" \
  --tf reject-nodes "waterway=*" \
  --tf reject-nodes highway=bus_stop \
  --tf reject-nodes public_transport=stop_position \
  --tf reject-nodes public_transport=platform \
  --tf reject-nodes amenity=bus_station \
  --tf reject-ways highway=track,path,footway,cycleway,living_street,pedestrian,bridleway,bus_guideway,service \
  --tf reject-ways public_transport=platform \
  --tf reject-ways amenity=bus_station \
  --tf reject-ways bus=only \
  --tf reject-ways "3dr:type=*" \
  --tf reject-ways "abandoned=*" \
  --tf reject-ways "abandoned:*=*" \
  --tf reject-ways "aerialway=*" \
  --tf reject-ways "aerodrome=*" \
  --tf reject-ways "aeroway=*" \
  --tf reject-ways "archaeological_site=*" \
  --tf reject-ways "bunker_type=*" \
  --tf reject-ways "cemetery=*" \
  --tf reject-ways "communication=*" \
  --tf reject-ways "communication:*=*" \
  --tf reject-ways "golf=*" \
  --tf reject-ways "harbour=*" \
  --tf reject-ways "historic=*" \
  --tf reject-ways "landuse=*" \
  --tf reject-ways "leisure=*" \
  --tf reject-ways "military=*" \
  --tf reject-ways "natural=*" \
  --tf reject-ways "power=*" \
  --tf reject-ways "railway=*" \
  --tf reject-ways "seamark=*" \
  --tf reject-ways "seamark:*=*" \
  --tf reject-ways "sport=*" \
  --tf reject-ways "tourism=*" \
  --tf reject-ways "waterway=*" \
  --tf reject-relations route=bus \
  --tf reject-relations route=train \
  --tf reject-relations route=railway \
  --used-node \
  --write-xml output.osm

osmium cat output.osm -o output.osm.pbf

echo "Running OSRM extract..."
osrm-extract -p "profiles/car.lua" "output.osm.pbf"

echo "Running OSRM contract (CH pipeline)..."
osrm-contract "output.osm.pbf"

echo "Returning to project root..."
cd ..
rm -rf osrm-backend

echo "OSRM dataset preparation complete."
