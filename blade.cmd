@echo OFF
set BladeArgs=%1 %2 %3 %4 %5 %6 %7 %8 %9
for /L %%i in (0,1,8) do @shift
set BladeArgs=%BladeArgs% %1 %2 %3 %4 %5 %6 %7 %8 %9
for /L %%i in (0,1,8) do @shift
set BladeArgs=%BladeArgs% %1 %2 %3 %4 %5 %6 %7 %8 %9
BladeEngine.CLI.exe %BladeArgs%