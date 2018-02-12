using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Learning.EventStore.Snapshotting;

namespace Learning.EventStore.Test.Mocks
{
    class TestInMemorySnapshotStore : ISnapshotStore
    {
        public Task<Snapshot> GetAsync(string id)
        {
            return Task.FromResult(_snapshot);
        }

        public Task SaveAsync(Snapshot snapshot)
        {
            if (snapshot.Version == 0)
            {
                FirstSaved = true;
            }
                
            SavedVersion = snapshot.Version;
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id)
        {
            return Task.FromResult(_snapshot != null);
        }

        private Snapshot _snapshot;
        public int SavedVersion { get; private set; }
        public bool FirstSaved { get; private set; }
    }
}
