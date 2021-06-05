// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Threading.Tasks
open Discord
open Discord.Net.FSharp
open Discord.WebSocket
open FSharp.Control.Tasks
open Discord.Net.FSharp.Hosting
open Microsoft.Extensions.Hosting
    
let inline ( ^ ) f x = f x
let token = "TODO"
let handler =
    messageReceived ^fun msg ->
        if msg.Content = "ping" then
            sendMessageToChannel "pong!" msg.Channel
        else id
    
    
    
[<EntryPoint>]
let main argv =
    Host.CreateDefaultBuilder(argv) 
        .UseDiscord(handler, token, TokenType.Bot, fun config -> ())
        .Build()
        .Run()
    0