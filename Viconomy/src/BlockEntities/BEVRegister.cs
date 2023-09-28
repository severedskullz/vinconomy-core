using Microsoft.Win32;
using System;
using System.IO;
using System.Numerics;
using Viconomy.BlockTypes;
using Viconomy.GUI;
using Viconomy.Registry;
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

        public string ID { get; internal set; }
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
            this.block = (BlockVRegister)api.World.BlockAccessor.GetBlock(this.Pos);

            base.Initialize(api);          
                       
        }

        public void UpdateRegister(string Owner, string OwnerName, string ID, string Name)
        {
            ViconomyModSystem modSystem = this.Api.ModLoader.GetModSystem<ViconomyModSystem>();

            if (ID == null)
            {
                //Console.WriteLine("Register block was placed and did not have ID Set...");
                ViconRegister register = modSystem.AddRegister(Owner, OwnerName, OwnerName + "'s Shop", this.Pos);

                ID = register.ID;
            }
            else
            {
                //Console.WriteLine("Register block was placed and had ID Set... Updating");
                ViconRegister register = modSystem.UpdateRegister(Owner, ID, Name, this.Pos);
            }

            this.ID = ID;
            this.Owner = Owner;
            this.OwnerName = OwnerName;
            
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            ViconomyModSystem modSystem = this.Api.ModLoader.GetModSystem<ViconomyModSystem>();
            modSystem.registers.ClearRegisterPos(Owner, ID);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            ID = tree.GetString("ID");
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("ID", ID);
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

            if (byPlayer.PlayerUID == Owner)
            {
               
                    // Open shop admin gui
                    OpenAdminForPlayer(byPlayer);
                
            }
            else
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("viconomy:doesnt-own", new object[0]), EnumChatType.OwnMessage);
                }
               
            }

            return true;
        }

        private void OpenAdminForPlayer(IPlayer byPlayer)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                ViconomyModSystem modSystem = Api.ModLoader.GetModSystem<ViconomyModSystem>();
                ViconRegister register = modSystem.GetRegistry().GetRegister(Owner, ID);

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
                case VinConstants.SET_SHOP_NAME:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        string Name = reader.ReadString();
                        if (player.PlayerUID == Owner)
                        {
                            UpdateRegister(Owner, OwnerName, ID, Name);
                        }
                        else
                        {
                            //Console.WriteLine("You do not own this");
                        }
                    }

                    
                    break;
                default:
                    if (packetid < 1000)
                    {
                        //Console.Write("Handling Inv Packet");
                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified();
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
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;

                ((ClientCoreAPI) Api).Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.CLOSE_GUI, null);
            };
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