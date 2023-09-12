using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract]
    internal class RegistryUpdatePacket
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
}
