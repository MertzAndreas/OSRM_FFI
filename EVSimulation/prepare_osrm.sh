set -e

DATA_DIR="data"
OSM_FILE="output.osm.pbf"
OSRM_BACKEND="osrm-backend-362b388d7e0582291662105d7bfc004a3a44a393"
CAR_FILE="../"$OSRM_BACKEND"/profiles/car.lua"

# Package mappings for different systems
declare -A PACKAGE_MAPS
PACKAGE_MAPS[arch]="base-devel git cmake pkgconf bzip2 libxml2 libzip boost lua52 onetbb git-lfs"
PACKAGE_MAPS[debian]="build-essential git cmake pkg-config libbz2-dev libxml2-dev libzip-dev libboost-all-dev liblua5.2-dev libtbb-dev git-lfs"
PACKAGE_MAPS[macos]="cmake boost libxml2 libzip lua tbb git-lfs"
PACKAGE_MAPS[ubuntu]="build-essential git cmake pkg-config libbz2-dev libxml2-dev libzip-dev libboost-all-dev liblua5.2-dev libtbb-dev git-lfs"
# Package check commands
declare -A CHECK_COMMANDS
CHECK_COMMANDS[arch]="pacman -Qi"
CHECK_COMMANDS[debian]="dpkg -l"
CHECK_COMMANDS[macos]="brew list"
CHECK_COMMANDS[ubuntu]="apt list --installed"

detect_os() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "macos"
    elif [[ -f /etc/arch-release ]]; then
        echo "arch"
    elif [[ -f /etc/debian_version ]]; then
        echo "debian"
    elif [[ -f /etc/lsb-release ]]; then
        . /etc/lsb-release
        if [[ "$DISTRIB_ID" == "Ubuntu" ]]; then
            echo "ubuntu"
        else
            echo "unknown"
        fi    
    else
        echo "unknown"
    fi
}

check_package() {
    local package="$1"
    local os="$2"
    
    if [[ -n "${CHECK_COMMANDS[$os]}" ]]; then
        ${CHECK_COMMANDS[$os]} "$package" &> /dev/null
    else
        return 1
    fi
}

check_dependencies() {
    echo "Checking dependencies..."
    
    local os=$(detect_os)
    echo "Detected OS: $os"
    
    if [[ "$os" == "unknown" ]]; then
        echo "Unsupported OS"
        exit 1
    fi
    
    local packages=(${PACKAGE_MAPS[$os]})
    local missing=()
    
    for package in "${packages[@]}"; do
        check_package "$package" "$os" || missing+=("$package")
    done
    
    if [ ${#missing[@]} -ne 0 ]; then
        echo "Missing packages: ${missing[*]}"
        exit 1
    fi
    
    echo "All dependencies satisfied."
}

check_dependencies
# Ensure source exists
if [ ! -d "$OSRM_BACKEND" ]; then
    curl -L https://github.com/Project-OSRM/osrm-backend/archive/362b388d7e0582291662105d7bfc004a3a44a393.tar.gz | tar -xz
fi

# Build if binary doesn't exist
if [ ! -f "$OSRM_BACKEND/build/osrm-extract" ]; then
    cd "$OSRM_BACKEND"
    mkdir -p build && cd build
    cmake .. -DCMAKE_BUILD_TYPE=Release
    make -j4
    sudo make install
    cd ../..
fi

mkdir -p "$DATA_DIR" && cd "$DATA_DIR"

if [ ! -f "../$OSM_FILE" ]; then
    echo "OSM file not found: $OSM_FILE"
    exit 1
fi

if [ ! -d "$OSM_FILE" ]; then
    cp "../$OSM_FILE" "$OSM_FILE"
fi


echo "Running OSRM extract..."
osrm-extract -p "$CAR_FILE" "$OSM_FILE"

echo "Running OSRM contract (CH pipeline)..."
osrm-contract "$OSM_FILE"

echo "Returning to project root..."
cd ..
rm -rf "$OSRM_BACKEND"

echo "OSRM dataset preparation complete."