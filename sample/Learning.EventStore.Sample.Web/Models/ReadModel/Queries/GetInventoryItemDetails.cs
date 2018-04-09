using Learning.Cqrs;
using Learning.EventStore.Sample.Web.Models.ReadModel.Dto;
using Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Queries
{
    public class GetInventoryItemDetails : IQuery<InventoryItemDetails>
    {
        public string Id { get; }

        public GetInventoryItemDetails(string id)
        {
            Id = id;
        }
    }

    public class GetInventoryItemDetailsHandler : IQueryHandler<GetInventoryItemDetails, InventoryItemDetails>
    {
        public InventoryItemDetails Handle(GetInventoryItemDetails query)
        {
            var details = InMemoryDatabase.Details[query.Id];

            return details;
        }
    }
}
