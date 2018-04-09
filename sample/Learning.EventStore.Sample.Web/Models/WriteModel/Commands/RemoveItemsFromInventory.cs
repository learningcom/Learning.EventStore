using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.Cqrs;
using Learning.EventStore.Domain;
using Learning.EventStore.Sample.Web.Models.Aggregates;

namespace Learning.EventStore.Sample.Web.Models.WriteModel.Commands
{
    public class RemoveItemsFromInventory : ICommand
    {
        public string Id { get; }
        public int Count { get; }

        public RemoveItemsFromInventory(string id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    public class RemoveItemsFromInventoryHandler : IAsyncCommandHandler<RemoveItemsFromInventory>
    {
        private readonly ISession _session;

        public RemoveItemsFromInventoryHandler(ISession session)
        {
            _session = session;
        }

        public async Task Handle(RemoveItemsFromInventory command)
        {
            var item = await _session.GetAsync<InventoryItem>(command.Id);
            item.Remove(command.Count);
            await _session.CommitAsync();
        }
    }
}
