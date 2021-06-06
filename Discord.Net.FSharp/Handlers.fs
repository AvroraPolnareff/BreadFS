[<AutoOpen>]
module Discord.Net.FSharp.Handlers

open Discord.WebSocket
open FSharp.Control.Tasks

let skipPipeline : DiscordFuncResult = task { return None }
    
let earlyReturn ctx : DiscordFuncResult = task { return Some ctx } 

let messageReceived (f: SocketMessage -> DiscordHandler) : DiscordHandler =
    fun next ctx ->
        match ctx.Event with
        | DiscordEvent.MessageReceived msg -> f msg next ctx
        | _ -> skipPipeline
        
let sendMessageToChannel (msg: string) (channel: ISocketMessageChannel) : DiscordHandler =
    fun next ctx -> task {
        let! _message = channel.SendMessageAsync(msg)
        return! next ctx
    }
