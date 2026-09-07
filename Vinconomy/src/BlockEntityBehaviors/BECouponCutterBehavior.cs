
using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Common.Util;
using Commercially.Vinconomy.Util;
using System.Collections.Generic;
using System.IO;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.Slots;
using Vinconomy.ItemTypes;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors
{
    public class BECouponCutterBehavior : BlockEntityBehavior, IBlockEntityComponent, IInventoryProvider
    {
        private VinconGenericInventory inventory;
        public string BonusType { get; private set; } = ItemCoupon.BONUS_TYPE_DISCOUNT;
        public string DiscountType { get; private set; } = ItemCoupon.DISCOUNT_TYPE_UNIT;
        public string CouponName { get; private set; }
        public int CouponValue { get; private set; }
        public bool ConsumeCoupon { get; private set; }
        public bool ItemBlacklist { get; private set; }
        public int[] AppliedShops { get; private set; }

        public InventoryBase Inventory => inventory;

        private int NumCouponsPerCraft = 16;

        public BECouponCutterBehavior(BlockEntity blockentity) : base(blockentity)
        {
            this.inventory = new VinconGenericInventory(11, null, Api, onNewSlot);
            AppliedShops = [];
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            inventory.LateInitialize($"CouponCutter-{Blockentity.Pos}", api);
        }

        private ItemSlot onNewSlot(int slotId, InventoryGeneric self)
        {
            if (slotId == 0)
            {
                FilteredItemSlot slot = new FilteredItemSlot(self);
                slot.Filter = IsPaper;
                slot.BackgroundIcon = "vicon-paper";
                return slot;
            }
            else
            {
                VinconCloningSlot slot = new VinconCloningSlot(self);
                slot.BackgroundIcon = "vicon-general";
                return slot;
            }
        }

        public static bool IsPaper(ItemSlot slot)
        {
            if (slot.Itemstack?.Item == null)
                return false;

            return slot.Itemstack.Item.Code.Path.Contains("paper");
        }

        private void CutCoupons(IPlayer byPlayer)
        {
            if (Inventory[0].Itemstack == null)
                return;

            var coupon = new ItemStack(Api.World.GetItem(new AssetLocation("vinconomy:coupon")), NumCouponsPerCraft);
            ITreeAttribute tree = coupon.Attributes;
            tree.SetString(ItemCoupon.OWNER, byPlayer.PlayerUID);
            tree.SetString(ItemCoupon.OWNER_NAME, byPlayer.PlayerName);
            tree.SetString(ItemCoupon.NAME, CouponName);
            tree.SetInt(ItemCoupon.VALUE, CouponValue);
            tree.SetString(ItemCoupon.DISCOUNT_TYPE, DiscountType);
            tree.SetString(ItemCoupon.BONUS_TYPE, BonusType);
            tree.SetBool(ItemCoupon.CONSUME_COUPON, ConsumeCoupon);
            tree.SetBool(ItemCoupon.IS_BLACKLIST, ItemBlacklist);



            CommerciallyModSystem modSystem = Api.ModLoader.GetModSystem<CommerciallyModSystem>();
            IOwnableRegistry registry = modSystem.OwnableRegistry;

            // Save all shops which we either own, or have access to (Might remove that part)
            // Note: Changing the shop list would erase shops not actually owned - Meaning we can create coupons for a cutter that someone else set up, but the second
            // we change the available shops, those shops vanish from the list. Additionally, this doesnt work for "all" shops when none is explicitly selected
            List<OwnableRegistration> validShops = new List<OwnableRegistration>();
            for (int i = 0; i < AppliedShops.Length; i++)
            {
                OwnableRegistration reg = registry.GetOwnable(AppliedShops[i]);
                if (reg != null)
                {
                    if (reg.CanAccess(byPlayer))
                    {
                        validShops.Add(reg);
                    }
                    else
                    {
                        modSystem.Mod.Logger.Warning($"Player {byPlayer.PlayerName} did not have access to create coupon for shop [{reg.ID}] {reg.Name} owned by {reg.OwnerName}. Skipping...");
                    }
                }
                else
                {
                    modSystem.Mod.Logger.Warning($"Could not find shop with ID of {AppliedShops[i]}. Skipping...");
                }
            }
            if (validShops.Count > 0)
            {
                ITreeAttribute appliedTree = tree.GetOrAddTreeAttribute(ItemCoupon.APPLIED_SHOPS);
                for (int i = 0; i < validShops.Count; i++)
                {
                    appliedTree.SetLong("ID-" + i.ToString(), validShops[i].ID);
                    appliedTree.SetString("Name-" + i.ToString(), validShops[i].Name);
                }
                tree.SetInt(ItemCoupon.APPLIED_SHOPS_COUNT, validShops.Count);
            }


            // Save all Whitelisted/Blacklisted items that were not null
            List<string> items = new List<string>();
            for (int i = 1; i < inventory.Count; i++)
            {
                ItemStack stack = inventory[i].Itemstack;
                if (stack != null)
                {
                    items.Add(stack.Collectible.Code);
                }

            }
            if (items.Count > 0)
            {
                ITreeAttribute itemList = tree.GetOrAddTreeAttribute(ItemCoupon.ITEM_LIST);
                for (int i = 0; i < items.Count; i++)
                {
                    itemList.SetString(i.ToString(), items[i]);
                }
                tree.SetInt(ItemCoupon.ITEM_LIST_COUNT, items.Count);
            }

            Inventory[0].TakeOut(1);
            Inventory[0].MarkDirty();

            Api.World.SpawnItemEntity(coupon, Pos.AddCopy(0.5f, 0.5f, 0.5f), null);
            Api.World.PlaySoundAt(new AssetLocation("sounds/tool/scythe2"), Pos, 0.5f, null, true, 16, 1);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            inventory.FromTreeAttributes(tree);
            CouponName = tree.GetString(ItemCoupon.NAME);
            AppliedShops = new int[tree.GetInt(ItemCoupon.APPLIED_SHOPS_COUNT)];
            for (int i = 0; i < AppliedShops.Length; i++)
            {
                AppliedShops[i] = tree.GetInt("AS" + i);
            }
            CouponValue = tree.GetInt(ItemCoupon.VALUE);
            DiscountType = tree.GetString(ItemCoupon.DISCOUNT_TYPE, ItemCoupon.DISCOUNT_TYPE_UNIT);
            BonusType = tree.GetString(ItemCoupon.BONUS_TYPE, ItemCoupon.BONUS_TYPE_DISCOUNT);
            ConsumeCoupon = tree.GetBool(ItemCoupon.CONSUME_COUPON);
            ItemBlacklist = tree.GetBool(ItemCoupon.IS_BLACKLIST);


        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            inventory.ToTreeAttributes(tree);
            tree.SetString(ItemCoupon.NAME, CouponName);
            tree.SetInt(ItemCoupon.APPLIED_SHOPS_COUNT, AppliedShops.Length);
            for (int i = 0; i < AppliedShops.Length; i++)
            {
                tree.SetInt("AS" + i, AppliedShops[i]);
            }
            tree.SetInt(ItemCoupon.VALUE, CouponValue);
            tree.SetString(ItemCoupon.DISCOUNT_TYPE, GetDefaultValue(DiscountType, ItemCoupon.DISCOUNT_TYPE_UNIT));
            tree.SetString(ItemCoupon.BONUS_TYPE, GetDefaultValue(BonusType, ItemCoupon.BONUS_TYPE_DISCOUNT));
            tree.SetBool(ItemCoupon.CONSUME_COUPON, ConsumeCoupon);
            tree.SetBool(ItemCoupon.IS_BLACKLIST, ItemBlacklist);
        }

        private string GetDefaultValue(string val, string defaultVal)
        {
            if (val == null || val.Trim() == "")
                return defaultVal;
            return val;
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            //PrintClientMessage(player, Api.Side + ": OnRecievedClientPacket");
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {

                case CommerciallyConstants.OPEN_GUI:
                    if (inventoryManager == null)
                    {
                        return;
                    }
                    inventoryManager.OpenInventory(this.Inventory);
                    break;


                case CommerciallyConstants.CLOSE_GUI:
                    if (inventoryManager != null)
                    {
                        inventoryManager.CloseInventory(this.Inventory);
                    }
                    break;
                case VinConstants.SET_COUPON_BONUS_TYPE:
                    BonusType = CommUtils.ReadStreamString(data);
                    break;
                case VinConstants.SET_COUPON_DISCOUNT_TYPE:
                    DiscountType = CommUtils.ReadStreamString(data);
                    break;
                case VinConstants.SET_ITEM_NAME:
                    CouponName = CommUtils.ReadStreamString(data);
                    break;
                case VinConstants.SET_COUPON_SHOPS:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int length = reader.ReadInt32();
                        AppliedShops = new int[length];
                        for (int i = 0; i < length; i++)
                        {
                            AppliedShops[i] = reader.ReadInt32();
                        }
                    }
                    break;
                case VinConstants.SET_COUPON_VALUE:
                    CouponValue = CommUtils.ReadStreamInt(data);
                    break;
                case VinConstants.SET_COUPON_CONSUME_ON_PURCHASE:
                    ConsumeCoupon = CommUtils.ReadStreamBool(data);
                    break;
                case VinConstants.SET_COUPON_BLACKLIST:
                    ItemBlacklist = CommUtils.ReadStreamBool(data);
                    break;
                case VinConstants.ACTIVATE_BLOCK:
                    CutCoupons(player);
                    break;

                default:
                    if (packetid < 1000)
                    {

                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                        return;
                    }
                    break;
            }
        }

        public BlockEntity GetBlockEntity()
        {
            return this.Blockentity;
        }
    }
}
