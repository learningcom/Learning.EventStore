using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Domain
{
    public class GetAggregateWithoutConstructor
    {
        private readonly Guid _id;
        private readonly Repository _repository;

        public GetAggregateWithoutConstructor()
        {
            _id = Guid.NewGuid();
            var eventStore = new TestInMemoryEventStore();
            _repository = new Repository(eventStore);
            var aggreagate = new TestAggregateNoParameterLessConstructor(1, _id);
            aggreagate.DoSomething();
            _repository.Save(aggreagate).Wait();
        }

        [Test]
        public async Task Should_throw_missing_parameterless_constructor_exception()
        {
            try
            {
                await _repository.Get<TestAggregateNoParameterLessConstructor>(_id);
                Assert.Fail();
            }
            catch(MissingParameterLessConstructorException e)
            {
                Assert.Pass();
            }
        }
    }
}
