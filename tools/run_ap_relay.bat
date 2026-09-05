@echo off
setlocal

echo Bomber Crew Archipelago TLS relay
echo ==================================
echo Needed if you're connecting to a room hosted on archipelago.gg (or any
echo TLS/wss:// server) - Bomber Crew's old Mono runtime can't do that handshake
echo itself, so this relay does it on your behalf.
echo.

set /p REMOTE="Room address shown by the site (e.g. archipelago.gg:12345): "
set /p LOCALPORT="Local port to use (press Enter for default 39000): "
if "%LOCALPORT%"=="" set LOCALPORT=39000

echo.
echo Starting relay... once it says "Listening on ws://localhost:%LOCALPORT%",
echo connect the mod's Host field to: localhost:%LOCALPORT%
echo Leave this window open while you play - closing it disconnects you.
echo.

python "%~dp0ap_ws_relay.py" --remote %REMOTE% --local-port %LOCALPORT%
pause
