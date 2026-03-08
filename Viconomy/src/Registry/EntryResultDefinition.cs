using ProtoBuf;

namespace Viconomy.Registry
{

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class EntryResultDefinition
    {
        public int Id;
        public int ShopId;
        public int Type;
        public string Code;
        public byte[] Attributes;
        public int Supply;
    }
}
