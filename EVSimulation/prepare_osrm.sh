set -e

DATA_DIR="data"
PROFILES_DIR="$DATA_DIR/profiles"

gh repo clone Project-OSRM/osrm-backend
cd osrm-backend
mkdir -p build
cd build
cmake ..
make -j4
sudo make install

cd ..
cd ..

mkdir -p "$DATA_DIR"

if [ ! -d "$PROFILES_DIR" ]; then
    echo "Fetching OSRM profiles..."
    wget -q -O "$DATA_DIR/osrm.zip" "https://github.com/Project-OSRM/osrm-backend/archive/refs/heads/master.zip"
    unzip -q "$DATA_DIR/osrm.zip" "osrm-backend-master/profiles/*" -d "$DATA_DIR"
    mv "$DATA_DIR/osrm-backend-master/profiles" "$PROFILES_DIR"
    rm -rf "$DATA_DIR/osrm-backend-master" "$DATA_DIR/osrm.zip"
fi

echo "Changing working directory to data..."
cp output.osm.pbf "$DATA_DIR"/output.osm.pbf
cd "$DATA_DIR"

echo "Running OSRM extract..."
osrm-extract -p "profiles/car.lua" "output.osm.pbf"

echo "Running OSRM contract (CH pipeline)..."
osrm-contract "output.osm.pbf"

echo "Returning to project root..."
cd ..
rm -rf osrm-backend

echo "OSRM dataset preparation complete."