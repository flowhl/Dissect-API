@echo off
setlocal

:: Check if version argument is provided
if "%~1"=="" (
    echo No version specified. Use: deploy.bat -v version_number
    goto :EOF
)

:: Extract version number from argument
set "version=%~2"

:: Docker build
echo Building Docker image...
docker build -t flowhl/dissect-api:%version% .

:: Docker tag
echo Tagging Docker image...
docker tag flowhl/dissect-api:%version% ghcr.io/flowhl/dissect-api:%version%

:: Docker push
echo Pushing Docker image...
docker push ghcr.io/flowhl/dissect-api:%version%

echo Deployment completed.
