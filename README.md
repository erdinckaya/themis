`Themis` is designed as an basic example of authoritative server which is UDP and RUDP server.
It uses [`Yojimbo.Net`](https://github.com/erdinckaya/yojimbo.net) for networking which is very fast and 
lightweight networking library for dedicated servers.

## DEPENDENCIES
* For testing your server you need an client which is [Prometheus](https://github.com/erdinckaya/prometheus)

## USAGE

Only thing is just write your server ip and port into `Constants.cs` and run the game.


## BASIC LOGIC
Themis is following the `Snapshot Interpolation` paradigm which is 
streaming game state every tick and client adjusts objects respect to these states.
Themis does not use JSON, XML or any other equivalent serialization types. Since 
it is developed with yojimbo which uses bit compression, its bandwidth is really low.
 