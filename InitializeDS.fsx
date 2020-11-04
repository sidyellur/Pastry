#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
module DataStructure =
    open System
    open Akka.Actor
    let mutable actorMap : Map<String, IActorRef> = Map.empty 
    let mutable actorHopsMap: Map<String, Double list> = Map.empty
    let mutable idleActorSet : Set<String> = Set.empty