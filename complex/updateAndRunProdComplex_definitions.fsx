#r "nuget: Fli, Version=1.111.10"
#r "nuget: MedallionShell, Version=1.6.2"


open System
open System.Collections.Generic
open System.IO
open Fli
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Medallion.Shell

let cwd = Directory.GetCurrentDirectory()

let ValidateCWD () =
    if not (File.Exists(cwd + "/updateAndRunProdComplex.fsx")) then
        Console.WriteLine($"Wrong CWD: {cwd}")
        raise (InvalidOperationException($"Wrong CWD: {cwd}"))
    else
        Console.WriteLine($"CWD validated: {cwd}")

type ContainerStopRemove =
    { Nigginx: int
      WebApi: int
      Postgres: int
      Minio: int }

type ImageRemove = { Nigginx: int; WebApi: int }

type VolumeRemove =
    { WebApi: int
      Minio: int
      Postgres: int }

type State =
    { ContainerStopRemove: ContainerStopRemove
      ImageRemove: ImageRemove
      VolumeRemove: VolumeRemove }

let CreateZeroState () =
    { ContainerStopRemove =
        { Nigginx = 0
          WebApi = 0
          Postgres = 0
          Minio = 0 }
      ImageRemove = { Nigginx = 0; WebApi = 0 }
      VolumeRemove = { WebApi = 0; Minio = 0; Postgres = 0 } }

let GetCurrentState () =
    let currentStateFilePath = "VdsStates/State_Current.json"

    if File.Exists(currentStateFilePath) then
        JsonSerializer.Deserialize<State>(File.ReadAllText(currentStateFilePath))
    else
        CreateZeroState()

type Tasks =
    { ContainerStopRemoveNigginx: bool
      ContainerStopRemoveWebApi: bool
      ContainerStopRemovePostgres: bool
      ContainerStopRemoveMinio: bool

      ImageRemoveNigginx: bool
      ImageRemoveWebApi: bool

      VolumeRemoveWebApi: bool
      VolumeRemoveMinio: bool
      VolumeRemovePostgres: bool }

let CalculateTasks (currentState: State, newState: State) =
    { ContainerStopRemoveNigginx = newState.ContainerStopRemove.Nigginx > currentState.ContainerStopRemove.Nigginx
      ContainerStopRemoveWebApi = newState.ContainerStopRemove.WebApi > currentState.ContainerStopRemove.WebApi
      ContainerStopRemovePostgres = newState.ContainerStopRemove.Postgres > currentState.ContainerStopRemove.Postgres
      ContainerStopRemoveMinio = newState.ContainerStopRemove.Minio > currentState.ContainerStopRemove.Minio

      ImageRemoveNigginx = newState.ImageRemove.Nigginx > currentState.ImageRemove.Nigginx
      ImageRemoveWebApi = newState.ImageRemove.WebApi > currentState.ImageRemove.WebApi

      VolumeRemoveWebApi = newState.VolumeRemove.WebApi > currentState.VolumeRemove.WebApi
      VolumeRemoveMinio = newState.VolumeRemove.Minio > currentState.VolumeRemove.Minio
      VolumeRemovePostgres = newState.VolumeRemove.Postgres > currentState.VolumeRemove.Postgres }

let GetNewState () =
    let newStateFilePath = "VdsStates/State_New.json"
    JsonSerializer.Deserialize<State>(File.ReadAllText(newStateFilePath))

let executeCommand1 (command: string, throwIfErrored:bool) =
    Console.WriteLine($"Executing command: {command}")

    let log (output: string) = Console.WriteLine($"CLI log: {output}")

    let preOutput =
        ( cli {
            
            Shell Shells.SH
            Command command
            WorkingDirectory cwd
            Output log
            CancelAfter (1000*10)
         }
         |> Command.executeAsync |> Async.RunSynchronously
        )
    
    let output =
        if throwIfErrored then
            preOutput |> Output.throwIfErrored
        else
            preOutput
    
    Console.WriteLine($"Command executing finished with code {output.ExitCode}: {command}")

let executeCommand (command: string, throwIfErrored: bool) =

    let cancelTokenSource = new CancellationTokenSource()
    let timeOut: int = 1000 * 60 * 10
    cancelTokenSource.CancelAfter(timeOut)
    let token = cancelTokenSource.Token

    Console.WriteLine($"Executing command: {command}")

    use command =
        Command
            .Run(
                "sh",
                [ "-c"; command ] |> Seq.cast<obj>,
                Action<Shell.Options>(fun options ->
                    options.DisposeOnExit(false).Timeout(TimeSpan.FromMilliseconds timeOut).WorkingDirectory(cwd)
                    |> ignore)
            )
            .RedirectTo(Console.Out).RedirectStandardErrorTo(Console.Error)
            
    Console.WriteLine("Before command.Wait()")
    command.Wait()
    
    Console.WriteLine($"Command executing finished with code {command.Process.ExitCode}")

let executeShScriptAsync (scriptPath: string) =
    executeCommand ($"chmod ugoa=rwx {scriptPath}.sh", true)
    executeCommand ($"./{scriptPath}.sh", true)

let executeVdsShScript (scriptFileName: string) =
    executeShScriptAsync $"VdsShScripts/{scriptFileName}"

let executeVdsShScriptSynchronously (scriptFileName: string) =
    executeShScriptAsync $"VdsShScripts/{scriptFileName}"

let executeShScriptAsyncNoThrow (scriptPath: string) =
    executeCommand ($"chmod ugoa=rwx {scriptPath}.sh", true)
    executeCommand ($"./{scriptPath}.sh", false)

let executeVdsShScriptNoThrow (scriptFileName: string) =
    executeShScriptAsyncNoThrow $"VdsShScripts/{scriptFileName}"

let executeVdsShScriptSynchronouslyNoThrow (scriptFileName: string) =
    executeShScriptAsyncNoThrow $"VdsShScripts/{scriptFileName}"
