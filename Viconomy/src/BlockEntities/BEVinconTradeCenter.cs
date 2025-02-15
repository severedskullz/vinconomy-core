using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Viconomy.BlockEntities
{
    public class BEVinconTradeCenter : BlockEntity
    {
        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            EntityProperties type = Api.World.GetEntityType(new AssetLocation("vinconomy", "vincon-trader"));
            Entity entity = Api.World.ClassRegistry.CreateEntity(type); // as EntityVinconTrader;
            entity.ServerPos.SetFrom(Pos.ToVec3d());
            Api.World.SpawnEntity(entity);

            return true;
        }
    }
}
