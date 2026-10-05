@echo off
title open.mp ModSync Indexer (eLdarqO)
echo ==================================================
echo   open.mp ModSync - Re-indexing Server Mods
echo   Developer: eLdarqO
echo ==================================================

if exist "ModSyncServer.exe" (
    ModSyncServer.exe --port 8080 --mods-dir server_mods --reindex
) else if exist "src\ModSyncServer\ModSyncServer.csproj" (
    dotnet run --project src\ModSyncServer\ModSyncServer.csproj -- --port 8080 --mods-dir server_mods --reindex
) else (
    echo [ERROR] Neither ModSyncServer.exe nor src\ModSyncServer project found.
)

pause
