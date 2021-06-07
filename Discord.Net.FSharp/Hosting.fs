namespace Discord.Net.FSharp.Hosting

open System
open Discord
open Discord.Net.FSharp
open Discord.Rest
open Discord.WebSocket
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging


type DiscordOptions =
    { Token: string
      Config: DiscordSocketConfig
      TokenType: TokenType }

type IDiscordClientProvider =
    abstract Client: DiscordSocketClient

type DiscordClientHostingService(options: DiscordOptions) =
    let client = new DiscordSocketClient(options.Config)
    interface IHostedService with
        member this.StartAsync(cancellationToken) = unitTask {
            do! client.LoginAsync(options.TokenType, options.Token)
            do! client.StartAsync()
        }
        member this.StopAsync(cancellationToken) = unitTask {
            do! client.LogoutAsync()
            do! client.StopAsync()
            do client.Dispose()
        }
    interface IDiscordClientProvider with
        member _.Client = client

type DiscordHandlerService(handler: DiscordHandler, options: DiscordOptions, client: DiscordSocketClient, logger: ILogger<DiscordHandlerService>) =

    let func = handler DiscordHandlers.earlyReturn

    let handleEvent event =
        unitTask {
            let ctx = { Client = client; Event = event }
            let! result = func ctx

            match result with
            | Some ctx -> ()
            | None -> logger.LogError("Discord Handler returned None")
        }
    
    let registerEvents (client: BaseSocketClient) =
        let inline createHandler eventMapping = Func<_, _>(eventMapping >> handleEvent)
        client.add_MessageReceived (createHandler DiscordEvent.MessageReceived)

    interface IHostedService with
        member this.StartAsync(cancellationToken) =
            unitTask {
                registerEvents client
            }
        member this.StopAsync(cancellationToken) =
            unitTask {
                // TODO: unregister all events
                ()
            }

type DiscordLoggingService(client: BaseSocketClient, logger: ILogger<DiscordLoggingService>) =
    member _.OnLog(msg: LogMessage) = unitTask {
        match msg.Severity with
        | LogSeverity.Info -> logger.LogInformation(msg.Message)
        | LogSeverity.Debug -> logger.LogDebug(msg.Message)
        | LogSeverity.Error -> logger.LogError(msg.Exception, msg.Message)
        | LogSeverity.Critical -> logger.LogCritical(msg.Exception, msg.Message)
        | LogSeverity.Verbose -> logger.LogTrace(msg.Message)
        | LogSeverity.Warning -> logger.LogWarning(msg.Message)
        | _ -> invalidOp "LogSeverity"
    }
    interface IHostedService with
        member this.StartAsync(cancellationToken) = unitTask {
            client.add_Log(Func<_, _> this.OnLog)
        }
        member this.StopAsync(cancellationToken) = unitTask {
            client.remove_Log(Func<_, _> this.OnLog)
        }

type IDiscordBuilder =
    abstract UseHandler: handler: DiscordHandler -> IDiscordBuilder
    abstract UseLogging: unit -> IDiscordBuilder

type DefaultDiscordBuilder(services: IServiceCollection) =
    interface IDiscordBuilder with
        member this.UseHandler(handler) =
            services.AddHostedService(fun sp ->
                DiscordHandlerService(handler, sp.GetRequiredService<_>(), sp.GetRequiredService<_>(), sp.GetRequiredService<_>())
            ) |> ignore
            upcast this
        member this.UseLogging() =
            services.AddHostedService<DiscordLoggingService>() |> ignore
            upcast this

[<AutoOpen>]
module Extensions =

    type IServiceCollection with
        member this.AddDiscordConfiguration(token, tokenType, config): IServiceCollection =
            this.AddTransient<DiscordOptions>(fun sp ->
                { Token = token
                  Config = config
                  TokenType = tokenType }
            ) |> ignore
            this.AddSingleton<DiscordClientHostingService>() |> ignore
            this.AddHostedService<DiscordClientHostingService>(fun sp -> sp.GetRequiredService<_>()) |> ignore
            this.AddTransient<IDiscordClientProvider>(fun sp -> upcast sp.GetRequiredService<DiscordClientHostingService>()) |> ignore
            this.AddTransient<DiscordSocketClient>(fun sp -> sp.GetRequiredService<IDiscordClientProvider>().Client) |> ignore
            this.AddTransient<BaseSocketClient, DiscordSocketClient>(fun sp -> sp.GetRequiredService()) |> ignore
            this
    
    type IHostBuilder with
        member this.ConfigureDiscord(configureDiscord: IDiscordBuilder -> unit) =
            this.ConfigureServices(fun services ->
                let defaultBuilder = DefaultDiscordBuilder(services) :> IDiscordBuilder
                configureDiscord defaultBuilder
            )
