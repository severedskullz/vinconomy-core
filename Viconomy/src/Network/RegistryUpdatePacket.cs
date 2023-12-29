using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract]
    public class RegistryUpdatePacket
    {
        [ProtoMember(1)]
        public RegistryUpdate[] registry;

        public RegistryUpdatePacket()
        {
        }


        public RegistryUpdatePacket(RegistryUpdate[] registry)
        {
            this.registry = registry;
        }

    }

    [ProtoContract]
    public class RegistryUpdate
    {
        [ProtoMember(1)]
        public int ID;

        [ProtoMember(2)]
        public string Name;

        [ProtoMember(3)]
        public string Owner;

        public RegistryUpdate() { }
        public RegistryUpdate(string Owner, int ID, string Name)
        {
            this.Owner = Owner;
            this.ID = ID;
            this.Name = Name;

        }

    }
}
