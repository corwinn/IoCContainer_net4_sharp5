cmake_minimum_required(VERSION 3.16)

set(APP Examples)

project(${APP} LANGUAGES CSharp)

file(GLOB_RECURSE ${APP}_src RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} "*.cs")
add_executable(${APP} ${${APP}_src}
    Properties/AssemblyInfo.cs
)

set_property(TARGET ${APP} PROPERTY ENABLE_EXPORTS OFF)
# Not "working" here (but at the main cmakelists):
#set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT ${APP})
# useless:
#set_property(TARGET ${APP} PROPERTY VS_GLOBAL_Prefer32Bit "false")
set_property(TARGET ${APP} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.0")
set(CMAKE_CSharp_FLAGS "/langversion:4")
target_link_libraries(${APP} ${ASM})

set_property(TARGET ${APP} PROPERTY VS_DOTNET_REFERENCES
    "System"
    "System.Core"
    "System.Data"
    "System.Xml"
    "System.Xml.Linq"
)
