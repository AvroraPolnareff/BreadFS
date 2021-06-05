namespace Discord.Net.FSharp

open System.Threading.Tasks
open Discord
open Discord.WebSocket


type DiscordEvent =
    | MessageReceived of SocketMessage
    
type DiscordContext =
    { Client: BaseSocketClient
      Event: DiscordEvent }

type DiscordFuncResult = Task<DiscordContext option>
type DiscordFunc = DiscordContext -> DiscordFuncResult
type DiscordHandler = DiscordFunc -> DiscordFunc

    