using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Test.Mocks
{
    public class TestAggregateNoParameterLessConstructor : AggregateRoot
    {
        public TestAggregateNoParameterLessConstructor(int i, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
        }

        public void DoSomething()
        {
            ApplyChange(new TestAggregateDidSomething());
        }
    }
}
