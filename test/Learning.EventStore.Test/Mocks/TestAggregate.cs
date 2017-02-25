using System;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Test.Mocks
{
    public class TestAggregate : AggregateRoot
    {
        private TestAggregate() { }
        public TestAggregate(string id)
        {
            Id = id;
            ApplyChange(new TestAggregateCreated());
        }

        public int DidSomethingCount;

        public void DoSomething()
        {
            ApplyChange(new TestAggregateDidSomething());
        }

        public void DoSomethingElse()
        {
            ApplyChange(new TestAggregateDidSomeethingElse());
        }

        public void Apply(TestAggregateDidSomething e)
        {
            DidSomethingCount++;
        }
    }
}
