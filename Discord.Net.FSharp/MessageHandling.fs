namespace Discord.Net.FSharp.MessageHandling

open Discord
open Discord.Net.FSharp
open FSharp.Control.Tasks


type MessageHandler = IMessage -> DiscordHandler

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
    
    let choose (msgHandlers: MessageHandler seq) : MessageHandler =
        fun msg ->
            let handlers = msgHandlers |> Seq.map (fun h -> h msg)
            DiscordHandlers.choose handlers
