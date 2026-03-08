using ProtoBuf;
using Vintagestory.API.Common;

namespace Vinconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CurrencyDefinition
    {
        public int Id;
        public int ShopId;

        public string CurrencyCode;
        public byte[] CurrencyAttributes;

        public bool IgnoreAttributes;

        public int Supply;

        public int IntervalType;
        public int IntervalDuration;
        public int IntervalPeriod;
        public int IntervalAction;
        public int IntervalActionValue;
    }
}
