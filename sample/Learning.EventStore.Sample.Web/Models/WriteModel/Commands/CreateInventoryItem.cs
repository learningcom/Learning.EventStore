using System;
using System.Threading.Tasks;
using Learning.Cqrs;
using Learning.EventStore.Domain;
using Learning.EventStore.Sample.Web.Models.Aggregates;

namespace Learning.EventStore.Sample.Web.Models.WriteModel.Commands
{
    public class CreateInventoryItem : ICommand
    {
        public string Name { get; }

        public CreateInventoryItem(string name)
        {
            Name = name;
        }
    }

    public class CreateInventoryItemHandler : IAsyncCommandHandler<CreateInventoryItem>
    {
        private readonly ISession _session;

        public CreateInventoryItemHandler(ISession session)
        {
            _session = session;
        }

        public async Task Handle(CreateInventoryItem message)
        {
            var inventoryItem = new InventoryItem(Guid.NewGuid().ToString(), message.Name);

            _session.Add(inventoryItem);
            await _session.CommitAsync();
        }
    }
}
