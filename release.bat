@ECHO OFF

SETLOCAL EnableDelayedExpansion

IF "%1" == "" (
	ECHO Please specify the version to release like this:
	ECHO.
	ECHO    .\release.bat 0.0.23
	ECHO.
	GOTO EXIT	
)

ECHO Building and publishing packages
msbuild build.proj /t:release_packages /p:Version=%1
IF !ERRORLEVEL! NEQ 0 (
	ECHO Could not build.
	GOTO FAIL
)

ECHO Creating git tag %1
git tag %1
IF !ERRORLEVEL! NEQ 0 (
	ECHO Could not create tag.
	GOTO FAIL
)

ECHO Pushing tags
git push --tags
IF !ERRORLEVEL! NEQ 0 (
	ECHO Could not push tag.
	GOTO FAIL
)

:EXIT
GOTO SUCCESS

:FAIL
ECHO An error occurred.
EXIT /B 1


:SUCCESS