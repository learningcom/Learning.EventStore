using System;

namespace Learning.EventStore.Snapshotting
{
    /// <summary>
    /// A memento object of a aggregate in a version.
    /// </summary>
    public abstract class Snapshot
    {
        public string Id { get; set; }
        public int Version { get; set; }
    }
}
