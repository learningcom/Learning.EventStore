using System;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Domain
{
    public class ReplayEventsTest
    {
        [Test]
        public void CallsApplyIfExist()
        {
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());
            aggregate.DoSomething();
            Assert.AreEqual(1, aggregate.DidSomethingCount);
        }

        [Test]
        public void DoesNotFailApplyIfDoesNotExist()
        {
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());
            aggregate.DoSomethingElse();
            Assert.AreEqual(0, aggregate.DidSomethingCount);
        }
    }
}
