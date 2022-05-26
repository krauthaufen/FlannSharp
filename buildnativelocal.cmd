@echo off

git clone https://github.com/Microsoft/vcpkg.git ./.vcpkg/vcpkg --depth 1
copy .vcpkg\vcpkg\triplets\community\x64-windows-static-md.cmake .vcpkg\vcpkg\triplets\community\x64-windows-static-md-rel.cmake
echo set(VCPKG_BUILD_TYPE release) >> .vcpkg\vcpkg\triplets\community\x64-windows-static-md-rel.cmake

cmd /C ".vcpkg\vcpkg\bootstrap-vcpkg.bat -disableMetrics"

SET VCPKG_NUGET_REPOSITORY=https://github.com/krauthaufen/FlannSharp
.vcpkg\vcpkg\vcpkg.exe install flann --triplet x64-windows-static-md-rel

cmake -S src\FlannNative -B src\FlannNative\build -DCMAKE_TOOLCHAIN_FILE="%~dp0\.vcpkg\vcpkg\scripts\buildsystems\vcpkg.cmake" -DVCPKG_TARGET_TRIPLET=x64-windows-static-md-rel -DCMAKE_BUILD_TYPE=Release

cmake --build src\FlannNative\build --config Release --target install