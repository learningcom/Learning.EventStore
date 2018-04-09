using System.Collections.Generic;
using System.Linq;
using Learning.Cqrs;
using Learning.EventStore.Sample.Web.Models.ReadModel.Dto;
using Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Queries
{
    public class GetInventoryItems : IQuery<IEnumerable<InventoryItemDetails>>
    {
    }

    public class GetInventoryItemsHandler : IQueryHandler<GetInventoryItems, IEnumerable<InventoryItemDetails>>
    {
        public IEnumerable<InventoryItemDetails> Handle(GetInventoryItems query)
        {
            var items = InMemoryDatabase.Details.Values.ToList();

            return items;
        }
    }
}
