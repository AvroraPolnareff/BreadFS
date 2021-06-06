namespace Discord.Net.FSharp

open Discord.WebSocket
open FSharp.Control.Tasks


module DiscordHandlers =
    
    let skipPipeline : DiscordFuncResult = task { return None }
        
    let earlyReturn ctx : DiscordFuncResult = task { return Some ctx } 

    let messageReceived (onMessageRecieved: SocketMessage -> DiscordHandler) : DiscordHandler =
        fun next ctx ->
            match ctx.Event with
            | DiscordEvent.MessageReceived msg -> onMessageRecieved msg next ctx
            | _ -> skipPipeline

    let sendMessageToChannel (msg: string) (channel: ISocketMessageChannel) : DiscordHandler =
        fun next ctx -> task {
            let! _message = channel.SendMessageAsync(msg)
            return! next ctx
        }

    let choose (handlers: DiscordHandler seq) : DiscordHandler =
        let rec handle (handlers: DiscordHandler list) = fun next ctx -> task {
            match handlers with
            | [] -> return! skipPipeline
            | handler :: tail ->
                let! result = handler next ctx
                match result with
                | None -> return! handle tail next ctx
                | Some ctx -> return! earlyReturn ctx
        }
        handle (Seq.toList handlers)
