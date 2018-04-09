using System;
using Learning.EventStore.Domain;
using Learning.EventStore.Sample.Web.Models.Events;

namespace Learning.EventStore.Sample.Web.Models.Aggregates
{
    public class InventoryItem : AggregateRoot
    {
        public string Name { get; set; }

        public int Count { get; set; }

        public InventoryItem()
        {
        }

        public InventoryItem(string id, string name)
        {
            Id = id;
            ApplyChange(new InventoryItemCreated
            {
                Id = id,
                Name = name
            });
        }

        public void CheckIn(int count)
        {
            if (count <= 0) throw new InvalidOperationException("must have a count greater than 0 to add to inventory");
            ApplyChange(new ItemsCheckedInToInventory
            {
                Id = Id,
                Count = count
            });
        }

        public void Remove(int count)
        {
            if (count <= 0) throw new InvalidOperationException("can't remove negative amount from inventory");
            ApplyChange(new ItemsRemovedFromInventory
            {
                Id = Id,
                Count = count
            });

        }

        public void ChangeName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("newName");
            ApplyChange(new InventoryItemRenamed
            {
                Id = Id,
                Name = name
            });
        }

        public void Apply(InventoryItemCreated @event)
        {
            Name = @event.Name;
        }

        public void Apply(ItemsCheckedInToInventory @event)
        {
            Count += @event.Count;
        }

        public void Apply(ItemsRemovedFromInventory @event)
        {
            Count -= @event.Count;
        }

        public void Apply(InventoryItemRenamed @event)
        {
            Name = @event.Name;
        }
    }
}
