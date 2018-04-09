using System.Collections.Generic;
using Learning.EventStore.Sample.Web.Models.ReadModel.Dto;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure
{
    public static class InMemoryDatabase
    {
        public static readonly Dictionary<string, InventoryItemDetails> Details = new Dictionary<string, InventoryItemDetails>();
    }
}
