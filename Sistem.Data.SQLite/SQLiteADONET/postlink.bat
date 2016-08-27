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
copy ".\Resources\%_TGTCPU%\*.*" "%SG_OUTPUT_ROOT%\oak\target\%_TGTCPU%\%WINCEDEBUG%"  /Y
copy ".\Resources\*.*" "%SG_OUTPUT_ROOT%\oak\target\%_TGTCPU%\%WINCEDEBUG%"  /Y
rem copy "C:\WINCE800\3rdParty\CESQLite2013\SQLiteADONET\sqlite-netFx39-binary-WinCE-ARM-2012-1.0.91.0\SQLite.Interop.091.dll"   "%SG_OUTPUT_ROOT%\oak\target\%_TGTCPU%\%WINCEDEBUG%"  /Y
rem copy "C:\WINCE800\3rdParty\CESQLite2013\SQLiteADONET\sqlite-netFx39-binary-WinCE-ARM-2012-1.0.91.0\System.Data.SQLite.dll"   "%SG_OUTPUT_ROOT%\oak\target\%_TGTCPU%\%WINCEDEBUG%"  /Y

) else (
echo Compact/CE Version:%_WINCEOSVER%  Not supported
GOTO SKIP
)

@echo .

@echo copying Content Files from Resource Files folder to FlatRelease Directory
rem copy "%BUILDROOT%\Resources\*.*" %_FLATRELEASEDIR%  /Y
if %_WINCEOSVER%==800 (
REM Small change for Compact 2013
copy ".\Resources\\%_TGTCPU%\*.*" %_FLATRELEASEDIR%  /Y
copy ".\Resources\*.*" %_FLATRELEASEDIR%  /Y
rem copy "C:\WINCE800\3rdParty\CESQLite2013\SQLiteADONET\sqlite-netFx39-binary-WinCE-ARM-2012-1.0.91.0\SQLite.Interop.091.dll"   %_FLATRELEASEDIR%  /Y
rem copy "C:\WINCE800\3rdParty\CESQLite2013\SQLiteADONET\sqlite-netFx39-binary-WinCE-ARM-2012-1.0.91.0\System.Data.SQLite.dll"   %_FLATRELEASEDIR%  /Y

) else (
echo Compact/CE Version:%_WINCEOSVER%  Not supported
GOTO SKIP
)

@echo .
@echo Building .cab file
@echo .

PUSHD
cd %_FLATRELEASEDIR%
IF EXIST SQLiteADONET.inf (
    cabwiz SQLiteADONET.inf
	IF EXIST SQLiteADONET.cab (
		@echo Generated .cab file: SQLiteADONET.cab in FLATRELEASEDIR.
	) else (
		@echo Generation of .cab file: SQLiteADONET.cab failed.
	)
)else (
	@echo No file SQLiteADONET.inf for .cab file generation
)
 
POPD

@echo .
@echo Done Copying
@echo .

:SKIP

