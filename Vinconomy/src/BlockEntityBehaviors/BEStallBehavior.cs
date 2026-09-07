using Commercially.Common.Blocks.BlockEntityBehaviors;
using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.ModSystems;
using Commercially.Vinconomy.Trading;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Commercially.Vinconomy.BlockEntityBehaviors
{
    public class BEStallBehavior : BEBehaviorOwnableChild, IStallComponent
    {
        protected VinconomyCoreSystem VinconomyCore;
        protected CommerciallyModSystem CommerciallyCore;

        protected Dictionary<int, int> _StallIndices = [];

        IStallInventoryProvider IStallComponent.InventoryProvider => _InventoryProvider;
        protected IStallInventoryProvider _InventoryProvider;
        IOwnableChild IStallComponent.Ownable => _Ownable;
        protected IOwnableChild _Ownable => this;
        public bool RequiresParent { get; protected set; } = true;
        public override string DefaultLangCode => "vinconomy:default-stall-name";
        public int StallCount => _InventoryProvider?.StallCount ?? 0;

        public BEStallBehavior(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _InventoryProvider = this.GetComponent<IStallInventoryProvider>();
            //_Ownable = this.GetComponent<IOwnableChild>();

            VinconomyCore = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            CommerciallyCore = api.ModLoader.GetModSystem<CommerciallyModSystem>();

            if (properties["stallSelectionIndexes"] != null)
            {
                Dictionary<int, int> stallIndexObj = properties["stallSelectionIndexes"].AsObject<Dictionary<int, int>>();
                _StallIndices = stallIndexObj;
            }

        }

        public ItemStack GetCurrencyForStallSlot(int stallSlot)
        {
            return _InventoryProvider.GetStallSlot(stallSlot).Currency.Itemstack?.Clone();
        }

        public ItemStack GetProductForStallSlot(int stallSlot)
        {
            return _InventoryProvider.GetStallSlot(stallSlot).Product.Itemstack?.Clone();
        }

        public BaseStallSlot GetStallSlot(int stallSlot)
        {
            return _InventoryProvider.GetStallSlot(stallSlot);
        }

        public T GetStallSlot<T>(int stallSlot) where T : BaseStallSlot
        {
            return _InventoryProvider.GetStallSlot<T>(stallSlot);
        }

        public virtual bool CanPurchaseItem(IPlayer player, IShopComponent parent, int stallSlot, int numPurchases)
        {
            if (numPurchases <= 0)
            {
                CommerciallyModSystem.PrintClientMessage(player, TradingConstants.PURCHASED_ZERO);
                return false;
            }

            ItemSlot currency = _InventoryProvider.GetStallSlot(stallSlot).Currency;

            if (currency.Itemstack == null)
            {
                CommerciallyModSystem.PrintClientMessage(player, TradingConstants.NO_PRICE);
                return false;
            }

            if (RequiresParent)
            {
                
                // Does the shop have a Parent ID set if it needs one?
                if (parent == null && !_Ownable.IsAdminOwned)
                {
                    CommerciallyModSystem.PrintClientMessage(player, TradingConstants.NOT_REGISTERED);
                    return false;
                }

                //TODO: Trade Pass? Maybe just handle it in purchase logic
            }


            if (_InventoryProvider.GetStallSlot(stallSlot).GetNumPurchasesRemaining() <= 0)
            {
                CommerciallyModSystem.PrintClientMessage(player, TradingConstants.NO_PRODUCT);
                return false;
            }

            return VinconomyCore.CanPurchaseItem(player, this, parent, stallSlot, numPurchases);
        }


        public virtual bool TryPurchaseItem(IPlayer player, int stallSlot, int numPurchases)
        {
            IShopComponent shop = null;

            if (_Ownable?.ParentID != null && _Ownable?.OwnerUID != null)
                shop = VinconomyCore.GetShop(_Ownable.OwnerUID, _Ownable.ParentID);

            if (CanPurchaseItem(player, shop, stallSlot, numPurchases))
            {
                PurchaseResult result = PurchaseItem(player, stallSlot, numPurchases, shop);
                return result.ErrorMsg != null;
            }

            return false;
        }

        public virtual PurchaseResult PurchaseItem(IPlayer player, int stallSlot, int numPurchases, IShopComponent shopRegister)
        {
            PurchaseRequest request = GetStallSlot(stallSlot).CreatePurchaseRequest(player, numPurchases, shopRegister, this);

            PurchaseResult result = VinconomyCore.TryPurchaseItem(request);
            if (result.ErrorMsg != null)
            {
                CommerciallyModSystem.PrintClientMessage(player, result.ErrorMsg);
            }
            else
            {
                Blockentity.MarkDirty(true, null);
                //Blockentity.UpdateMeshes();
            }

            return result;
        }

        public int GetRemainingProductForStallSlot(int stallSlot)
        {
            return _InventoryProvider.GetStallSlot(stallSlot).GetTotalProductAvailable();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            return TesselateDecoBlock(mesher, tessThreadTesselator);
        }

        protected virtual bool TesselateDecoBlock(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            IDecocratedBlock deco = _InventoryProvider as IDecocratedBlock;
            if (deco != null)
            {
                ItemSlot decoration = deco.GetDecorationSlot();
                if (decoration?.Itemstack != null)
                {
                    MeshData mesh = CommerciallyCore.GetRenderer(decoration).CreateMesh(this, decoration, 0);
                    mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, (float)((Block.Shape.rotateY * Math.PI) / 180), 0);
                    mesher.AddMeshData(mesh);
                    return true;
                }
            }
           
            return false;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            if (Api != null && Api.Side == EnumAppSide.Client)
            {
                this.Blockentity.MarkDirty(true, null);
            }
        }

        /*
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;
            if (packetid == CommerciallyConstants.TOGGLE_GUI)
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
            if (packetid == CommerciallyConstants.OPEN_GUI)
            {
                OpenShopGui(data);
            }
            if (packetid == CommerciallyConstants.CLOSE_GUI)
            {
                CloseGui(clientWorld);
            }
        }
        */

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            int stallSlot;
            int amount;
            switch (packetid)
            {
                case CommerciallyConstants.CLOSE_GUI:
                    player.InventoryManager?.CloseInventory(_InventoryProvider.Inventory);
                    break;

                case CommerciallyConstants.PURCHASE_ITEMS:

                    using (MemoryStream memoryStream = new MemoryStream(data))
                    {
                        BinaryReader binaryReader = new BinaryReader(memoryStream);
                        stallSlot = binaryReader.ReadInt32();
                        amount = binaryReader.ReadInt32();
                    }
                    TryPurchaseItem(player, stallSlot, amount);
                    break;

                case CommerciallyConstants.SET_ITEMS_PER_PURCHASE:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stallSlot = (int)reader.ReadInt32();
                        amount = (int)reader.ReadInt32();
                    }
                    _InventoryProvider.GetStallSlot(stallSlot).ProductPerPurchase = amount;
                    break;

                case CommerciallyConstants.SET_ITEM_PRICE:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stallSlot = reader.ReadInt32();
                        amount = reader.ReadInt32();
                    }
                    _InventoryProvider.GetStallSlot(stallSlot).CurrencyPerPurchase = amount;
                    _InventoryProvider.GetStallSlot(stallSlot).Currency.MarkDirty();

                    break;

                case CommerciallyConstants.SET_PARENT_ID:
                    SetStallRegisterID(player, data);
                    break;

                case CommerciallyConstants.SET_ADMIN_OWNED:
                    bool isAdmin = false;
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        isAdmin = reader.ReadBoolean();
                    }
                    SetAdminShop(player, isAdmin);
                    break;
                case CommerciallyConstants.SET_SHOULD_DISCARD_CURRENCY:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        isAdmin = reader.ReadBoolean();
                    }
                    SetDiscardCurrency(player, isAdmin);
                    break;
                default:
                    base.OnReceivedClientPacket(player, packetid, data);
                    break;
            }
        }




        protected void SetStallRegisterID(IPlayer byPlayer, byte[] data)
        {
            //Only the owner can change the register! Not any joint ownership players
            if (_Ownable != null && !_Ownable.IsOwner(byPlayer))
            {
                CommerciallyModSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                SetParent(reader.ReadInt32());
                UpdateOwnership(OwnerUID, OwnerName, Name, IsAdminOwned);

            }

            //PrintClientMessage(byPlayer, "set ID to " + this.RegisterID);
            Blockentity.MarkDirty();
        }

        protected virtual void SetAdminShop(IPlayer byPlayer, bool isAdmin)
        {
            // No, this shouldnt be CanAccess(byPlayer) because we dont want admins accidentally turning player stalls into admin shops
            // even if they were given access...
            if (_Ownable != null && !_Ownable.IsOwner(byPlayer))
            {
                CommerciallyModSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                CommerciallyModSystem.PrintClientMessage(byPlayer, TradingConstants.NO_PRIVLEGE, new object[] { });
                return;
            }

            _Ownable.SetIsAdminOwned(isAdmin);

            //PrintClientMessage(byPlayer, "set Admin Shop to " + this.isAdminShip);
            Blockentity.MarkDirty();
        }

        public virtual void SetDiscardCurrency(IPlayer byPlayer, bool discard)
        {
            if (_Ownable != null && !_Ownable.IsOwner(byPlayer))
            {
                CommerciallyModSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN);
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                    CommerciallyModSystem.PrintClientMessage(byPlayer, TradingConstants.NO_PRIVLEGE);
                return;
            }

            (_InventoryProvider.Inventory as VinconBaseInventory).DiscardCurrency = discard;
            Blockentity.MarkDirty();

        }

        public int GetStallIndexFromSelection(int selectionIndex)
        {
            if (_StallIndices != null && _StallIndices.TryGetValue(selectionIndex, out int stallIndex))
            {
                return stallIndex;
            }
            return 0;
        }

    }
}
