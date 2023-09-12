using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract]
    internal class RegistryUpdate
    {
        [ProtoMember(1)]
        public string ID;

        [ProtoMember(2)]
        public string Name;

        public RegistryUpdate(string ID, string Name)
        {
            this.ID = ID;
            this.Name = Name;

        }

    }
}
