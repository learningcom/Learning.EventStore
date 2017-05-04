using System;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class ReplayEventsTest
    {
        [TestMethod]
        public void CallsApplyIfExist()
        {
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());
            aggregate.DoSomething();
            Assert.AreEqual(1, aggregate.DidSomethingCount);
        }

        [TestMethod]
        public void DoesNotFailApplyIfDoesNotExist()
        {
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());
            aggregate.DoSomethingElse();
            Assert.AreEqual(0, aggregate.DidSomethingCount);
        }
    }
}
