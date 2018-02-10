using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Learning.EventStore.Snapshotting;

namespace Learning.EventStore.Test.Mocks
{
    public class TestSnapshotStore : ISnapshotStore
    {
        public bool VerifyGet { get; private set; }
        public bool VerifySave { get; private set; }
        public int SavedVersion { get; private set; }

        public Task<Snapshot> GetAsync(string id)
        {
            VerifyGet = true;
            var snapshot = new TestSnapshotAggregateSnapshot
            {
                Id = Guid.NewGuid().ToString()
            };

            return Task.FromResult((Snapshot)snapshot);
        }

        public Task SaveAsync(Snapshot snapshot)
        {
            VerifySave = true;
            SavedVersion = snapshot.Version;
            return Task.CompletedTask;
        }
    }
}
