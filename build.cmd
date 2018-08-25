@echo off
cls

nuget install FAKE -Version 3.26.7 -o packages\ -ExcludeVersion

nuget restore -SolutionDirectory .\Source\

packages\FAKE\tools\FAKE.exe build.fsx