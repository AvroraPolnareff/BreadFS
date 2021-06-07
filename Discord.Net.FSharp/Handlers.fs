[<AutoOpen>]
module Discord.Net.FSharp.Handlers

open System.Text.RegularExpressions
open Discord
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

// !ping
// !music
// !music play http://... --next
// !music p http://...
// !music play "http://.../1" http://.../2
// !say 532859235 "I Love You!"
// HAHA MAGGOTS!!!
// HOW DARE YOU!!!
// Adeventure Time!


(*

let routes =
    MessageHandlers.choose [
        // command "ping" >=> reply "pong"
        
        // subCommand "music" [
        //     commandR1 "play|p" playMusic // (fun arg -> (playMusic arg : DiscordHandler))
        //     musicHandler
        // ]
        command ^ command1 "say" sayHandler
    ]

*)


type MessageHandler = IMessage -> DiscordHandler


type CommandHandlerState =
    { Entries: string list }

type CommandHandler = CommandHandlerState -> MessageHandler * CommandHandlerState


module MessageHandlers =
    
    let compose (mHandler1: MessageHandler) (mHandler2: MessageHandler) : MessageHandler =
        fun msg ->
            (mHandler1 msg) >> (mHandler2 msg)

    let reply (text: string) : MessageHandler =
        fun msg ->
            fun next ctx -> task {
                let! _message = msg.Channel.SendMessageAsync(text)
                return! next ctx
            }
    

    let command (cmdHandler: CommandHandler) : MessageHandler =
        fun msg ->
            let entries = msg.Content.Split(' ') |> Array.toList
            let state = { Entries = entries }
            let msgHandler, _newState = cmdHandler state
            msgHandler msg

    let choose (msgHandlers: MessageHandler seq) : MessageHandler =
        fun msg ->
            let handlers = msgHandlers |> Seq.map (fun h -> h msg)
            DiscordHandlers.choose handlers


let (|Eq|_|) x y = if x = y then Some Eq else None


module CommandHandlers =
    
    let skip: MessageHandler = fun _msg -> fun _next _ctx -> DiscordHandlers.skipPipeline
    
    // a b c
    let command1 (pattern: string) (cont: string -> MessageHandler) : CommandHandler =
        fun state ->
            match state.Entries with
            | command :: arg :: tail ->
                if command = pattern then
                    let newState = { Entries = tail }
                    let msgHandler = cont arg
                    msgHandler, newState
                else
                    skip, state
            | _ -> skip, state

    let subCommand (pattern: string) (innerCmdHandler: CommandHandler) : CommandHandler =
        fun state ->
            match state.Entries with
            | Eq pattern :: tail ->
                let state' = { Entries = tail }
                innerCmdHandler state'
            | _ -> skip, state
