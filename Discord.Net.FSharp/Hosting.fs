namespace Discord.Net.FSharp.Hosting

open System
open Discord
open Discord.Net.FSharp
open Discord.WebSocket
open Discord.WebSocket
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options



type DiscordHostingServiceOptions =
    { Handler: DiscordHandler
      Token: string
      Config: DiscordSocketConfig
      TokenType: TokenType }

type DiscordHostingService(options: DiscordHostingServiceOptions, logger: ILogger<DiscordHostingService>) =
    let client =
        new DiscordSocketClient(options.Config)

    let handler = options.Handler
    let func = handler earlyReturn

    let handleEvent event =
        unitTask {
            let ctx = { Client = client; Event = event }
            let! result = func ctx

            match result with
            | Some ctx -> ()
            | None -> logger.LogError("Discord Handler returned None")
        }

    interface IHostedService with
        member this.StartAsync(cancellationToken) =
            unitTask {
                let inline createHandler eventMapping = Func<_, _>(eventMapping >> handleEvent)
                client.add_MessageReceived (createHandler DiscordEvent.MessageReceived)

                do! client.LoginAsync(options.TokenType, options.Token)
                do! client.StartAsync()
            }

        member this.StopAsync(cancellationToken) =
            unitTask {
                do! client.LogoutAsync()
                do! client.StopAsync()
                do client.Dispose()
                // TODO: unregister all events
            }


type IDiscordBuilder =
    abstract UseHandler: handler: DiscordHttpHandler -> IDiscordBuilder
    abstract Build: unit -> DiscordFunc

type DefaultDiscordBuilder() =
    let mutable handler = id
    interface IDiscordBuilder with
        member this.UseHandler(handler') =
            handler <- handler >> handler'
            this
        member this.Build() =
            handler earlyReturn


[<AutoOpen>]
module HostBuilderExtensions =

    type IHostBuilder with
//        member this.UseDiscord(handler, token, tokenType, configureConfig) =
//            this.ConfigureServices(fun services ->
//                services.AddTransient<DiscordHostingServiceOptions>(fun sp ->
//                    let config = DiscordSocketConfig()
//                    configureConfig config
//                    { Handler = handler
//                      Token = token
//                      Config = config
//                      TokenType = tokenType }
//                ) |> ignore
//                services.AddHostedService<DiscordHostingService>()
//                |> ignore
//            )
        member this.ConfigureDiscord(configureDiscord: IDiscordBuilder -> unit) =
            let discorBuilder = DefaultDiscordBuilder()
            configureDiscord discordBuilder
            let func = discordBuilder.Build()

            ()

