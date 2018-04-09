using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Dto
{
    public class InventoryItemDetails
    {
        public string Id;
        public string Name;
        public int CurrentCount;
        public int Version;

        public InventoryItemDetails(string id, string name, int currentCount, int version)
        {
            Id = id;
            Name = name;
            CurrentCount = currentCount;
            Version = version;
        }
    }
}
