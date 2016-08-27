REM (C)2010-13 David Jones
REM Thanks to Mike Hall

REM CEContentWiz
@echo off
@echo Executing CEContentWiz Postlink.bat
@echo http://CEContentWiz.codeplex.com
@echo .

if %_WINCEOSVER%WXYZ == WXYZ GOTO SKIP

@echo copying Content Files from Resource Files folder to Targeted Debug Directory
if %_WINCEOSVER%==800 (
REM Small change for Compact 2013
copy ".\Resources\%_TGTCPU%\*.dll" "%SG_OUTPUT_ROOT%\oak\target\%_TGTCPU%\%WINCEDEBUG%"  /Y
copy ".\Resources\%_TGTCPU%\*.exe" "%SG_OUTPUT_ROOT%\oak\target\%_TGTCPU%\%WINCEDEBUG%"  /Y
) else (
echo Compact/CE Version:%_WINCEOSVER%  Not supported
GOTO SKIP
)

@echo .

@echo copying Content Files from Resource Files folder to FlatRelease Directory
rem copy "%BUILDROOT%\Resources\*.*" %_FLATRELEASEDIR%  /Y
if %_WINCEOSVER%==800 (
REM Small change for Compact 2013
copy ".\Resources\%_TGTCPU%\*.dll" %_FLATRELEASEDIR%  /Y
copy ".\Resources\%_TGTCPU%\*.exe" %_FLATRELEASEDIR%  /Y
copy ".\Resources\*.inf" %_FLATRELEASEDIR%  /Y
) else (
echo Compact/CE Version:%_WINCEOSVER%  Not supported
GOTO SKIP
)

@echo .
@echo Building .cab file
@echo .

PUSHD
cd %_FLATRELEASEDIR%
IF EXIST SQLite2013.inf (
    cabwiz SQLite2013.inf
	IF EXIST SQLite2013.cab (
		@echo Generated .cab file: SQLite2013.cab in FLATRELEASEDIR.
	) else (
		@echo Generation of .cab file: SQLite2013.cab failed.
	)
)else (
	@echo No file SQLite2013.inf for .cab file generation
)
 
POPD

@echo .
@echo Done Copying
@echo .

:SKIP

