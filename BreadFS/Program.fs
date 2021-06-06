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

let messageHandler : MessageHandler =
    MessageHandlers.choose [
        MessageHandlers.command ^ CommandHandlers.command1 "say" MessageHandlers.reply
    ]

let handler =
    DiscordHandlers.messageReceived messageHandler


let configureServices (ctx: HostBuilderContext) (services: IServiceCollection) : unit =
    let token = ctx.Configuration.["Discord:Token"]
    let discordConfig = DiscordSocketConfig(LogLevel = LogSeverity.Debug)
    services.AddDiscordConfiguration(token, TokenType.Bot, discordConfig) |> ignore

let configureDiscord (ds: IDiscordBuilder) : unit =
    ds.UseHandler(handler) |> ignore
    ds.UseLogging() |> ignore

[<EntryPoint>]
let main argv =
    Host.CreateDefaultBuilder(argv)
        .ConfigureServices(configureServices)
        .ConfigureDiscord(configureDiscord)
        .Build()
        .Run()
    0
