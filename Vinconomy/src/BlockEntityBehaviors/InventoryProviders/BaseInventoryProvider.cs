using Commercially.Common.Blocks.BlockEntityBehaviors;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public abstract class BaseInventoryProvider : BEBehaviorOwnableContainer
    {
        protected RoomRegistry roomReg;
        protected Room room;
        protected float temperatureCached = -1000f;

        protected BaseInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            //Inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
        }

        protected virtual float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            float num = Api != null && transType == EnumTransitionType.Perish ? GetPerishRate() : 1f;
            if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
            {
                num = 0.25f;
            }

            return baseMul * num;
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            Inventory.Pos = Pos;
            Inventory.Api = api;
            Inventory.ResolveBlocksOrItems();
            Inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
            if (Api.Side == EnumAppSide.Client)
            {
                Inventory.OnInventoryOpened += Inventory_OnInventoryOpenedClient;
            }

            roomReg = Api.ModLoader.GetModSystem<RoomRegistry>();
            Blockentity.RegisterGameTickListener(OnTick, 10000);
        }

        private void Inventory_OnInventoryOpenedClient(IPlayer player)
        {
            OnTick(1f);
        }

        protected virtual void OnTick(float dt)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                return;
            }

            temperatureCached = -1000f;
            if (!HasTransitionables())
            {
                return;
            }

            room = roomReg.GetRoomForPosition(Pos);
            if (room.AnyChunkUnloaded != 0)
            {
                return;
            }

            foreach (ItemSlot item in Inventory)
            {
                try
                {
                    if (item.Itemstack != null)
                    {
                        AssetLocation code = item.Itemstack?.Collectible?.Code;
                        item.Itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, item);
                        if (item.Itemstack?.Collectible?.Code != code)
                        {
                            Blockentity.MarkDirty(redrawOnClient: true);
                        }
                    }
                }
                catch (Exception e)
                {
                    return;
                }

            }

            temperatureCached = -1000f;
        }

        protected virtual bool HasTransitionables()
        {
            foreach (ItemSlot item in Inventory)
            {
                ItemStack itemstack = item.Itemstack;
                if (itemstack != null && itemstack.Collectible != null && itemstack.Collectible.RequiresTransitionableTicking(Api.World, itemstack))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual float GetPerishRate()
        {
            BlockPos blockPos = Pos.Copy();
            blockPos.Y = Api.World.SeaLevel;
            float temperature = temperatureCached;
            if (temperature < -999f)
            {
                temperature = Api.World.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
                if (Api.Side == EnumAppSide.Server)
                {
                    temperatureCached = temperature;
                }
            }
            if (room == null)
            {
                room = roomReg.GetRoomForPosition(Pos);
            }

            float num = 0f;
            float num2 = room.SkylightCount / (float)Math.Max(1, room.SkylightCount + room.NonSkylightCount);
            if (room.IsSmallRoom)
            {
                num = 1f;
                num -= 0.4f * num2;
                num -= 0.5f * GameMath.Clamp(room.NonCoolingWallCount / (float)Math.Max(1, room.CoolingWallCount), 0f, 1f);
            }
            int lightLevel = Api.World.BlockAccessor.GetLightLevel(Pos, EnumLightLevelType.OnlySunLight);
            float num3 = 0.1f;
            num3 = room.IsSmallRoom ? num3 + (0.3f * num + 1.75f * num2) : !(room.ExitCount <= 0.1f * (room.CoolingWallCount + room.NonCoolingWallCount)) ? num3 + 0.5f * num2 : num3 + 1.25f * num2;
            num3 = GameMath.Clamp(num3, 0f, 1.5f);
            float num4 = temperature + GameMath.Clamp(lightLevel - 11, 0, 10) * num3;
            float v = 5f;
            float val = GameMath.Lerp(num4, v, num);
            val = Math.Min(val, num4);
            return Math.Max(0.1f, Math.Min(2.4f, (float)Math.Pow(3.0, (double)(val / 19f) - 1.2) - 0.1f));
        }
    }
}
