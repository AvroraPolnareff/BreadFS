namespace Discord.Net.FSharp.CommandHandling

open System
open Discord.Net.FSharp
open Discord.Net.FSharp.MessageHandling


type CommandHandlerState =
    { Entries: string list }

type CommandHandler = CommandHandlerState -> MessageHandler * CommandHandlerState

module MessageHandlers =
    let private parseEntries (str: string) : string list =
        let rec parseSymbols chars isQuoting : string list =
            match chars, isQuoting with
            | '"' :: tail, false -> "" :: parseSymbols tail true
            | '"' :: tail, true -> parseSymbols tail false
            | ' ' :: tail, false -> "" :: parseSymbols tail false
            | anyChar :: tail, isQuoting ->
                let tailEntries = parseSymbols tail isQuoting
                match tailEntries with
                | entry :: tailEntries' -> entry + string anyChar :: tailEntries'
                | [] -> []
            | [], _ -> []

        let chars = List.ofSeq str
        let entriesRev = parseSymbols chars false
        List.rev entriesRev
        // TODO
        raise (NotImplementedException())
        
    let command (cmdHandler: CommandHandler) : MessageHandler =
        fun msg ->
            let entries = parseEntries msg.Content
            let state = { Entries = entries }
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

    let subCommands (pattern: string) (innerCmdHandlers: CommandHandler seq) : CommandHandler =
        subCommand pattern (fun state -> MessageHandlers.commands innerCmdHandlers, state)
        
