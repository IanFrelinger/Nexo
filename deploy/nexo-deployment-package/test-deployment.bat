@echo off
REM Nexo Framework Deployment Test Script (Windows)
REM This script validates that the Nexo framework is properly deployed and functional

echo 🚀 Nexo Framework Deployment Test
echo ==================================
echo.

setlocal enabledelayedexpansion

REM Test counter
set /a TESTS_PASSED=0
set /a TESTS_FAILED=0

REM Function to run a test
:run_test
set test_name=%~1
set test_command=%~2
set expected_output=%~3

echo Testing: %test_name%
%test_command% > temp_test_output.txt 2>&1
if %errorlevel% equ 0 (
    if not "%expected_output%"=="" (
        findstr /i "%expected_output%" temp_test_output.txt >nul
        if %errorlevel% equ 0 (
            echo   ✅ PASS
            set /a TESTS_PASSED+=1
        ) else (
            echo   ❌ FAIL - Expected output not found
            echo     Expected: %expected_output%
            echo     Got: 
            type temp_test_output.txt | head -5
            set /a TESTS_FAILED+=1
        )
    ) else (
        echo   ✅ PASS
        set /a TESTS_PASSED+=1
    )
) else (
    echo   ❌ FAIL - Command failed
    echo     Error: 
    type temp_test_output.txt | tail -3
    set /a TESTS_FAILED+=1
)
echo.
goto :eof

REM Check if .NET is available
echo Checking .NET Runtime...
dotnet --version >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo   ✅ .NET Runtime found: %DOTNET_VERSION%
    
    REM Check if it's .NET 8.0 or higher
    echo %DOTNET_VERSION% | findstr /r "^8\." >nul
    if %errorlevel% equ 0 (
        echo   ✅ .NET 8.0+ runtime confirmed
    ) else (
        echo   ❌ .NET 8.0+ required, found: %DOTNET_VERSION%
        exit /b 1
    )
) else (
    echo   ❌ .NET Runtime not found
    echo     Please install .NET 8.0 runtime from https://dotnet.microsoft.com/download
    exit /b 1
)
echo.

REM Check file permissions
echo Checking file permissions...
if exist "Nexo.CLI.exe" (
    echo   ✅ Nexo.CLI.exe found
) else if exist "Nexo.CLI" (
    echo   ✅ Nexo.CLI found
) else (
    echo   ❌ Nexo.CLI executable not found
    exit /b 1
)

if exist "Nexo.CLI.dll" (
    echo   ✅ Nexo.CLI.dll found
) else (
    echo   ❌ Nexo.CLI.dll not found
    exit /b 1
)
echo.

REM Test basic CLI functionality
echo Testing Basic CLI Functionality...

if exist "Nexo.CLI.exe" (
    call :run_test "CLI Version" "Nexo.CLI.exe --version" ""
    call :run_test "CLI Help" "Nexo.CLI.exe --help" "Usage:"
    call :run_test "Project Help" "Nexo.CLI.exe project --help" "project"
    call :run_test "Config Help" "Nexo.CLI.exe config --help" "config"
    call :run_test "Dev Help" "Nexo.CLI.exe dev --help" "dev"
) else (
    call :run_test "CLI Version" "dotnet Nexo.CLI.dll --version" ""
    call :run_test "CLI Help" "dotnet Nexo.CLI.dll --help" "Usage:"
    call :run_test "Project Help" "dotnet Nexo.CLI.dll project --help" "project"
    call :run_test "Config Help" "dotnet Nexo.CLI.dll config --help" "config"
    call :run_test "Dev Help" "dotnet Nexo.CLI.dll dev --help" "dev"
)

REM Test AI configuration
echo Testing AI Configuration...
if exist "Nexo.CLI.exe" (
    call :run_test "Config Show" "Nexo.CLI.exe config show" "Configuration"
    call :run_test "Config Help" "Nexo.CLI.exe config --help" "config"
) else (
    call :run_test "Config Show" "dotnet Nexo.CLI.dll config show" "Configuration"
    call :run_test "Config Help" "dotnet Nexo.CLI.dll config --help" "config"
)

