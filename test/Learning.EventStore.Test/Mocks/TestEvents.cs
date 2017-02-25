using System;

namespace Learning.EventStore.Test.Mocks
{
    public class TestAggregateDidSomething : IEvent
    {
        public string Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
    public class TestAggregateDidSomeethingElse : IEvent
    {
        public string Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

    public class TestAggregateDidSomethingHandler : IEventHandler<TestAggregateDidSomething>
    {
        public void Handle(TestAggregateDidSomething message)
        {
            lock (message)
            {
                TimesRun++;
            }
        }

        public int TimesRun { get; private set; }
    }

    public class TestAggregateCreated : IEvent
    {
        public string Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
