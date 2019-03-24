# Learning.EventStore #

Learning.EventStore is a framework for CQRS, Eventsourcing, and messaging based on [CQRSLite](https://github.com/gautema/cqrslite). It uses Redis pub/sub for messaging and offers event persistence in Redis, SQL Server, or PostgreSQL. It is written in C# and targets .NET 4.5.2, .NET 4.6, and .NET Standard. It is developed and maintained by [Learning.com](https://www.learning.com) and is currently being used in production there.

# Features #
* Message publishing with Redis Pub/Sub and an implementation of the [Redis reliable queue pattern](https://redis.io/commands/rpoplpush)
* Eventsourcing with full support for event persistence, replay, and snapshotting
* Event persistence options including Redis, SQL Server, and PostgreSQL out of the box and extensibility points for custom persistence implementations
* In-Memory and Redis caching with concurrency checks and updating to latest version
* Optimistic concurrency checking
* CQRS (Command Query Responsibility Segregation) framework
* Dead letter queue and retry support
* Optional queue locking to maintain message order in multi-host environments

# Installation #

There are three separate libraries included in this repository. All three are available as Nuget packages and can be installed from there.

* [Learning.Cqrs](https://www.nuget.org/packages/Learning.Cqrs/)
    - This package provides CQRS functionality, it is not dependent on the others, nor are the others dependent on it
    - Package Manager - `Install-Package Learning.Cqrs`
    - .NET CLI - `dotnet add package Learning.Cqrs`
* [Learning.MessageQueue](https://www.nuget.org/packages/Learning.MessageQueue/)
    - This is provided as a separate package in case you only want messaging capability without event persistence
    - Package Manager - `Install-Package Learning.MessageQueue`
    - .NET CLI - `dotnet add package Learning.MessageQueue`
* [Learning.EventStore](https://www.nuget.org/packages/Learning.EventStore/)
    - This will also install Learning.MessageQueue as a dependency
    - Package Manager - `Install-Package Learning.EventStore`
    - .NET CLI - `dotnet add package Learning.EventStore`

# Getting Started #

## Configuration ##

### Configure Redis Connection ###
In your startup.cs file add the following in the ConfigureServices method replacing the host and port with your Redis host and port. The example assumes localhost with port 6379:
```
// Configure Redis Connection
var redisConfigOptions = ConfigurationOptions.Parse("127.0.0.1:6379");
redisConfigOptions.AbortOnConnectFail = false;
services.AddSingleton(new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConfigOptions)));
```
\* For production environments it is recommended that you turn on [persistence features](https://redis.io/topics/persistence) in Redis to preserve your message queues in the event that Redis crashes or is restarted. 

### Configure Message Queue ###
To configure the message queue, add the following in your startup.cs file in the ConfigureServices method
```
// Configure message queue services
var applicationName = "Some.Unique.Name.For.Your.App";
services.AddSingleton<IRedisClient>(y => new RedisClient(y.GetService<Lazy<IConnectionMultiplexer>>()));
services.AddSingleton<IEventSubscriber>(y => new RedisEventSubscriber(y.GetService<IRedisClient>(), applicationName, y.GetService<IHostingEnvironment>().EnvironmentName, y.GetService<ILoggerFactory>()));
services.AddSingleton<IMessageQueue>(y => new RedisMessageQueue(y.GetService<IRedisClient>(), keyPrefix, y.GetService<IHostingEnvironment>().EnvironmentName));
services.AddScoped<IRepository>(y => new Repository(y.GetService<IEventStore>()));
```

### Configure Event Store
To configure event persistence with Redis, in addition to the above message queue and Redis configuration, add the following to the ConfigureServices method in startup.cs
```
// Configure event store services
var eventStoreSettings = new RedisEventStoreSettings
{
    ApplicationName = applicationName,
    EnableCompression = false
};
services.AddScoped<ISession, Session>();
services.AddSingleton<IEventStore>(y => new RedisEventStore(y.GetService<IRedisClient>(), eventStoreSettings, y.GetService<IMessageQueue>()));
services.AddSingleton<ICache, MemoryCache>();
```
