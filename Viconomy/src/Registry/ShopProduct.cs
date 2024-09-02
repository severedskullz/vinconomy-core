using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Registry
{
    [ProtoContract]
    public class ShopProduct
    {
        [ProtoMember(1)] public string ProductName { get; internal set; }
        [ProtoMember(2)] public string ProductCode { get; internal set; }
        [ProtoMember(3)] public byte[] ProductAttributes { get; internal set; }
        [ProtoMember(4)] public int ProductQuantity { get; internal set; }
        [ProtoMember(5)] public int TotalStock { get; internal set; }
        [ProtoMember(6)] public byte[] CurrencyAttributes { get; internal set; }
        [ProtoMember(7)] public int CurrencyAmount { get; internal set; }
        [ProtoMember(8)] public string CurrencyCode { get; internal set; }
        [ProtoMember(9)] public string CurrencyName { get; internal set; }
       
    }
}
