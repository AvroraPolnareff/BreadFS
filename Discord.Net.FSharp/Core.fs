namespace Discord.Net.FSharp

open System.Threading.Tasks
open Discord
open Discord.WebSocket


[<RequireQualifiedAccess>]
type DiscordEvent =
    | MessageReceived of SocketMessage
    | UserJoined of SocketGuildUser

type DiscordContext =
    { Client: BaseSocketClient
      Event: DiscordEvent }

type DiscordFuncResult = Task<DiscordContext option>
type DiscordFunc = DiscordContext -> DiscordFuncResult
type DiscordHandler = DiscordFunc -> DiscordFunc
