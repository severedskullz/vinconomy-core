using Vintagestory.API.MathTools;

namespace Viconomy.Registry
{
    public class ViconRegister
    {
        public string Name { get;  set; }
        public string ID { get; internal set; }
        public string Owner { get; internal set; }
        public string OwnerName { get; internal set; }
        public BlockPos Position { get; set; }
    }
}