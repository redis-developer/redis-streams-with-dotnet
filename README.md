# Redis Streams With .NET Examples

This repo contains three example projects using Redis streams with .NET

* Using the [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) Library - [RedisStreamsWithStackExchange](/RedisStreamsStackExchange)
* Using the [ServiceStack.Redis](https://github.com/ServiceStack/ServiceStack.Redis) Library - [StreamsWithServicestack](/StreamsWithServicestack)
* Using the [CSRedis](https://github.com/2881099/csredis) - [StreamsWithCSRedis](/StreamsWithCSRedis)

These projects are meant to provide basica example of preforming reads and writes to a Redis Stream using the various libraries, as well as things like creating consuemr groups and acknowleding messages from redis.

## Running Redis

There are many ways to run Redis, if you are looking to get into production a good option might be to use the [Redis Cloud](https://app.redislabs.com/#/), however for development it may just be easiest to use docker:

```bash
docker run -p 6379:6379
```

## Running the apps

Each of the apps can be run using the `dotnet run` command, you can either change directories into the directory where the app is and run `dotnet run` or you can use the `--project` option to specify which project you want to run, e.g.

```bash
dotnet run --project 
```