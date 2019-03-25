# Learning.EventStore #

Learning.EventStore is a framework for CQRS, Eventsourcing, and messaging based on [CQRSLite](https://github.com/gautema/cqrslite). It uses Redis pub/sub for messaging and offers event persistence in Redis, SQL Server, or PostgreSQL. It is written in C# and targets .NET 4.5.2, .NET 4.6.1, and .NET Standard 2.0. It is developed and maintained by [Learning.com](https://www.learning.com) and is currently being used in production there.

# Features #
* Message publishing with Redis Pub/Sub and an implementation of the [Redis reliable queue pattern](https://redis.io/commands/rpoplpush)
* Eventsourcing with full support for event persistence, replay, and snapshotting
* Event persistence options including Redis, SQL Server, and PostgreSQL out of the box and extensibility points for custom persistence implementations
* Optimistic concurrency checking
* CQRS (Command Query Responsibility Segregation) framework
* Dead letter queue and retry support
* Optional distributed locking to maintain message order in multi-host environments

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
In addition to the documentation below, this repository also contains a [sample application](https://github.com/learningcom/Learning.EventStore/tree/master/sample/Learning.EventStore.Sample.Web) to help you get started.

## Configuration ##
The examples below are using the built-in ASP.NET Core dependency injection framework. Other DI frameworks should be similar.

### Configure Redis Connection ###
In your startup.cs file add the following in the ConfigureServices method replacing the host and port with your Redis host and port. The example assumes localhost with port 6379:
```
// Configure Redis connection
var redisConfigOptions = ConfigurationOptions.Parse("127.0.0.1:6379");
redisConfigOptions.AbortOnConnectFail = false;
services.AddSingleton(new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConfigOptions)));
```
\* **For production environments it is recommended that you turn on [persistence features](https://redis.io/topics/persistence) in Redis to preserve your message queues in the event that Redis crashes or is restarted.**

### Configure Message Queue ###
To configure the message queue, add the following in your startup.cs file in the ConfigureServices method
```
// Configure message queue services
var applicationName = "Some.Unique.Name.For.Your.App";
services.AddSingleton<IRedisClient>(y => new RedisClient(y.GetService<Lazy<IConnectionMultiplexer>>()));
services.AddSingleton<IEventSubscriber>(y => new RedisEventSubscriber(y.GetService<IRedisClient>(), applicationName, y.GetService<IHostingEnvironment>().EnvironmentName, y.GetService<ILoggerFactory>()));
services.AddSingleton<IMessageQueue>(y => new RedisMessageQueue(y.GetService<IRedisClient>(), keyPrefix, y.GetService<IHostingEnvironment>().EnvironmentName));
```

### Configure Event Store ###
To configure event persistence with Redis, in addition to the above message queue and Redis configuration, add the following to the ConfigureServices method in startup.cs
```
// Configure event store services
var eventStoreSettings = new RedisEventStoreSettings
{
    ApplicationName = applicationName,
    EnableCompression = false  //set this to true to enable gzip compression of event data to save memory in Redis
};
services.AddScoped<ISession, Session>();
services.AddSingleton<IEventStore>(y => new RedisEventStore(y.GetService<IRedisClient>(), eventStoreSettings, y.GetService<IMessageQueue>()));
services.AddSingleton<ICache, MemoryCache>();
services.AddScoped<IRepository>(y => new Repository(y.GetService<IEventStore>()));
```

\* **For production environments it is required that you turn on [persistence features](https://redis.io/topics/persistence) in Redis if you are using it to store event data as in the above example. Otherwise your data will be lost if Redis crashes or is restarted**

# Usage #
Learning.EventStore implements the concept of an aggregates as defined in Domain Driven Design. The aggregates in your application are objects that describe a business concept in your domain and how various events act on it to bring it to its current state. Therefore, it is important to think about the aggregates in your domain and the events that act on them up front. The [sample application](https://github.com/learningcom/Learning.EventStore/tree/master/sample/Learning.EventStore.Sample.Web) included in this repository is a simple "inventory management" system and serves as a good starting point for illustrating this concept.

## Creating an Event ##
To begin, create a class that describes an event in your domain and that implements the IEvent interface. These are simple POCOs that just include properties for the data that will be included in your event and stored in the Event Store.
```
public class InventoryItemCreated : IEvent
{
    public string Id { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public int Version { get; set; }
    public string AggregateType { get; set; }
    public string Name { get; set; }
}
```

And here is another that describes the quantity of an item being increased
```
public class ItemsCheckedInToInventory : IEvent
{
    public string Id { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public int Version { get; set; }
    public string AggregateType { get; set; }
    public int Count { get; set; }
}
```

## Creating an Aggregate ##
Aggregates are the fundamental construct in Learning.EventStore. They describe a particular business entity and how events act on it to modify it's state over time. To create an aggregate, simply create a class that inherits from AggregateRoot and contains properties that describe it. For example, here is a stub of the InventoryItem class from the sample application.
```
public class InventoryItem : AggregateRoot
{
    public string Name { get; set; }

    public int Count { get; set; }

    public InventoryItem()
    {
    }
...
```
\* Note - explicitly defining the empty constructor is necessary so the framework can properly create an instance of the class when it's loaded from the event store.

Next, create methods in your aggregate class that apply events and change the state of the aggregate
```
public InventoryItem(string id, string name)
{
    Id = id;
    ApplyChange(new InventoryItemCreated
    {
        Id = id,
        Name = name
    });
}

public void CheckIn(int count)
{
    if (count <= 0) throw new InvalidOperationException("must have a count greater than 0 to add to inventory");
    ApplyChange(new ItemsCheckedInToInventory
    {
        Id = Id,
        Count = count
    });
}

public void Apply(InventoryItemCreated @event)
{
    Name = @event.Name;
}

public void Apply(ItemsCheckedInToInventory @event)
{
    Count += @event.Count;
}
```

The `Apply` methods above are called when the aggregate is loaded from the event store. Using reflection, the framework finds the appropriate `Apply` method to call as it replays the events from the event store, changing the state of the object in order until it gets to the current state.

## Creating, saving, and changing aggregates in the event store
To initially create and save your aggregate in the event store, use the `Session` class. A `Session` allows you to store multiple changes to an aggregate in memory before committing them to the persistent event store all at once.
```
public class CreateInventoryItemHandler
{
    private readonly ISession _session;

    public CreateInventoryItemHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(CreateInventoryItem message)
    {
        var inventoryItem = new InventoryItem(Guid.NewGuid().ToString(), message.Name);

        _session.Add(inventoryItem);
        await _session.CommitAsync();
    }
}
```

To make a change to an existing aggregate, first load the aggregate from the event store using the `GetAsync` method and passing the AggregateId. This will load all of the events from the event store, create an instance of your aggregate class, and apply the events in order to bring the aggregate to it's current state. Next, call the appropriate methods on your aggregate to change it's state, and finally commit the aggregate to the event store.

```
public class CheckInItemsToInventoryHandler
{
    private readonly ISession _session;

    public CheckInItemsToInventoryHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(CheckInItemsToInventory message)
    {
        var item = await _session.GetAsync<InventoryItem>(message.Id);
        item.CheckIn(message.Count);
        await _session.CommitAsync();
    }
}
```

Once you call the `CommitAsync` method on a session, all changes will be stored to the event store and messages will be published to the Redis message queue describing the changes. 

## Subscribing to Events ##
To create a subscription to an event, begin by creating a class that implements the `ISubscription` interface. Then, inject an instance of `RedisEventSubscriber` into this class. Finally, you will create an anonymous callback function that describes the processing you want to occur on the event and pass this function to the `SubscribeAsync` method on `RedisEventSubscriber`. This callback can contain any processing you want, but is typically used to write the data to a read projection in an RDBMS for example. Subscriptions can be in a completely separate service or app, the service only needs to be connected to the same Redis server as the publisher.
```
public class InventoryItemCreatedSubscription : ISubscription
{
    private readonly IEventSubscriber _subscriber;

    public InventoryItemCreatedSubscription(IEventSubscriber subscriber, ILogger logger)
    {
        _subscriber = subscriber;
    }

    public async Task SubscribeAsync()
    {
        void CallBack(InventoryItemCreated data)
        {
            var inventoryItem = new InventoryItemDetails(data.Id, data.Name, 0, data.Version);
            InMemoryDatabase.Details.Add(data.Id, inventoryItem);
        }

        await _subscriber.SubscribeAsync((Action<InventoryItemCreated>)CallBack);
    }
}
```

## Publishing and Subscribing to Messages Without Event Persistence ##
Sometimes you just want to publish messages without persisting events as an alternative communication pattern between microservices for example. Learning.MessageQueue can be used by itself to achieve this.

First, create a class that implements the `IMessage` interface and contains properties for the data you want contained in your message
```
public class MyMessage : IMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

    public string Data { get; set; }
}
```

To publish your message use the `MessageQueue` class
```
public class MyMessagePublisher
{
    private readonly IMessageQueue _messageQueue

    public MyMessagePublisher(IMessageQueue messageQueue)
    {
        _messageQueue = messageQueue
    }

    public async Task Publish(string data)
    {
        var message = new MyMessage { Data = data };
        await _messageQueue.PublishAsync(message);
    }
}
```

You can subscribe to messages using `ISubscription` and `RedisMessageSubscriber` as described in "Subscribing to Events" section above.

# Contributing #
Contributions are always welcome!

1. Fork it
2. Create your feature branch (git checkout -b my-new-feature)
3. Commit your changes (git commit -am 'Add some feature')
4. Push to the branch (git push origin my-new-feature)
5. Create new Pull Request
