cmake_minimum_required(VERSION 3.16)

project(ExampleServiceAssembly LANGUAGES CSharp)
file(GLOB ExampleServiceAssembly_src RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} "*.cs")
add_library(ExampleServiceAssembly SHARED ${ExampleServiceAssembly_src}
    Properties/AssemblyInfo.cs
)

add_custom_command(
    TARGET ExampleServiceAssembly
    POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy
    $<TARGET_FILE:ExampleServiceAssembly>
    $<TARGET_FILE_DIR:Examples>/$<TARGET_FILE_NAME:ExampleServiceAssembly>
)

set_property(TARGET ExampleServiceAssembly PROPERTY ENABLE_EXPORTS ON)
set_property(TARGET ExampleServiceAssembly PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.0")
set(CMAKE_CSharp_FLAGS "/langversion:4")

set_property(TARGET ExampleServiceAssembly PROPERTY VS_DOTNET_REFERENCES
    "System"
    "System.Core"
    "System.Data"
    "System.Xml"
    "System.Xml.Linq"
)
