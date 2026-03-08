using ProtoBuf;

namespace Vinconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ProductDefinition
    {
        public int Id;
        public int ShopId;

        public string ProductCode;
        public byte[] ProductAttributes;
        public int ProductQuantity;

        public string CurrencyCode;
        public byte[] CurrencyAttributes;
        public int CurrencyQuantity;

        public bool IgnoreAttributes;

        public int Supply;

        public int IntervalType;
        public int IntervalDuration;
        public int IntervalPeriod;
        public int IntervalAction;
        public int IntervalActionValue;


        public int SupplyThreshold;
        public int ThresholdScale;
        public int CurrencyLowQuantity;
        public int CurrencyHighQuantity;
        public int IdealSupply;
        public int MaxSupply;
        public bool SalesContribute;
        public bool UnlimitedSupply;
        public bool UnlimitedDemand;

    }
}
