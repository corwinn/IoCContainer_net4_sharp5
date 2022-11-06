cmake_minimum_required(VERSION 3.16)

set(TEST_SUITE IoCContainer_net4_sharp5.Test.Suite)
project(${TEST_SUITE} LANGUAGES CSharp)
file(GLOB ${TEST_SUITE}_src RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} "*.cs")
add_library(${TEST_SUITE} SHARED ${${TEST_SUITE}_src}
    Properties/AssemblyInfo.cs
)

set_property(TARGET ${TEST_SUITE} PROPERTY ENABLE_EXPORTS ON)
set_property(TARGET ${TEST_SUITE} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.0")
set(CMAKE_CSharp_FLAGS "/langversion:4")
set_property(TARGET ${TEST_SUITE} PROPERTY
    VS_DOTNET_REFERENCE_nunit.framework "${CMAKE_SOURCE_DIR}/3rd_party/testing/bin/net40/nunit.framework.dll")
target_link_libraries(${TEST_SUITE} ${ASM})

set_property(TARGET ${TEST_SUITE} PROPERTY VS_DOTNET_REFERENCES
    "System"
    "System.Core"
    "System.Data"
    "System.Xml"
    "System.Xml.Linq"
)
