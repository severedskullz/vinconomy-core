using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.GUI;
using Viconomy.Inventory.Slots;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities.Unfinished
{
    public class BEVinconCouponCutter : BlockEntityContainer
    {
        private InventoryGeneric inventory;
        private GuiViconCouponCutter invDialog;
        public string BonusType { get; private set; }
        public string DiscountType { get; private set; }
        public string CouponName { get; private set; }
        public int CouponValue { get; private set; }
        public bool ConsumeCoupon { get; private set; }
        public bool ItemBlacklist { get; private set; }
        public int[] AppliedShops { get; private set; }

        private const int NumCouponsPerCraft = 16;

        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "InventoryGeneric";

        public BEVinconCouponCutter()
        {
            this.inventory = new InventoryGeneric(11, null, Api, onNewSlot);
        }

        private ItemSlot onNewSlot(int slotId, InventoryGeneric self)
        {
            if (slotId == 0)
            {
                ViconItemSlot slot = new ViconItemSlot(self, 0, slotId);
                slot.setFilter(IsPaper);
                slot.BackgroundIcon = "vicon-general2";
                return slot;
            }
            else
            {
                return new ViconCurrencySlot(self);
            }
        }

        public void PrintCoupon()
        {

        }

        #region GUI

        protected void OpenShopForPlayer(IPlayer byPlayer)
        {

            if (Api.Side == EnumAppSide.Server)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    TreeAttribute tree = new TreeAttribute();
                    writer.Write(CouponName == null ? "Coupon" : CouponName);
                    writer.Write(AppliedShops.Length);
                    foreach (int shop in AppliedShops)
                    {
                        writer.Write(shop);
                    }
                    writer.Write(CouponValue);
                    writer.Write(DiscountType == null ? "Units" : DiscountType);
                    writer.Write(BonusType == null ? "Bonus" : BonusType);
                    writer.Write(ConsumeCoupon);
                    writer.Write(ItemBlacklist);

                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);

                    data = ms.ToArray();
                }
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.OPEN_GUI, data);
                byPlayer.InventoryManager.OpenInventory(this.inventory);
            }
        }

        protected virtual void OpenShopGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);

                CouponName = reader.ReadString();
                AppliedShops = new int[reader.ReadInt32()];
                for (int i = 0; i < AppliedShops.Length; i++)
                {
                    AppliedShops[i] = reader.ReadInt32();
                }
                CouponValue = reader.ReadInt32();
                DiscountType = reader.ReadString();
                BonusType = reader.ReadString();
                ConsumeCoupon = reader.ReadBoolean();
                ItemBlacklist = reader.ReadBoolean();
                tree.FromBytes(reader);
            }

            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
           
            this.invDialog = new GuiViconCouponCutter("Coupon Cutter", this.Inventory, this.Pos, this.Api as ICoreClientAPI);
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;
                (this.Api as ICoreClientAPI).Network.SendBlockEntityPacket(this.Pos, VinConstants.CLOSE_GUI, null);
            };
            //Console.WriteLine(Api.Side + ": Attempted to open Shop GUI");
        }

        protected virtual void CloseGui(IClientWorldAccessor clientWorld)
        {
            clientWorld.Player.InventoryManager.CloseInventory(this.Inventory);

            if (invDialog != null)
            {
                if (this.invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }
            }

            if (this.invDialog != null)
            {
                this.invDialog.Dispose();
            }

            this.invDialog = null;
        }

        #endregion


        #region Networking

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            //PrintClientMessage(player, Api.Side + ": OnRecievedClientPacket");
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {
                
                case VinConstants.OPEN_GUI:
                    if (inventoryManager == null)
                    {
                        return;
                    }
                    inventoryManager.OpenInventory(this.Inventory);
                    break;
                

                case VinConstants.CLOSE_GUI:
                    if (inventoryManager != null)
                    {
                        inventoryManager.CloseInventory(this.Inventory);
                    }
                    break;
                case VinConstants.SET_COUPON_BONUS_TYPE:
                    BonusType = ReadStreamString(data);
                    break;
                case VinConstants.SET_COUPON_DISCOUNT_TYPE:
                    DiscountType = ReadStreamString(data);
                    break;
                case VinConstants.SET_ITEM_NAME:
                    CouponName = ReadStreamString(data);
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
                case VinConstants.SET_ITEM_PRICE:
                    CouponValue = ReadStreamInt(data);
                    break;
                case VinConstants.SET_COUPON_CONSUME_ON_PURCHASE:
                    ConsumeCoupon = ReadStreamBool(data);
                    break;
                case VinConstants.SET_COUPON_BLACKLIST:
                    ItemBlacklist = ReadStreamBool(data);
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

        private void CutCoupons(IPlayer byPlayer)
        {
            if (Inventory[0].Itemstack == null)
                return;

            var coupon = new ItemStack(Api.World.GetItem(new AssetLocation("vinconomy:coupon")), NumCouponsPerCraft);
            ITreeAttribute tree = coupon.Attributes;
            tree.SetString("CouponOwner", byPlayer.PlayerUID);
            tree.SetString("CouponOwnerName", byPlayer.PlayerName);
            tree.SetString("CouponName", CouponName);
            tree.SetInt("CouponValue", CouponValue);
            tree.SetString("DiscountType", DiscountType);
            tree.SetString("BonusType", BonusType);
            tree.SetBool("ConsumeCoupon", ConsumeCoupon);
            tree.SetBool("ItemBlacklist", ItemBlacklist);
            
           

            VinconomyCoreSystem modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            ShopRegistry registry = modSystem.GetRegistry();

            // Save all shops which we either own, or have access to (Might remove that part)
            // Note: Changing the shop list would erase shops not actually owned - Meaning we can create coupons for a cutter that someone else set up, but the second
            // we change the available shops, those shops vanish from the list. Additionally, this doesnt work for "all" shops when none is explicitly selected
            List<ShopRegistration> validShops = new List<ShopRegistration>();
            for (int i = 0; i < AppliedShops.Length; i++)
            {
                ShopRegistration reg = registry.GetShop(AppliedShops[i]);
                if (reg != null) {
                    if (reg.CanAccess(byPlayer))
                    {
                        validShops.Add(reg);
                    } else
                    {
                        modSystem.Mod.Logger.Warning($"Player {byPlayer.PlayerName} did not have access to create coupon for shop [{reg.ID}] {reg.Name} owned by {reg.OwnerName}. Skipping...");
                    }
                } else
                {
                    modSystem.Mod.Logger.Warning($"Could not find shop with ID of {AppliedShops[i]}. Skipping...");
                }
            }
            ITreeAttribute appliedTree = tree.GetOrAddTreeAttribute("AppliedShops");
            for (int i = 0; i < validShops.Count; i++)
            {
                appliedTree.SetInt("ID-"+i.ToString(), validShops[i].ID);
                appliedTree.SetString("Name-" + i.ToString(), validShops[i].Name);
            }
            tree.SetInt("AppliedShopLength", validShops.Count);

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
            ITreeAttribute itemList = tree.GetOrAddTreeAttribute("ItemList");
            for (int i = 0; i < items.Count; i++)
            {
                itemList.SetString(i.ToString(), items[i]);
            }
            tree.SetInt("ItemListLength", items.Count);


            Api.World.SpawnItemEntity(coupon, Pos.AddCopy(0.5f, 0.5f, 0.5f), null);
            Api.World.PlaySoundAt(new AssetLocation("sounds/tool/scythe2"),Pos,0.5f,null,true,16,1);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;

            switch(packetid)
            {
                case VinConstants.OPEN_GUI:
                    OpenShopGui(data);
                    break;
                case VinConstants.CLOSE_GUI:
                    CloseGui(clientWorld);
                    break;
                case VinConstants.TOGGLE_GUI:

                    if (invDialog != null)
                    {
                        Console.WriteLine(Api.Side + ": Toggling GUI OFF");
                        CloseGui(clientWorld);
                    }
                    else
                    {
                        Console.WriteLine(Api.Side + ": Toggling GUI ON");
                        OpenShopGui(data);
                    }
                    break;

                default:
                    break;
            }


        }

        static bool ReadStreamBool(byte[] data)
        {
            bool value;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                value = reader.ReadBoolean();
            }
            return value;
        }

        static string ReadStreamString(byte[] data)
        {
            string value;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                value = reader.ReadString();
            }
            return value;
        }

        static int ReadStreamInt(byte[] data)
        {
            int value;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                value = reader.ReadInt32();
            }
            return value;
        }

        #endregion

        public static bool IsPaper(ItemSlot slot)
        {
            if (slot.Itemstack?.Item == null)
                return false;

            return slot.Itemstack.Item.Code.Path.Contains("paper");
        }

        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            OpenShopForPlayer(byPlayer);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            CouponName = tree.GetString("CouponName");
            AppliedShops = new int[tree.GetInt("AppliedShopLength")];
            for (int i = 0; i < AppliedShops.Length; i++)
            {
                AppliedShops[i] = tree.GetInt("AS" + i);
            }
            CouponValue = tree.GetInt("CouponValue");
            DiscountType = tree.GetString("DiscountType");
            BonusType = tree.GetString("BonusType");
            ConsumeCoupon = tree.GetBool("ConsumeCoupon");
            ItemBlacklist = tree.GetBool("ItemBlacklist");


        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("CouponName", CouponName);
            tree.SetInt("AppliedShopLength", AppliedShops.Length);
            for (int i = 0; i < AppliedShops.Length; i++)
            {
                tree.SetInt("AS"+i, AppliedShops[i]);
            }
            tree.SetInt("CouponValue", CouponValue);
            tree.SetString("DiscountType", DiscountType);
            tree.SetString("BonusType", BonusType);
            tree.SetBool("ConsumeCoupon", ConsumeCoupon);
            tree.SetBool("ItemBlacklist", ItemBlacklist);
        }
    }
}   
