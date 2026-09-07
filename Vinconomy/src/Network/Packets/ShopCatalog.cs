using Commercially.Common.Registry.Packets;
using ProtoBuf;

namespace Commercially.Vinconomy.Network.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopCatalog
    {
        public OwnableEntry Ownable;
        public ShopProductList ProductList;
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string WebHook { get; set; }

    }
}
