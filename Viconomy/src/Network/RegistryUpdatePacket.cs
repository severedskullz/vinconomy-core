using ProtoBuf;
using System.Collections.Generic;
using Vinconomy.Registry;
using Vintagestory.API.Server;

namespace Vinconomy.Network
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RegistryUpdatePacket
    {
        public List<ShopUpdatePacket> registry;

        public RegistryUpdatePacket() { }

        public RegistryUpdatePacket(List<ShopUpdatePacket> registry)
        {
            this.registry = registry;
        }

    }
}
