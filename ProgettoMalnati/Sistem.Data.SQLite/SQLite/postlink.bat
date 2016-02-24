If EXIST ..\SQLite2013\Resources\%_TGTCPU% (
@ECHO ON
copy .\obj\%_TGTCPU%\%WINCEDEBUG%\SQLite.dll ..\SQLite2013\Resources\%_TGTCPU% /Y
copy .\obj\%_TGTCPU%\%WINCEDEBUG%\SQLite.lib ..\SQLite2013\Resources\%_TGTCPU% /Y
@ECHO OFF
)

