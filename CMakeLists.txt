# The version I used ; feel free to comment this out, and face the consequences.
cmake_minimum_required(VERSION 3.16)

# Send ALL_BUILD to a folder specifying its purpose.
set_property(GLOBAL PROPERTY USE_FOLDERS ON)

set(ASM IoCContainer_net4_sharp5)

project(${ASM} LANGUAGES CSharp)

include(CSharpUtilities)
# Disable ZERO_CHECK - "visual studio" has "cmake" support - use it instead.
set(CMAKE_SUPPRESS_REGENERATION ON)

file(GLOB ${ASM}_src RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} "*.cs")
add_library(${ASM} SHARED ${${ASM}_src}
    Properties/AssemblyInfo.cs
    ${ASM}.config
)

set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT Examples)
set_property(TARGET ${ASM} PROPERTY ENABLE_EXPORTS ON)
# useless:
#set_property(TARGET ${ASM} PROPERTY VS_GLOBAL_Prefer32Bit "false")
set_property(TARGET ${ASM} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.0")
set(CMAKE_CSharp_FLAGS "/langversion:4")

set_property(TARGET ${ASM} PROPERTY VS_DOTNET_REFERENCES
    "System"
    "System.Core"
    "System.Data"
    "System.Xml"
    "System.Xml.Linq"
)
# Required, because "cmake" has no notion of "assembly".
add_subdirectory("Examples")
add_subdirectory("ExampleServiceAssembly")
add_subdirectory("Tests")
