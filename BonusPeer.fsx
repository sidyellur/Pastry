#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#load "MessageTypes.fsx"
#load "AllFunctions.fsx"
#load "InitializeDS.fsx"

module BPeer =
    open Akka.FSharp
    open System
    open MessageTypes.Messages
    open AllFunctions.Functions
    open InitializeDS.DataStructure
    let Peer (mailBox:Actor<_>) = 
        let mutable id = String.Empty
        let mutable rows = 0 
        let mutable cols = 16
        let mutable routingTable: string[,] = Array2D.zeroCreate 0 0
        let mutable commonPrefixLength=0
        let mutable currentRow=0
        let mutable leafSet : Set<String> = Set.empty
       
        let rec loop() = actor {
            let! message = mailBox.Receive()
            match message with
                | BuildNetwork(i,d)->
                    id <- i
                    rows <- d
                    routingTable <- Array2D.zeroCreate rows cols
                   
                    let mutable itr=0
                    let number = Int32.Parse(id, Globalization.NumberStyles.HexNumber)

                    let mutable neighbor1 = number
                    let mutable neighbor2 = number
                    
                    while itr<8 do 
                        if neighbor1 = 0 then
                            neighbor1 <- actorMap.Count-1 //check
                        leafSet <- leafSet.Add(neighbor1.ToString())
                        itr <- itr + 1
                        neighbor1 <- neighbor1 - 1
                      
                    while itr < 16 do
                        if neighbor2 = actorMap.Count-1 then
                          neighbor2 <- 0
                        leafSet <- leafSet.Add(neighbor2.ToString())
                        itr <- itr + 1
                        neighbor2 <- neighbor2 + 1
                
                | Join(key, currentIndex) ->
                    let mutable i = 0
                    // let mutable j = 0
                    let mutable k = currentIndex

                    while key.[i] = id.[i] do
                        i<- i+1
                    commonPrefixLength <- i
                    let mutable routingRow: string[] = Array.zeroCreate 0

                    while k<=commonPrefixLength do
                        routingRow <- clone k routingTable
                        routingRow.[Int32.Parse(id.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)] <- id
                        let foundKey = actorMap.TryFind key
                        match foundKey with
                        | Some x->
                            x<! UpdateRoutingTable(routingRow)
                            ()
                        | None -> printfn "Key does not exist in the map!"

                        k<- k+1

                    let rtrow = commonPrefixLength
                    let rtcol = Int32.Parse(key.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)
                    if isNull routingTable.[rtrow, rtcol] then
                        routingTable.[rtrow, rtcol] <- key
                    else
                        let temp = routingTable.[rtrow, rtcol]
                        let final = actorMap.TryFind temp

                        match final with
                        | Some x ->
                            x<!Join(key, k)
                        | None ->printfn "Key does not exist in the map "

                | UpdateRoutingTable(row: String[])->
                    routingTable.[currentRow, *] <- row
                    currentRow <- currentRow + 1

                | Route(key, source, hops) ->
                    if id = key then
                        if actorHopsMap.ContainsKey(source) then
                            let foundKey = actorHopsMap.TryFind source
                            match foundKey with 
                            | Some x->
                                let total = x.[1]
                                let avgHops = x.[0]
                                let value = [((avgHops*total)+(hops |> double))/ (total+1.0); total+1.0]
                                actorHopsMap <- actorHopsMap.Add(source, value)
                            | None -> printfn "Key does not exist in the map"
                        else
                            let value = [hops |> double; 1.0]
                            actorHopsMap <- actorHopsMap.Add(source, value)


                    elif leafSet.Contains(key) then
                        let actor = actorMap.Item(key)
                        actor <! Route(key, source, hops+1)

                    else
                        let mutable i = 0

                        while key.Length<> 0 && key.[i] = id.[i] do
                            i<- i+1
                        commonPrefixLength <- i
                        let check = 0
                        let mutable rtrow = commonPrefixLength
                        let mutable rtcol = Int32.Parse(key.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)
                        if idleActorSet.Contains(routingTable.[rtrow, rtcol]) && ( rtcol <> 0) then
                            rtcol <- rtcol - 1

                        if isNull routingTable.[rtrow, rtcol] then
                            rtcol <- 0

                        if(id = "00" && rtcol = 0) then 
                            rtcol<- rtcol + 1

                        actorMap.Item(routingTable.[rtrow, rtcol]) <! Route(key, source, hops+1)

            return! loop()
            }
        loop()
