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
        let rec handle handlers = task {
            match handlers with
            | [] -> skipPipeline
            | handler :: tail ->
                let! result = handler next ctx
                match result with
                | None -> return! handle tail next ctx
                | Some ctx -> return! earlyReturn ctx
        }
        handle handlers

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
            fun next ctx -> unitTask {
                let! _message = message.Channel.SendMessageAsync(text)
                return! next ctx
            }
    

    let command (cmdHandler: CommandHandler) : MessageHandler =
        fun msg ->
            let entries = msg.Content.Split(' ')
            let state = { Entries = entries }
            let msgHandler, _newState = cmdHandler state
            msgHandler msg

    // let commandR (pattern: string) : MessageHandler =
    //     fun msg ->
    //         if Regex.IsMatch(msg.Content, pattern) then
    //             fun next ctx -> next ctx
    //         else
    //             fun _ -> skipPipeline
    

    
    let choose (msgHandlers: MessageHandler seq) : MessageHandler =
        fun msg ->
            let handlers = msgHandlers |> Seq.map (fun h -> h msg)
            DiscordHandlers.choose handler


let (|Eq|_|) x y = if x = y then Some Eq else None


module CommandHandlers =
    
    let skip = fun _msg -> DiscordHandlers.skipPipeline
    
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
            | (Eq pattern) :: tail -> innerCmdHandler tail
            | _ -> skip, state


// module Pg =

//     let (|Foo|) x = x + 1
//     let (Foo a) = 5
//     let a' = (|Foo|) 5
    
//     let (|Odd|Even|) x = if x % 2 = 0 then Even else Odd
//     match 4 with
//     | Odd -> ()
//     | Even -> ()

//     let (|Quoted|) (s: string) = $"\"{s}\""
//     let foo (Quoted s) = printfn %"{s}"
//     foo "123"
    
//     let (|Split|_|) (s: string) =
//         let r = s.Split(" ", 2)
//         if r.Length = 2
//         then Some (r.[0], r.[1])
//         else None
    
//     match "asdf zxcv" with
//     | Split (adf, zxcv) -> ()
//     | _ -> ()


//     let (|Cons|Nil|) (ls: 'a list) =
//         match ls with
//         | h::t -> Cons (h, t)
//         | [] -> Nil
    
//     match [ 1; 2; 3 ] with
//     | Cons (Eq 1, t) ->
//         printf $"{h}"
//     | Nil -> ()
    
//     let (|Apply|) f x = f x
    
//     let inc x = x + 1
//     let (Apply inc x) = 2
    
//     let x = Choice<int, string, float> = failwith ""
//     match x with
//     | Choice1Of3 i -> ()
//     | Choice2Of3 s -> ()
//     | Choice3Of3 f -> ()

    
//     type UserId = UserId of Guid
//         with static member Unwrap(UserId x) = x

//     let nGuid = Guid.NewGuid()
//     let uid = UserId nGuid
//     let (UserId gid) = uid
//     let gid2 = uid |> fun (UserId x) -> x
//     let gid3 = UserId.Unwrap uid
    
//     let deleteUser : Guid -> unit =
//         fun (Apply UserId uid) ->
//             ignore uid
        // fun gid ->
        //     let uid = UserId gid
        //     ignore uid
