﻿using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Test.Mocks
{
    public class TestRepository : IRepository
    {
        public async Task SaveAsync<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot
        {
            await Task.Run(() =>
            {
                Saved = aggregate;
                if (expectedVersion == 100)
                {
                    throw new Exception();
                }
            }).ConfigureAwait(false);
        }

        public AggregateRoot Saved { get; private set; }

        public async Task<T> GetAsync<T>(string aggregateId) where T : AggregateRoot
        {
            return await Task.Run<T>(() =>
            {
                var obj = (T) Activator.CreateInstance(typeof(T), true);
                obj.LoadFromHistory(new[] {new TestAggregateDidSomething {Id = aggregateId, Version = 1}});
                return obj;
            }).ConfigureAwait(false);
        }
    }
}
