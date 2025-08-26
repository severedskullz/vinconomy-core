using System.Collections.Generic;
using System.Text;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities.TextureSwappable
{
    //This is, for the most part, BlockEntityContainer. Since I need to inherit BETextureSwappableBlock instead, I have to reimplement it...
    public abstract class BETextureSwappableBlockContainer : BETextureSwappableBlock, IBlockEntityContainer
    {
        private RoomRegistry roomReg;

        protected Room room;

        protected float temperatureCached = -1000f;

        public abstract InventoryBase Inventory { get; }
        IInventory IBlockEntityContainer.Inventory => Inventory;

        public abstract string InventoryClassName { get; }

        public virtual void DropContents(Vec3d atPos)
        {
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            Inventory.Pos = Pos;
            Inventory.ResolveBlocksOrItems();
            Inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
            if (api.Side == EnumAppSide.Client)
            {
                Inventory.OnInventoryOpened += Inventory_OnInventoryOpenedClient;
            }

            RegisterGameTickListener(OnTick, 10000);
            roomReg = Api.ModLoader.GetModSystem<RoomRegistry>();
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
                if (item.Itemstack != null)
                {
                    AssetLocation code = item.Itemstack.Collectible.Code;
                    item.Itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, item);
                    if (item.Itemstack?.Collectible.Code != code)
                    {
                        MarkDirty(redrawOnClient: true);
                    }
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

        protected virtual float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            float num = Api != null && transType == EnumTransitionType.Perish ? GetPerishRate() : 1f;
            if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
            {
                num = 0.25f;
            }

            return baseMul * num;
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

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack?.Block is BlockContainer blockContainer)
            {
                ItemStack[] contents = blockContainer.GetContents(Api.World, byItemStack);
                if (contents != null && contents.Length > Inventory.Count)
                {
                    throw new InvalidOperationException($"OnBlockPlaced stack copy failed. Trying to set {contents.Length} stacks on an inventory with {Inventory.Count} slots");
                }

                int num = 0;
                while (contents != null && num < contents.Length)
                {
                    Inventory[num].Itemstack = contents[num]?.Clone();
                    num++;
                }
            }
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.World is IServerWorldAccessor)
            {
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            base.OnBlockBroken(byPlayer);
        }

        public ItemStack[] GetNonEmptyContentStacks(bool cloned = true)
        {
            List<ItemStack> list = new List<ItemStack>();
            foreach (ItemSlot item in Inventory)
            {
                if (!item.Empty)
                {
                    list.Add(cloned ? item.Itemstack.Clone() : item.Itemstack);
                }
            }

            return list.ToArray();
        }

        public ItemStack[] GetContentStacks(bool cloned = true)
        {
            List<ItemStack> list = new List<ItemStack>();
            foreach (ItemSlot item in Inventory)
            {
                list.Add(cloned ? item.Itemstack?.Clone() : item.Itemstack);
            }

            return list.ToArray();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (Inventory != null)
            {
                ITreeAttribute treeAttribute = new TreeAttribute();
                Inventory.ToTreeAttributes(treeAttribute);
                tree["inventory"] = treeAttribute;
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            //room = roomReg.GetRoomForPosition(Pos);
            float num = GetPerishRate();
            if (Inventory is InventoryGeneric)
            {
                InventoryGeneric inventoryGeneric = Inventory as InventoryGeneric;
                if (inventoryGeneric.TransitionableSpeedMulByType != null && inventoryGeneric.TransitionableSpeedMulByType.TryGetValue(EnumTransitionType.Perish, out var value))
                {
                    num *= value;
                }

                if (inventoryGeneric.PerishableFactorByFoodCategory != null)
                {
                    dsc.AppendLine(Lang.Get("Stored food perish speed:"));
                    foreach (KeyValuePair<EnumFoodCategory, float> item in inventoryGeneric.PerishableFactorByFoodCategory)
                    {
                        string text = Lang.Get("foodcategory-" + item.Key.ToString().ToLowerInvariant());
                        dsc.AppendLine(Lang.Get("- {0}: {1}x", text, Math.Round(num * item.Value, 2)));
                    }

                    if (inventoryGeneric.PerishableFactorByFoodCategory.Count != Enum.GetValues(typeof(EnumFoodCategory)).Length)
                    {
                        dsc.AppendLine(Lang.Get("- {0}: {1}x", Lang.Get("food_perish_speed_other"), Math.Round(num, 2)));
                    }

                    return;
                }
            }

            dsc.AppendLine(Lang.Get("Stored food perish speed: {0}x", Math.Round(num, 2)));
        }
    }
}
