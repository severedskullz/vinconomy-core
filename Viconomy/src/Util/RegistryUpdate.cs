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

        [ProtoMember(3)]
        public string Owner;

        public RegistryUpdate() { }
        public RegistryUpdate(string Owner, string ID, string Name)
        {
            this.Owner = Owner; 
            this.ID = ID;
            this.Name = Name;

        }

    }
}
