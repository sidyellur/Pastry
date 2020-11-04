#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System
open System.Threading

type Message =
    |Initailize of String * int
    |Route of String * String * int
    |Join of String*int
    |UpdateRoutingTable of String[]
    |Print

let system = ActorSystem.Create("DOSProject3")
let mutable actorMap : Map<String, IActorRef> = Map.empty 
let mutable actorHopsMap: Map<String, Double list> = Map.empty
let mutable idleActorSet : Set<String> = Set.empty 
let rand = Random()
let clone i (arr:'T[,]) = arr.[i..i, *]|> Seq.cast<'T> |> Seq.toArray

let Peer (mailBox:Actor<_>) = 
    let mutable id =""
    let mutable rows = 0
    let mutable cols = 16
    let mutable prefix =""
    let mutable suffix =""
    let mutable routingTable: string[,] = Array2D.zeroCreate 0 0
    let mutable commonPrefixLength=0
    let mutable currentRow=0
    let mutable leafSet : Set<String> = Set.empty
   
    
    

    let rec loop() = actor {
            
        let! message = mailBox.Receive()
        match message with
            | Initailize(i,d)->
                id <- i
                rows <- d
                routingTable <- Array2D.zeroCreate rows cols
               
                let mutable itr=0
                let number = Int32.Parse(id, Globalization.NumberStyles.HexNumber)

                let mutable left = number
                let mutable right = number
                
                while itr<8 do 
                    if left = 0 then
                        left <- actorMap.Count-1 //check
                    leafSet <- leafSet.Add(left.ToString())
                    itr <- itr + 1
                    left <- left - 1
                  
                while itr < 16 do
                    if right = actorMap.Count-1 then
                      right <- 0
                    leafSet <- leafSet.Add(right.ToString())
                    itr <- itr + 1
                    right <- right + 1
            
            | Join(key, currentIndex) ->
                let mutable i = 0
                let mutable j = 0
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
                        // let mutable j = 0
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

            | Print ->
                printfn "Routing table of node is %s \n%A" id routingTable    
        return! loop()
        }
    loop()




let args : string array = fsi.CommandLineArgs |> Array.tail
let mutable numNodes =  args.[0] |> int
let numRequest = args.[1] |> string |> int
let numFailures = args.[2] |> string |> int
let numDigits = Math.Log(numNodes |> float, 16.0) |> ceil |> int
let multiply text times = String.replicate times text
printfn "Network construction initiated"
let mutable nodeId = ""
let mutable hexNum = ""
let mutable len = 0


nodeId <- multiply "0" numDigits
let mutable actor = spawn system nodeId Peer
actor <! Initailize(nodeId, numDigits)
actorMap<- actorMap.Add(nodeId, actor)

for i in [1.. numNodes-1] do
    if i = numNodes / 4 then
        printfn "The network is 25 percent done"
    elif i = numNodes / 2 then
        printfn "The network is 50 percent done"
    elif i = numNodes*(3/4) then
        printfn "The network is 75 percent done"

    hexNum <- i.ToString("X")
    len <- hexNum.Length
    nodeId <-  multiply "0" (numDigits-len) + hexNum
    actor<- spawn system nodeId Peer
    actor <! Initailize(nodeId, numDigits)
    actorMap<- actorMap.Add(nodeId, actor)
    let temp = multiply "0" numDigits
    let final = actorMap.Item temp
    final<!Join(nodeId, 0)
    Thread.Sleep 5


Thread.Sleep 1000
printfn "Network is now built"

let actorsArray = actorMap |> Map.toSeq |> Seq.map fst |> Seq.toArray
let mutable idlePeer = multiply "0" numDigits
let mutable f = 0
while f< numFailures do
    while idlePeer = multiply "0" numDigits || idleActorSet.Contains(idlePeer) do
        idlePeer <- actorsArray.[rand.Next actorsArray.Length]
    Thread.Sleep(5)
    idleActorSet <- idleActorSet.Add(idlePeer)
    f<- f+1


printfn "Failure peers are %d" idleActorSet.Count
printfn "Processing requests" 

let mutable k = 1
let mutable destinationId = ""
let mutable ctr = 0
while k<=numRequest do
    for sourceId in actorsArray do
        if not(idleActorSet.Contains(sourceId)) then
            ctr <- ctr + 1
            destinationId <- sourceId
            while destinationId = sourceId || idleActorSet.Contains(destinationId) do
                destinationId <-  actorsArray.[rand.Next actorsArray.Length]
                let temp = actorMap.Item sourceId
                temp<!Route(destinationId, sourceId, 0)
        Thread.Sleep 5
    printfn "Each peer performed %i requests" k
    k<- k + 1

Thread.Sleep 1000
printfn "Requests Processed"
let mutable totalHopSize: Double = 0.0
printfn "Computing average hop size"

let averageHop = actorHopsMap |> Map.toSeq |> Seq.map snd |> Seq.toArray

for i in averageHop do
    totalHopSize <- totalHopSize + i.[0]


printfn "Average Hop size %A" (totalHopSize/ (actorHopsMap.Count |> double))

Environment.Exit 0