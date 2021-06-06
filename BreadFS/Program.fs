open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks
open Discord
open Discord.Net.FSharp
open Discord.WebSocket
open Discord.Net.FSharp.Hosting


let inline ( ^ ) f x = f x

let handler =
    messageReceived ^fun msg ->
        if msg.Content = "ping" then
            sendMessageToChannel "pong!" msg.Channel
        else id

let configureServices (ctx: HostBuilderContext) (services: IServiceCollection) : unit =
    let token = ctx.Configuration.["Token"]
    services.ConfigureDiscordConfiguration(token, TokenType.Bot, DiscordSocketConfig()) |> ignore


[<EntryPoint>]
let main argv =
    Host.CreateDefaultBuilder(argv)
        .ConfigureServices(configureServices)
        .ConfigureDiscord(handler)
        .Build()
        .Run()
    0