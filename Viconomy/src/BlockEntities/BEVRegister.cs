using System.IO;
using Viconomy.BlockTypes;
using Viconomy.Delegates;
using Viconomy.GUI;
using Viconomy.ItemTypes;
using Viconomy.Registry;
using Viconomy.src.GUI;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVRegister : BlockEntityContainer
    {
        private BlockVRegister block;
        private InventoryGeneric inventory;
        private GuiViconRegister invDialog;
        private GuiViconWaypoint waypointDialogue;
        private ViconomyCoreSystem modSystem;

        public int ID { get; internal set; } = -1;
        public string Owner { get; internal set; }
        public string OwnerName { get; internal set; }

        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "ViconomyRegisteryInventory";

        public BEVRegister()
        {
            this.inventory = new InventoryGeneric(25, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            block = (BlockVRegister)api.World.BlockAccessor.GetBlock(this.Pos);
            modSystem = Api.ModLoader.GetModSystem<ViconomyCoreSystem>();


        }

        public void UpdateShop(string Owner, string OwnerName, int ID, string Name)
        {          
            if (Api.Side == EnumAppSide.Server)
            {
                if (ID <= 0)
                {
                    //Console.WriteLine("Register block was placed and did not have ID Set...");
                    ShopRegistration register = modSystem.AddShop(Owner, OwnerName, OwnerName + "'s Shop", this.Pos);

                    ID = register.ID;
                }
                else
                {
                    ShopRegistration reg = modSystem.GetRegistry().GetShop(ID);
                    if (reg != null)
                    {
                        //Console.WriteLine("Register block was placed and had ID Set... Updating");
                        ShopRegistration register = modSystem.UpdateShop(Owner, ID, Name, this.Pos);
                    } else {
                        modSystem.Mod.Logger.Warning("Somehow the shop with ID " + ID + " got removed. Recreating it...");
                        ShopRegistration register = modSystem.AddShop(Owner, OwnerName, Name, this.Pos);
                        ID = register.ID;
                    }
                    
                }
            }

            this.ID = ID;
            this.Owner = Owner;
            this.OwnerName = OwnerName;
            
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            modSystem.GetRegistry().ClearShopPos(ID);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            ID = tree.GetInt("ID");
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("ID", ID);
            tree.SetString("Owner", Owner);
            tree.SetString("OwnerName", OwnerName);
        }

        public bool CanHold(ItemStack sourceStack)
        {
            int amountLeft = sourceStack.StackSize;
            foreach (ItemSlot slot in inventory)
            {
                if (slot.Itemstack == null)
                {
                    return true;
                } else if (slot.Itemstack.Equals(sourceStack)) {
                    // 64 - 10 = 54
                    amountLeft -= slot.Itemstack.Item.MaxStackSize - slot.Itemstack.StackSize;
                    if (amountLeft <= 0)
                    {
                        return true;
                    }

                }
            }
            return false;
        }

        public bool AddItem(ItemStack sourceStack, int quantity)
        {
            ItemSlot dslot = new ItemSlot(null);
            dslot.Itemstack = sourceStack;

            int amountLeft = quantity;
            foreach (ItemSlot slot in inventory)
            {
                if (slot.CanHold(dslot))
                {
                    amountLeft -= dslot.TryPutInto(this.Api.World, slot, amountLeft);
                }
              
                if (amountLeft <= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (shiftMod && handSlot.Itemstack?.Item?.Code.ToString() == "vinconomy:ledger")
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    IServerPlayer player = ((IServerPlayer)byPlayer);
                    if (player.PlayerUID == Owner)
                    {
                        if (handSlot.Itemstack.Attributes.GetInt("ShopId", -1) <= 0)
                        {
                            handSlot.Itemstack.Attributes.SetInt("ShopId", ID);
                            handSlot.Itemstack.Attributes.SetString("Owner", Owner);
                            handSlot.MarkDirty();
                            ViconomyCoreSystem modSystem = Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
                            ShopRegistration shop = modSystem.GetRegistry().GetShop(ID);
                            player.SendMessage(0, Lang.Get("vinconomy:ledger-set", new object[] { shop.Name }), EnumChatType.OwnMessage);
                        }
                        else
                        {
                            player.SendMessage(0, Lang.Get("vinconomy:ledger-already-set", new object[0]), EnumChatType.OwnMessage);
                        }
                    }
                    else
                    {
                        player.SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.OwnMessage);
                    }
                }
            }
            else if (byPlayer.PlayerUID == Owner)
            {
                // Open shop admin gui
                OpenAdminForPlayer(byPlayer);
            }
            else
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.OwnMessage);
                }
               
            }

            return true;
        }

        private void OpenAdminForPlayer(IPlayer byPlayer)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                ShopRegistration register = modSystem.GetRegistry().GetShop(ID);

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write("VinconomyInventory");
                    if (register != null && register.Name != null)
                    {
                        writer.Write(register.Name);
                    } else
                    {
                        writer.Write((OwnerName == null ? "Unowned" : OwnerName + "'s") + " Shop");
                    }
                    
                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
              ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.TOGGLE_GUI, data);
                byPlayer.InventoryManager.OpenInventory(this.inventory);
            }
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {
                case VinConstants.OPEN_GUI:

                    //Todo: Check if owner before we open the inventory?
                    if (inventoryManager == null)
                    {
                        return;
                    }
                    inventoryManager.OpenInventory(this.Inventory);
                    break;

                case VinConstants.CLOSE_GUI:
                    //Todo: Check if owner before we open the inventory?
                    if (inventoryManager != null)
                    {
                        inventoryManager.CloseInventory(this.Inventory);
                    }
                    break;
                case VinConstants.SET_SHOP_NAME:
                    using (MemoryStream ms = new MemoryStream(data))
                    {

                        if (player.PlayerUID == Owner)
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            string Name = reader.ReadString();
                            UpdateShop(Owner, OwnerName, ID, Name);
                        }
                        else
                        {
                            ((IServerPlayer)player).SendMessage(0, Lang.Get("viconomy:doesnt-own", new object[0]), EnumChatType.OwnMessage);
                        }
                    }
                    break;
                case VinConstants.SET_WAYPOINT:
                    using (MemoryStream ms = new MemoryStream(data))
                    {

                        if (player.PlayerUID == Owner)
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            bool enabled = reader.ReadBoolean();
                            string icon = reader.ReadString();
                            int color = reader.ReadInt32();
                            modSystem.UpdateShopWaypoint(ID, enabled, icon, color);
                        }
                        else
                        {
                            ((IServerPlayer)player).SendMessage(0, Lang.Get("viconomy:doesnt-own", new object[0]), EnumChatType.OwnMessage);
                        }
                    }
                    break;
                default:
                    if (packetid < 1000)
                    {
                        if (player.PlayerUID != Owner)
                        {
                            if (!((ICoreServerAPI)Api).Server.IsDedicated)
                            {
                                ViconomyCoreSystem.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.", new object[] { });
                            }
                            else
                            {
                                ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Register they didn't own)");
                            }
                            return;
                        }

                        //Console.Write("Handling Inv Packet");
                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                        return;
                    }
                    break;
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;
            if (packetid == VinConstants.TOGGLE_GUI)
            {
                if (this.invDialog != null)
                {
                    //Console.WriteLine(Api.Side + ": Toggling GUI OFF");
                    CloseGui(clientWorld);
                }
                else
                {
                    //Console.WriteLine(Api.Side + ": Toggling GUI ON");
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

        private void OpenShopGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            string dialogTitle;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                reader.ReadString();
                dialogTitle = reader.ReadString();
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            this.invDialog = new GuiViconRegister(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI);
            this.waypointDialogue = new GuiViconWaypoint("Waypoint", this.Pos, this.ID, this.Api as ICoreClientAPI);
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;

                ((ClientCoreAPI) Api).Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.CLOSE_GUI, null);
            };
            this.waypointDialogue.TryOpen();
            //Console.WriteLine(Api.Side + ": Attempted to open Shop GUI");
        }

        private void CloseGui(IClientWorldAccessor clientWorld)
        {
            clientWorld.Player.InventoryManager.CloseInventory(this.Inventory);

            if (this.invDialog != null)
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
            //Console.WriteLine(Api.Side + ": Attempted to close GUI");
        }

    }

}