using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.Cqrs;
using Learning.EventStore.Domain;
using Learning.EventStore.Sample.Web.Models.Aggregates;

namespace Learning.EventStore.Sample.Web.Models.WriteModel.Commands
{
    public class CheckInItemsToInventory : ICommand
    {
        public string Id { get; }
        public int Count { get; }

        public CheckInItemsToInventory(string id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    public class CheckInItemsToInventoryHandler : IAsyncCommandHandler<CheckInItemsToInventory>
    {
        private readonly ISession _session;

        public CheckInItemsToInventoryHandler(ISession session)
        {
            _session = session;
        }

        public async Task Handle(CheckInItemsToInventory command)
        {
            var item = await _session.GetAsync<InventoryItem>(command.Id);
            item.CheckIn(command.Count);
            await _session.CommitAsync();
        }
    }
}
