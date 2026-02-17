#load "updateAndRunProdComplex_definitions.fsx"

open System
open System.Collections
open System.IO
open System.Text.Json
open UpdateAndRunProdComplex_definitions

Console.WriteLine("updateAndRunProdComplex.fsx started")

ValidateCWD()

let envKeys = Environment.GetEnvironmentVariables().Keys |> Seq.cast|> Seq.map(fun el -> ((string)el))
let str = String.Join(", ", envKeys |> Seq.toArray)
Console.WriteLine("Env: " + str);

let currentState = GetCurrentState()
let newState = GetNewState()

let tasks = CalculateTasks(currentState, newState)

if tasks.ContainerStopRemoveMinio then
    executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveMinio"

if tasks.ContainerStopRemoveNigginx then
    executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveNigginx"

if tasks.ContainerStopRemovePostgres then
    executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemovePostgres"

if tasks.ContainerStopRemoveWebApi then
    executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveWebApi"

if tasks.ImageRemoveNigginx then
    if not tasks.ContainerStopRemoveNigginx then
        executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveNigginx"

    executeVdsShScriptSynchronouslyNoThrow "ImageRemoveNigginx"

if tasks.ImageRemoveWebApi then
    if not tasks.ContainerStopRemoveWebApi then
        executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveWebApi"

    executeVdsShScriptSynchronouslyNoThrow "ImageRemoveWebApi"

if tasks.VolumeRemoveMinio then
    if not tasks.ContainerStopRemoveMinio then
        executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveMinio"

    executeVdsShScriptSynchronouslyNoThrow "VolumeRemoveMinio"

if tasks.VolumeRemovePostgres then
    if not tasks.ContainerStopRemovePostgres then
        executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemovePostgres"

    executeVdsShScriptSynchronouslyNoThrow "VolumeRemovePostgres"

if tasks.VolumeRemoveWebApi then
    if not tasks.ContainerStopRemoveMinio then
        executeVdsShScriptSynchronouslyNoThrow "ContainerStopRemoveMinio"

    executeVdsShScriptSynchronouslyNoThrow "VolumeRemoveWebApi"

executeVdsShScriptSynchronously "DockerComposeProdRun"

File.WriteAllText("VdsStates/State_Current.json", JsonSerializer.Serialize(newState))

Console.WriteLine("updateAndRunProdComplex.fsx finished")
