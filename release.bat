@ECHO OFF

IF "%1" == "" (
	ECHO Please specify the version to release like this:
	ECHO.
	ECHO    .\release.bat 0.0.23
	ECHO.
	GOTO EXIT	
)

ECHO Creating git tag %1
git tag %1

ECHO Building & publishing packages
msbuild build.proj /t:release_packages /p:Version=%1

ECHO Pushing tags
git push --tags

:EXIT