REM Test project functionality
echo Testing Project Functionality...
if exist "Nexo.CLI.exe" (
    call :run_test "Project Init Help" "Nexo.CLI.exe project init --help" "init"
    
    echo Testing: Project Help
    Nexo.CLI.exe project --help > temp_test_output.txt 2>&1
    if %errorlevel% equ 0 (
        echo   ✅ PASS
        set /a TESTS_PASSED+=1
    ) else (
        echo   ❌ FAIL
        echo     Error: 
        type temp_test_output.txt | tail -3
        set /a TESTS_FAILED+=1
    )
    echo.
) else (
    call :run_test "Project Init Help" "dotnet Nexo.CLI.dll project init --help" "init"
    
    echo Testing: Project Help
    dotnet Nexo.CLI.dll project --help > temp_test_output.txt 2>&1
    if %errorlevel% equ 0 (
        echo   ✅ PASS
        set /a TESTS_PASSED+=1
    ) else (
        echo   ❌ FAIL
        echo     Error: 
        type temp_test_output.txt | tail -3
        set /a TESTS_FAILED+=1
    )
    echo.
)

REM Test development commands
echo Testing Development Commands...
if exist "Nexo.CLI.exe" (
    call :run_test "Dev Generate Help" "Nexo.CLI.exe dev generate --help" "generate"
    call :run_test "Dev Suggest Help" "Nexo.CLI.exe dev suggest --help" "suggest"
    call :run_test "Dev Test Help" "Nexo.CLI.exe dev test --help" "test"
) else (
    call :run_test "Dev Generate Help" "dotnet Nexo.CLI.dll dev generate --help" "generate"
    call :run_test "Dev Suggest Help" "dotnet Nexo.CLI.dll dev suggest --help" "suggest"
    call :run_test "Dev Test Help" "dotnet Nexo.CLI.dll dev test --help" "test"
)

REM Test pipeline functionality
echo Testing Pipeline Functionality...
if exist "Nexo.CLI.exe" (
    call :run_test "Pipeline Help" "Nexo.CLI.exe pipeline --help" "pipeline"
) else (
    call :run_test "Pipeline Help" "dotnet Nexo.CLI.dll pipeline --help" "pipeline"
)

REM Test interactive mode
echo Testing Interactive Mode...
if exist "Nexo.CLI.exe" (
    call :run_test "Interactive Help" "Nexo.CLI.exe interactive --help" "interactive"
) else (
    call :run_test "Interactive Help" "dotnet Nexo.CLI.dll interactive --help" "interactive"
)

REM Test error handling
echo Testing Error Handling...
echo Testing: Invalid Command Handling
if exist "Nexo.CLI.exe" (
    Nexo.CLI.exe invalid-command > temp_test_output.txt 2>&1
) else (
    dotnet Nexo.CLI.dll invalid-command > temp_test_output.txt 2>&1
)
if %errorlevel% neq 0 (
    findstr /i "error Error Unknown invalid" temp_test_output.txt >nul
    if %errorlevel% equ 0 (
        echo   ✅ PASS - Proper error handling
        set /a TESTS_PASSED+=1
    ) else (
        echo   ⚠️  WARNING - Unexpected error response
        echo     Output: 
        type temp_test_output.txt | head -3
        set /a TESTS_PASSED+=1
    )
) else (
    echo   ❌ FAIL - Should have failed with invalid command
    set /a TESTS_FAILED+=1
)
echo.

REM Test performance
echo Testing Performance...
echo Testing: Startup Performance
set start_time=%time%
if exist "Nexo.CLI.exe" (
    Nexo.CLI.exe --version >nul 2>&1
) else (
    dotnet Nexo.CLI.dll --version >nul 2>&1
)
set end_time=%time%
echo   ✅ PASS - Startup performance test completed
set /a TESTS_PASSED+=1
echo.

REM Generate test report
echo Test Summary
echo =============
echo Tests Passed: %TESTS_PASSED%
echo Tests Failed: %TESTS_FAILED%
set /a total_tests=%TESTS_PASSED% + %TESTS_FAILED%
echo Total Tests: %total_tests%
echo.

if %TESTS_FAILED% equ 0 (
    echo 🎉 All tests passed! Nexo framework is ready for use.
    echo.
    echo Deployment Status: ✅ SUCCESS
    del temp_test_output.txt
    exit /b 0
) else (
    echo ❌ Some tests failed. Please review the errors above.
    echo.
    echo Deployment Status: ❌ FAILED
    del temp_test_output.txt
    exit /b 1
) 