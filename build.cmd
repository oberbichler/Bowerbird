@echo off
cls

7z e libraries.zip -olibraries\ -p%MY_LIB_PASSWORD% -y

nuget install FAKE -Version 3.26.7 -o packages\ -ExcludeVersion

packages\FAKE\tools\FAKE.exe build.fsx