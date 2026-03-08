using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Vinconomy.Network
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
        public string ProductAttributes;
        [ProtoMember(5)]
        public string CurrencyCode;
        [ProtoMember(6)]
        public int CurrencyQuantity;
        [ProtoMember(7)]
        public string CurrencyAttributes;

    }
}