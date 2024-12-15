using System;
using System.IO;
using System.Numerics;
using System.Threading;
using Viconomy.GUI;
using Viconomy.Inventory;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconGachaLoader : BlockEntityContainer, IDisposable
    {
        
        protected GuiDialogBlockEntity invDialog;
        protected InventoryGeneric inventory;
        public int[] ItemsPerSlot { get; private set; }
        public string GachaName { get; private set; }

        public override InventoryBase Inventory { get { return this.inventory; } }

        public override string InventoryClassName => "GachaLoaderInventory";

        public BEVinconGachaLoader()
        {
            this.inventory = new InventoryGeneric(8, null, Api, onNewSlot);
            this.ItemsPerSlot = new int[6];
            this.GachaName = Lang.Get("vinconomy:gui-gacha-ball");
        }

        private ItemSlot onNewSlot(int slotId, InventoryGeneric self)
        {
            if (slotId == 0)
            {
                return new ItemSlotOutput(self);
            } else if (slotId == 1) {
                ViconItemSlot slot = new ViconItemSlot(self, 0, slotId);
                slot.setFilter(Filters.ViconomyFilters.IsEmptyGachaSlot);
                slot.BackgroundIcon = "vicon-general2";
                return slot;
            } else
            {
                return new ViconItemSlot(self, 0, slotId);
            }
        }



        #region GUI

        private void OpenGuiForPlayer(IPlayer byPlayer)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.OPEN_GUI, data);


                byPlayer.InventoryManager.OpenInventory(this.inventory);
            }
        }

        private void OpenGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            
            this.invDialog = new GuiViconGachaPress(Lang.Get("vinconomy:gui-gacha-loader"), this.Inventory, this.Pos, this.Api as ICoreClientAPI);
           
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;
                ((ICoreClientAPI)this.Api).Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.CLOSE_GUI, null);
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

                case VinConstants.SET_ITEMS_PER_PURCHASE:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        //string name = reader.ReadString();
                        int index = reader.ReadInt32();
                        int amount = reader.ReadInt32();
                        //Console.WriteLine($"Got ID {index} with value of {amount}");
                        this.ItemsPerSlot[index] = amount;
                        MarkDirty();
                    }
                    break;

                case VinConstants.SET_SCULPTURE_NAME:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        GachaName = reader.ReadString();
                        MarkDirty();
                    }
                    break;

                case VinConstants.PURCHASE_ITEMS:
                    using (MemoryStream ms = new MemoryStream( data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int amount = reader.ReadInt32();
                        TryBundleItems(player, amount);
                    }

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

        private const int OUTPUT_SLOT = 0;
        private const int GACHA_SLOT = 1;
        public void TryBundleItems(IPlayer player, int amount)
        {

            if (Inventory[GACHA_SLOT].StackSize <= 0) {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_GACHA_BALL, null);
                return;
            }

            int maxAmount = amount;
            // Update 999 with current stack size of the Gacha Balls, if we are trying to bundle everything. Otherwise, this will be 1.
            if (Inventory[GACHA_SLOT].StackSize <= maxAmount)
            {
                maxAmount = Inventory[GACHA_SLOT].StackSize;
            }

            bool bundlesAlteastOneItem = false;
            for (int i = 0; i < 6; i++)
            {
                if (this.ItemsPerSlot[i] > 0)
                {
                    bundlesAlteastOneItem = true;
                }

                ItemSlot slot = Inventory[i + 2];
                int stackSize = slot.StackSize;
                //Stack size MUST be 0 if we want to ignore. This is to prevent players from using up all stock in 1 click, and miss inserting items in the next.
                if (stackSize < this.ItemsPerSlot[i])
                {
                    VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_ENOUGH_STOCK, null);
                    return;
                }

                // Requested: 999
                // Current Max: 10
                // Items Per: 5
                // Slot Stack Size 41
                // 41 / 5 = 8, which is less than 10. So update...

                if (this.ItemsPerSlot[i] != 0 && stackSize / this.ItemsPerSlot[i] < maxAmount)
                {
                    maxAmount = stackSize / this.ItemsPerSlot[i];
                }
            }

            if (!bundlesAlteastOneItem)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.GACHA_ATLEAST_ONE, null);
                return;
            }


            if (Inventory[GACHA_SLOT].StackSize <= 0)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.GACHA_BUNDLED_ZERO, null);
                return;
            }

            ItemStack gacha = Inventory[GACHA_SLOT].TakeOut(maxAmount);
            gacha.StackSize = maxAmount;

            gacha.Attributes.SetString("Name", GachaName);
            ITreeAttribute attr = gacha.Attributes.GetOrAddTreeAttribute("Contents");
            for (int i = 0; i < 6; i++)
            {
                if (ItemsPerSlot[i] > 0) { 
                    ItemStack stack = Inventory[i + 2].TakeOut(maxAmount * ItemsPerSlot[i]);
                    stack.StackSize = ItemsPerSlot[i];
                    attr.SetItemstack("Item" + i, stack);
                    attr.SetInt("ItemAmount" + i, stack.StackSize);
                }
            }

            ItemSlot outputSlot = Inventory[OUTPUT_SLOT];


            if (outputSlot.StackSize == 0)
            {
                Inventory[OUTPUT_SLOT].Itemstack = gacha;
            } else if (outputSlot.Itemstack.Equals(this.Api.World, gacha)) {
                outputSlot.Itemstack.StackSize += maxAmount;
            } else
            {
                gacha.StackSize = maxAmount;
                //Console.WriteLine("Should have spawned item for " + String.Format("{0}-{1}-{2}", x, y, z));
                this.Api.World.SpawnItemEntity(gacha, this.Pos.Add(0.0f, 0.5f, 0.0f).ToVec3d(), null);
            }
            
            MarkDirty();

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
                    OpenGui(data);
                }

            }
            if (packetid == VinConstants.OPEN_GUI)
            {
                OpenGui(data);
            }
            if (packetid == VinConstants.CLOSE_GUI)
            {
                CloseGui(clientWorld);
            }
        }

       

        #endregion
      
      
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            if (this.inventory[0].Itemstack == null)
            {
                return false;
            }
            //mesher.AddMeshData(this.meshes[this.inventory[0].StackSize], 1);

            return false;
        }

      


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.invDialog != null)
            {
                if (invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

                //this.invDialog.Dispose(); // Got a Null Pointer when I had this closed the game when menu was open... how is invDialog null here?
                this.invDialog = null;
            }

        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            GuiDialogBlockEntity guiDialogBlockEntity = this.invDialog;
            if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
            {
                GuiDialogBlockEntity guiDialogBlockEntity2 = this.invDialog;
                if (guiDialogBlockEntity2 != null)
                {
                    guiDialogBlockEntity2.TryClose();
                }
            }
            GuiDialogBlockEntity guiDialogBlockEntity3 = this.invDialog;
            if (guiDialogBlockEntity3 == null)
            {
                return;
            }
            guiDialogBlockEntity3.Dispose();
        }

        public void Dispose()
        {
            invDialog.Dispose();
        }

        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                OpenGuiForPlayer(byPlayer);
            }
            return true;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("GachaName", GachaName);
            tree.SetInt("Amount1", ItemsPerSlot[0]);
            tree.SetInt("Amount2", ItemsPerSlot[1]);
            tree.SetInt("Amount3", ItemsPerSlot[2]);
            tree.SetInt("Amount4", ItemsPerSlot[3]);
            tree.SetInt("Amount5", ItemsPerSlot[4]);
            tree.SetInt("Amount6", ItemsPerSlot[5]);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            GachaName = tree.GetString("GachaName", "Gacha Ball");
            ItemsPerSlot[0] = tree.GetInt("Amount1", 0);
            ItemsPerSlot[1] = tree.GetInt("Amount2", 0);
            ItemsPerSlot[2] = tree.GetInt("Amount3", 0);
            ItemsPerSlot[3] = tree.GetInt("Amount4", 0);
            ItemsPerSlot[4] = tree.GetInt("Amount5", 0);
            ItemsPerSlot[5] = tree.GetInt("Amount6", 0);
        }
    }

}
