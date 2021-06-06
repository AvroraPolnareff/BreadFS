namespace Discord.Net.FSharp.CommandHandling

open Discord.Net.FSharp
open Discord.Net.FSharp.MessageHandling


type CommandHandlerState =
    { Entries: string list }

type CommandHandler = CommandHandlerState -> MessageHandler * CommandHandlerState

module CommandHandlers =
    
    let private (|Eq|_|) x y = if x = y then Some Eq else None
    
    let skip: MessageHandler = fun _msg -> fun _next _ctx -> DiscordHandlers.skipPipeline
    
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


module MessageHandlers =
    
    let command (cmdHandler: CommandHandler) : MessageHandler =
        fun msg ->
            let entries = msg.Content.Split(' ') |> Array.toList
            let state = { Entries = entries }
            let msgHandler, _newState = cmdHandler state
            msgHandler msg
