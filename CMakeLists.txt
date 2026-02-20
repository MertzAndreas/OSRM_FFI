cmake_minimum_required(VERSION 3.10)
project(osrm_wrapper)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_BUILD_TYPE Release)

# Find OSRM via pkg-config
find_package(PkgConfig REQUIRED)
pkg_check_modules(OSRM REQUIRED libosrm)

# Find remaining Boost components. System is now header-only and removed in 1.89.
find_package(Boost REQUIRED COMPONENTS iostreams filesystem)

add_library(osrm_wrapper SHARED osrm_wrapper.cpp)

# Create compatibility alias for Boost::system (now header-only)
if(NOT TARGET Boost::system)
    add_library(Boost::system ALIAS Boost::headers)
endif()

target_include_directories(osrm_wrapper PRIVATE 
    ${OSRM_INCLUDE_DIRS} 
    ${Boost_INCLUDE_DIRS}
)

target_link_libraries(osrm_wrapper PRIVATE 
    ${OSRM_LIBRARIES} 
    Boost::iostreams 
    Boost::filesystem
    Boost::headers
)
