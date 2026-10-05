@echo off
title open.mp 1.5.9 ModSync Server (eLdarqO)
echo ==================================================
echo   open.mp 1.5.9 ModSync Server Package
echo   - ModSync CDN Server (:8080)
echo   - open.mp Game Server (:7777)
echo   Developer: eLdarqO
echo ==================================================

if exist "ModSyncServer.exe" (
    start "ModSync CDN Server" ModSyncServer.exe --port 8080 --mods-dir server_mods
) else if exist "src\ModSyncServer\ModSyncServer.csproj" (
    start "ModSync CDN Server" dotnet run --project src\ModSyncServer\ModSyncServer.csproj -- --port 8080 --mods-dir server_mods
) else (
    echo [ERROR] Neither ModSyncServer.exe nor src\ModSyncServer project found.
    pause
    exit /b 1
)

timeout /t 2 /nobreak >nul

if exist "omp-server.exe" (
    omp-server.exe
) else (
    echo [ERROR] omp-server.exe not found in server root directory.
    pause
    exit /b 1
)
