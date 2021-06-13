namespace Discord.Net.FSharp.CommandHandling

open System
open Discord.Net.FSharp
open Discord.Net.FSharp.MessageHandling


type CommandHandlerState =
    { Tokens: string list }

type CommandHandler = CommandHandlerState -> MessageHandler * CommandHandlerState

module MessageHandlers =
    
    type private TokenParsingState =
        | Quoting
        | InToken of isInToken: bool
    
    let private parseTokens (input: string) : string list =
        let rec parse chars state : string list =
            let processSingleToken ch tailChars state =
                let tailTokens = parse tailChars state
                match tailTokens with
                | token :: tailTokens' -> string ch + token :: tailTokens'
                | [] -> [string ch]
            match chars, state with
            | '"' :: tail, InToken _ -> parse tail Quoting
            | '"' :: tail, Quoting -> "" :: parse tail (InToken false)
            | ch :: tailChars, Quoting -> processSingleToken ch tailChars Quoting
            
            | ' ' :: tail, InToken false -> parse tail (InToken false)
            | ' ' :: tail, InToken true -> "" :: parse tail (InToken false)
            | ch :: tailChars, InToken true -> processSingleToken ch tailChars (InToken true)
            | chars, InToken false -> parse chars (InToken true)
            | [], _ -> []
        
        let chars = List.ofSeq input
        parse chars (InToken false)
    
    let command (cmdHandler: CommandHandler) : MessageHandler =
        fun msg ->
            let tokens = parseTokens msg.Content
            let state = { Tokens = tokens }
            let msgHandler, _newState = cmdHandler state
            msgHandler msg

    let commands (cmdHandlers: CommandHandler seq) : MessageHandler =
        command (fun state ->
            let messageHandlers = cmdHandlers |> Seq.map (fun cmdHandler -> cmdHandler state |> fst)
            MessageHandlers.choose messageHandlers, state
            // TODO: Don't discard state for successful command
        )

module CommandHandlers =
    
    let private (|Eq|_|) x y = if x = y then Some Eq else None
    
    let skip: MessageHandler = fun _msg -> fun _next _ctx -> DiscordHandlers.skipPipeline
    
    let command1 (pattern: string) (cont: string -> MessageHandler) : CommandHandler =
        fun state ->
            match state.Tokens with
            | command :: arg :: tail ->
                if command = pattern then
                    let newState = { Tokens = tail }
                    let msgHandler = cont arg
                    msgHandler, newState
                else
                    skip, state
            | _ -> skip, state

    let subCommand (pattern: string) (innerCmdHandler: CommandHandler) : CommandHandler =
        fun state ->
            match state.Tokens with
            | Eq pattern :: tail ->
                let state' = { Tokens = tail }
                innerCmdHandler state'
            | _ -> skip, state

    let subCommands (pattern: string) (innerCmdHandlers: CommandHandler seq) : CommandHandler =
        subCommand pattern (fun state -> MessageHandlers.commands innerCmdHandlers, state)
        
