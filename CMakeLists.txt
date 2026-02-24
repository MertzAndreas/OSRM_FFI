cmake_minimum_required(VERSION 3.10)
project(osrm_wrapper LANGUAGES CXX)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_BUILD_TYPE Release)


find_package(PkgConfig REQUIRED)
pkg_check_modules(OSRM REQUIRED libosrm)

find_package(Threads REQUIRED)

find_package(Boost REQUIRED COMPONENTS iostreams filesystem thread)

add_library(osrm_wrapper SHARED osrm_wrapper.cpp)

target_include_directories(osrm_wrapper PRIVATE
        ${OSRM_INCLUDE_DIRS}
        ${Boost_INCLUDE_DIRS}
)

if(NOT TARGET Boost::system)
    if(TARGET Boost::headers)
        add_library(Boost::system ALIAS Boost::headers)
    endif()
endif()

target_link_libraries(osrm_wrapper PRIVATE
        ${OSRM_LIBRARIES}
        Boost::iostreams
        Boost::filesystem
        Boost::thread
        Threads::Threads
)

if(TARGET Boost::system)
    target_link_libraries(osrm_wrapper PRIVATE Boost::system)
endif()

target_compile_options(osrm_wrapper PRIVATE ${OSRM_CFLAGS_OTHER})
