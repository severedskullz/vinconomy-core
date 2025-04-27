using ProtoBuf;

namespace Viconomy.Network.Api
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SearchResult
    {
        public string nodeId { get; set; }
        public string nodeName { get; set; }
        public long shopId { get; set; }
        public string shopName { get; set; }
        public string shopOwner { get; set; }
        public string description { get; set; }
    }
}
