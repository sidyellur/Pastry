
open System


module Messages = 
    type Message =
    |BuildNetwork of String * int
    |Route of String * String * int
    |Join of String*int
    |UpdateRoutingTable of String[]