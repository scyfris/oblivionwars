cmake_minimum_required (VERSION 2.6)
project (Oblivion_Engine)

#Version
set (OVN_VERSION_MAJOR 0)
set (OVN_VERSION_MINOR 1)

# Configure a header file to pass some of the CMake settings
# to the source code
#configure_file (
#  "$(PROJECT_SOURCE_DIR)/ovn_config.h.in"
#  "$(PROJECT_BINARY_DIR)/ovn_config.h"
#)

# Add the binary tree to the search path for include files
# so that we will find ovn_config.h
include_directories("${PROJECT_BINARY_DIR}")
include_directories(../SDL2-2.0.4/include)

# Add the executable
add_executable(Oblivion_Engine ../source/main.cpp)
target_link_libraries(Oblivion_Engine ${CMAKE_SOURCE_DIR}/../SDL2-2.0.4/build/x64/build/.libs/libSDL2-2.0.0.dylib)
