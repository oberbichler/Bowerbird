#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile

let buildDir = "./build/"
let examplesDir = "./examples/"

let version = AppVeyor.AppVeyorEnvironment.BuildVersion;

Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "AssemblyInfo" (fun _ ->
    CreateCSharpAssemblyInfo "./source/Bowerbird/Properties/AssemblyInfo.Version.cs"
        [Attribute.Version version
         Attribute.FileVersion version]
)

Target "Build" (fun _ ->
    !! "source/Bowerbird.sln"
        |> MSBuildRelease buildDir "Build"
        |> Log "AppBuild-Output: "
)

Target "ZipExamples" (fun _ ->
    !! (examplesDir + "*.*")
        |> Zip examplesDir (buildDir + "Examples.zip")
)

Target "Default" (fun _ ->
    trace "Complete!"
)

"Clean"
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "ZipExamples"
    ==> "Default"

RunTargetOrDefault "Default"