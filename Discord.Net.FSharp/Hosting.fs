namespace Discord.Net.FSharp.Hosting

open System
open Discord
open Discord.Net.FSharp
open Discord.WebSocket
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging


type DiscordHostingServiceOptions =
    { Token: string
      Config: DiscordSocketConfig
      TokenType: TokenType }

type DiscordHostingService(handler: DiscordHandler, options: DiscordHostingServiceOptions, logger: ILogger<DiscordHostingService>) =
    let client = new DiscordSocketClient(options.Config)

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


[<AutoOpen>]
module Extensions =

    type IServiceCollection with
        member this.ConfigureDiscordConfiguration(token, tokenType, config) =
            this.AddTransient<DiscordHostingServiceOptions>(fun sp ->
                { Token = token
                  Config = config
                  TokenType = tokenType }
            )
    
    type IHostBuilder with
        member this.ConfigureDiscord(handler) =
            this.ConfigureServices(fun services ->
                services.AddHostedService<DiscordHostingService>(fun sp ->
                    DiscordHostingService(handler, sp.GetRequiredService<_>(), sp.GetRequiredService<_>())
                ) |> ignore
            )
