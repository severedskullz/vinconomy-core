using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Viconomy.Registry
{
    [ProtoContract]
    public class ShopRegistration
    {
        [ProtoMember(1)] public int ID { get; internal set; } = -1;
        [ProtoMember(2)] public string Name { get;  set; }
        [ProtoMember(3)] public string Owner { get; internal set; }
        [ProtoMember(4)] public string OwnerName { get; internal set; }
        [ProtoMember(5)] internal int X { get; set; }
        [ProtoMember(6)] internal int Y { get; set; }
        [ProtoMember(7)] internal int Z { get; set; }

        public BlockPos Position { 
            get { 
                if (Y == -1)
                {
                    return null;
                }
                return new BlockPos(X, Y, Z, 0);
            }
            
            set {
                if (value != null)
                {
                    this.X = value.X;
                    this.Y = value.Y;
                    this.Z = value.Z;
                } else
                {
                    this.X = 0;
                    this.Y = -1;
                    this.Z = 0;
                }

            }
        }

    }
}