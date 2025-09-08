using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities.TextureSwappable;
using Viconomy.GUI;
using Viconomy.Inventory.Slots;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockEntities.Unfinished
{
    public class BEVinconResidence : BETextureSwappableBlockContainer, IOwnableStall, IInteractableStall
    {
        public string Owner { get; protected set; }
        public string OwnerName { get; protected set; }
        public string Tennant { get; protected set; }
        public string TennantName { get; protected set; }
        public int RegisterID { get; protected set; }
        public bool IsAdminShop { get; protected set; }
        public string Name { get; protected set; }

        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "InventoryGeneric";

        InventoryGeneric inventory;
        protected GuiDialogBlockEntity invDialog;

        public BEVinconResidence()
        {
            inventory = new InventoryGeneric(20, null, Api, onNewSlot);
        }

        private ItemSlot onNewSlot(int slotId, InventoryGeneric self)
        {
            if (slotId == 0)
            {
                return new ItemSlotOutput(self);
            }
            else if (slotId == 1)
            {
                ViconItemSlot slot = new ViconItemSlot(self, 0, slotId);
                slot.setFilter(Filters.ViconomyFilters.IsEmptyGachaSlot);
                slot.BackgroundIcon = "vicon-general2";
                return slot;
            }
            else
            {
                return new ViconItemSlot(self, 0, slotId);
            }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

        }

        public void SetIsAdminShop(bool adminShop)
        {
            IsAdminShop = adminShop;
        }

        public void SetOwner(IPlayer player)
        {
            Owner = player.PlayerUID;
            OwnerName = player.PlayerName;
        }
        public void SetOwner(string playerUUID, string playerName)
        {
            Owner = playerUUID;
            OwnerName = playerName;
        }

        public void SetRegisterID(int registerID)
        {
            RegisterID = registerID;
        }

        public void ShowClaims()
        {
            Cuboidi cuboidi = new Cuboidi();
            ICoreClientAPI api = (ICoreClientAPI)Api;
            List<BlockPos> list = new List<BlockPos>();
            list.Add(new BlockPos(cuboidi.MinX, cuboidi.MinY, cuboidi.MinZ));
            list.Add(new BlockPos(cuboidi.MaxX, cuboidi.MaxY, cuboidi.MaxZ));

            api.World.HighlightBlocks(api.World.Player, 1, list, [OwnerNameToColor("Sev")], EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube, 1f);
        }

        public static int OwnerNameToColor(string input)
        {
            uint num = 2166136261U;
            uint num2 = 2166136261U;
            uint num3 = 2166136261U;
            foreach (byte b in input)
            {
                num *= 328514948U;
                num ^= (uint)b;
                num2 *= 221669321U;
                num2 ^= (uint)b;
                num3 *= 301287251U;
                num3 ^= (uint)b;
            }
            return ColorUtil.ToRgba(150, (int)(num % 256U), (int)(num2 % 256U), (int)(num3 % 256U));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("Owner", Owner);
            tree.SetString("OwnerName", OwnerName);
            tree.SetInt("RegisterID", RegisterID);
            tree.SetBool("isAdminShop", IsAdminShop);
            if (Inventory != null)
            {
                ITreeAttribute treeAttribute = new TreeAttribute();
                Inventory.ToTreeAttributes(treeAttribute);
                tree["inventory"] = treeAttribute;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");
            RegisterID = tree.GetInt("RegisterID");
            IsAdminShop = tree.GetBool("isAdminShop");
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        }

        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel) {
            OpenShopForPlayer(byPlayer);
            return true;
        }

        protected virtual void OpenShopForPlayer(IPlayer byPlayer)
        {

            if (Api.Side == EnumAppSide.Server)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(OwnerName == null ? "" : OwnerName);
                    TreeAttribute tree = new TreeAttribute();
                    Inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.OPEN_GUI, data);

                //if (byPlayer.PlayerUID == this.Owner)
                    //byPlayer.InventoryManager.OpenInventory(Inventory);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;
            if (packetid == VinConstants.TOGGLE_GUI)
            {
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

            }
            if (packetid == VinConstants.OPEN_GUI)
            {
                OpenShopGui(data);
            }
            if (packetid == VinConstants.CLOSE_GUI)
            {
                CloseGui(clientWorld);
            }
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {
                case VinConstants.CLOSE_GUI:
                    inventoryManager?.CloseInventory(this.Inventory);
                    break;

                case VinConstants.PURCHASE_ITEMS:
                case VinConstants.SET_ITEMS_PER_PURCHASE:
                case VinConstants.SET_REGISTER_ID:
                case VinConstants.SET_ADMIN_SHOP:
                case VinConstants.SET_ITEM_PRICE:
                case VinConstants.SET_PURCHASES_REMAINING:
                case VinConstants.SET_REGISTER_FALLBACK:
                case VinConstants.SET_LIMITED_PURCHASES:
                case VinConstants.SET_ADMIN_DISCARD_CURRENCY:
                default:
                    if (packetid < 1000)
                    {
                        /*
                        if (!CanAccess(player))
                        {
                            if (!((ICoreServerAPI)Api).Server.IsDedicated)
                            {
                                VinconomyCoreSystem.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.");
                            }
                            else
                            {
                                ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Stall they didn't own)");
                            }
                            return;
                        }
                        */

                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                        return;
                    }
                    break;
            }
        }

        protected virtual void OpenShopGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            string dialogTitle;
            bool isOwner = true;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                string name = reader.ReadString();
                if (name.Length > 0)
                {
                    dialogTitle = Lang.Get("vinconomy:gui-stall-owner", [name]);
                }
                else
                {
                    dialogTitle = Lang.Get("vinconomy:gui-stall-unowned");
                }
                tree.FromBytes(reader);
            }



            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            if (isOwner && !VinconomyCoreSystem.ShouldForceCustomerScreen)
                this.invDialog = GetOwnerGui(dialogTitle, isOwner, 0);
            else
                this.invDialog = GetCustomerGui(dialogTitle, 0);
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;
                ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos, VinConstants.CLOSE_GUI, null);
            };
        }
        private GuiDialogBlockEntity GetCustomerGui(string dialogTitle, int stallSelection)
        {
            return new GuiVinconResidence("TEST", Inventory, this.Pos, (ICoreClientAPI)Api);
        }

        private GuiDialogBlockEntity GetOwnerGui(string dialogTitle, bool isOwner, int stallSelection)
        {
            return new GuiVinconResidence("TEST", Inventory, this.Pos, (ICoreClientAPI)Api);
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



    }
}
