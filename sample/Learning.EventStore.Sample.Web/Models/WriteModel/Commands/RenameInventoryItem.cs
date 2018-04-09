using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.Cqrs;
using Learning.EventStore.Domain;
using Learning.EventStore.Sample.Web.Models.Aggregates;

namespace Learning.EventStore.Sample.Web.Models.WriteModel.Commands
{
    public class RenameInventoryItem : ICommand
    {
        public string Id { get; }
        public string Name { get; }

        public RenameInventoryItem(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class RenameInventoryItemHandler : IAsyncCommandHandler<RenameInventoryItem>
    {
        private readonly ISession _session;

        public RenameInventoryItemHandler(ISession session)
        {
            _session = session;
        }

        public async Task Handle(RenameInventoryItem command)
        {
            var item = await _session.GetAsync<InventoryItem>(command.Id);
            item.ChangeName(command.Name);
            await _session.CommitAsync();
        }
    }
}
