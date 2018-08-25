@echo off
cls

nuget install FAKE -Version 3.26.7 -o packages\ -ExcludeVersion

nuget restore C:\projects\bowerbird\source\

packages\FAKE\tools\FAKE.exe build.fsx