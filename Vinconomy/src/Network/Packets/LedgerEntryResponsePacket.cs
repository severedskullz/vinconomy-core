using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Vinconomy.Network.Packets
{
    [ProtoContract]
    public class LedgerEntryResponsePacket
    {
        [ProtoMember(1)]
        public Dictionary<string, List<LedgerEntry>> entries;
    }

    [ProtoContract]
    public class LedgerEntry
    {
        [ProtoMember(1)]
        public string Customer;

        [ProtoMember(2)]
        public string ProductCode;
        [ProtoMember(3)]
        public int ProductQuantity;
        [ProtoMember(4)]
        public byte[] ProductAttributes;
        [ProtoMember(5)]
        public string CurrencyCode;
        [ProtoMember(6)]
        public int CurrencyQuantity;
        [ProtoMember(7)]
        public byte[] CurrencyAttributes;

    }
}