@echo off
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process cmd -ArgumentList '/c \"%~f0\"' -Verb RunAs"
    exit /b
)

set "APP=%~dp0Quick Share.exe"
if not exist "%APP%" (
    echo [ERROR] Quick Share.exe not found in: %~dp0
    pause
    exit /b 1
)

netsh advfirewall firewall delete rule name="quick share.exe"

echo Adding firewall rules for: %APP%
netsh advfirewall firewall add rule name="quick share.exe" dir=in action=allow program="%APP%" enable=yes profile=any
netsh advfirewall firewall add rule name="quick share.exe" dir=out action=allow program="%APP%" enable=yes profile=any

if %errorlevel% equ 0 (
    echo [OK] Firewall rules added successfully!
) else (
    echo [ERROR] Failed to add rules
)
pause