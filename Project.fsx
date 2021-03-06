#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#load "MessageTypes.fsx"
#load "AllFunctions.fsx"
#load "InitializeDS.fsx"
#load "Peer.fsx"
open Akka.Actor
open Akka.FSharp
open System
open System.Threading
open MessageTypes.Messages
open AllFunctions.Functions
open InitializeDS.DataStructure
open Peer.Peer

let system = ActorSystem.Create("DOSProject3")
let args : string array = fsi.CommandLineArgs |> Array.tail
let mutable numNodes =  args.[0] |> int
let numRequest = args.[1] |> string |> int
let numDigits = Math.Log(numNodes |> float, 16.0) |> ceil |> int
printfn "Network construction initiated"
let mutable nodeId = String.Empty
let mutable hexNum = String.Empty
let mutable len = 0
nodeId <- multiply "0" numDigits
let mutable actor = spawn system nodeId Peer
actor <! BuildNetwork(nodeId, numDigits)
actorMap<- actorMap.Add(nodeId, actor)
for i in [1.. numNodes-1] do
    hexNum <- i.ToString("X")
    len <- hexNum.Length
    nodeId <-  multiply "0" (numDigits-len) + hexNum
    actor<- spawn system nodeId Peer
    actor <! BuildNetwork(nodeId, numDigits)
    actorMap<- actorMap.Add(nodeId, actor)
    let temp = multiply "0" numDigits
    let final = actorMap.Item temp
    final<!Join(nodeId, 0)
    Thread.Sleep 5

Thread.Sleep 1000
printfn "Network is now built"
let actorsArray = actorMap |> Map.toSeq |> Seq.map fst |> Seq.toArray
printfn "Processing requests" 
let mutable k = 1
let mutable destinationId = ""
let mutable ctr = 0
while k<=numRequest do
    for sourceId in actorsArray do
        ctr <- ctr + 1
        destinationId <- sourceId
        while destinationId = sourceId do
            destinationId <-  actorsArray.[rand.Next actorsArray.Length]
        let temp = actorMap.Item sourceId
        temp<!Route(destinationId, sourceId, 0)
        Thread.Sleep 5
    printfn "Each peer performed %i requests" k
    k<- k + 1

Thread.Sleep 1000
printfn "Requests Processed"
let mutable totalHopSize = 0.0 |> double
printfn "Computing average hop size"
let averageHop = actorHopsMap |> Map.toSeq |> Seq.map snd |> Seq.toArray 
for i in averageHop do
    totalHopSize <- totalHopSize + i.[0]
let ans = totalHopSize / double(actorHopsMap.Count) 
printfn "Avg Hop Size %f" ans
Environment.Exit 0
