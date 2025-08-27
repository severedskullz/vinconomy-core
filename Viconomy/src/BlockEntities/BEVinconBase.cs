using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities.TextureSwappable;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Renderer;
using Viconomy.Trading;
using Viconomy.Trading.TradeHandlers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.BlockEntities
{
    public abstract class BEVinconBase : BETextureSwappableBlockDisplay, IOwnableStall
    {
        protected VinconomyCoreSystem modSystem;
        protected GuiDialogBlockEntity invDialog;

        public string Owner { get; protected set; }
        public string OwnerName { get; protected set; }
        public int RegisterID { get; protected set; } = -1;
        public bool IsAdminShop { get; protected set; }
        public bool DiscardProduct { get; protected set; }

        public override string InventoryClassName { get { return "VinconomyInventory"; } }

        public virtual int StallSlotCount => 4;
        public virtual int ProductStacksPerSlot => 9;
        public virtual int BulkPurchaseAmount => 5;

        public bool shouldRenderInventory;
        protected DistanceRenderer distanceRenderer;
        

        //protected AssetLocation OpenSound;
        //protected AssetLocation CloseSound;

        public override void Initialize(ICoreAPI api)
        {
            modSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            //this.block = api.World.BlockAccessor.GetBlock(this.Pos);
            base.Initialize(api);

            if (Inventory is IStallSlotUpdater && api.Side == EnumAppSide.Server)
            {
                Inventory.SlotModified += UpdateStallForSlot;
            }

            /*
            JsonObject attributes = block.Attributes;
            string openSound = null;
            if (attributes != null && attributes["openSound"] != null)
            {
                openSound = attributes["openSound"].AsString(null);
            }

            string closeSound = null;
            if (attributes != null && attributes["openSound"] != null)
            {
                closeSound = attributes["openSound"].AsString(null); ;
            }

            this.OpenSound = (openSound == null) ? null : AssetLocation.Create(openSound, block.Code.Domain);
            this.CloseSound = (closeSound == null) ? null : AssetLocation.Create(closeSound, block.Code.Domain);
            */
            if (api.Side == EnumAppSide.Client)
            {
                this.distanceRenderer = new DistanceRenderer(this);
            }
        }

        protected virtual void UpdateStallForSlot(int index)
        {
            IStallSlotUpdater inv = (Inventory as  IStallSlotUpdater);
            if (inv != null)
            {
                modSystem.Mod.Logger.Debug("Got a product slot update for slot: " + index);
                int stallSlot = inv.GetStallForSlot(index);
                ItemSlot[] slots = inv.GetSlotsForStallSlot(stallSlot);

                ItemStack product = null;
                foreach (var item in slots)
                {
                    if (item.Itemstack != null)
                    {
                        if (product == null)
                        {
                            product = item.Itemstack.Clone();
                        } else
                        {
                            product.StackSize += item.StackSize;
                        }

                    }
                }

                ItemStack currency = inv.GetCurrencyForStallSlot(stallSlot).Itemstack;
                modSystem.UpdateStallProductForStall(this.RegisterID, this.Pos, stallSlot,  product, GetNumItemsPerPurchaseForStall(stallSlot), currency);
            }
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

            IStallSlotUpdater inv = Inventory as IStallSlotUpdater;
            if (inv != null && Api.Side == EnumAppSide.Server)
            {
                int slots = inv.GetStallSlotCount();
                for (int i = 0; i < slots; i++)
                {
                    int index = Inventory.GetSlotId(inv.GetCurrencyForStallSlot(i));
                    if (index >= 0)
                    {
                        UpdateStallForSlot(index);
                    }
                }
            }
        }

        public void SetIsAdminShop(bool adminShop)
        {
            IsAdminShop = adminShop;
        }

        public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);

        public virtual int GetStallSlotForSelectionIndex(int index)
        {
            return index;
        }

        public abstract ItemSlot[] GetSlotsForStall(int stallSlot);
        
        public abstract ItemSlot GetCurrencyForStall(int stallSlot);
        
        public abstract int GetNumItemsPerPurchaseForStall(int stallSlot);

        public virtual ItemStack FindFirstNonEmptyStockStack(int stallSlot)
        {
            ItemSlot[] slots = GetSlotsForStall(stallSlot);
            foreach (ItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                    return slot.Itemstack;
            }
            return null;
        }

        protected virtual void SetAdminShop(IPlayer byPlayer, bool isAdmin)
        {
            // No, this shouldnt be CanAccess(byPlayer) because we dont want admins accidentally turning player stalls into admin shops
            // even if they were given access...
            if (byPlayer.PlayerUID != this.Owner)
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.NO_PRIVLEGE, new object[] { });
                return;
            }

            this.IsAdminShop = isAdmin;

            //PrintClientMessage(byPlayer, "set Admin Shop to " + this.isAdminShip);
            this.MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("Owner", Owner);
            tree.SetString("OwnerName", OwnerName);
            tree.SetInt("RegisterID", RegisterID);
            tree.SetBool("isAdminShop", IsAdminShop);
            tree.SetBool("DiscardProduct", DiscardProduct);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");
            RegisterID = tree.GetInt("RegisterID");
            IsAdminShop = tree.GetBool("isAdminShop");
            DiscardProduct = tree.GetBool("DiscardProduct");
        }

        protected void SetStallRegisterID(IPlayer byPlayer, byte[] data)
        {
            //Only the owner can change the register! Not any joint ownership players
            if (byPlayer.PlayerUID != this.Owner)
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                SetRegisterID(reader.ReadInt32());
            }

            //PrintClientMessage(byPlayer, "set ID to " + this.RegisterID);
            this.MarkDirty();
        }

        public void SetNowTesselatingObj(CollectibleObject collectible)
        {
            this.nowTesselatingObj = collectible;
        }

        public void SetNowTesselatingShape(Shape shape)
        {
            this.nowTesselatingShape = shape;
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

                this.invDialog?.Dispose();
                this.invDialog = null;
            }

        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (this.invDialog != null)
            {
                if (this.invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

            }

            this.invDialog?.Dispose();
            this.invDialog = null;
        }

        protected virtual AggregatedSlots GetAggregateProductSlots(int stallSlot)
        {
            AggregatedSlots slots = new AggregatedSlots();
            foreach (ItemSlot slot in GetSlotsForStall(stallSlot))
            {
                slots.Add(slot);
            }
            return slots;
        }

        public virtual void PurchaseItem(IPlayer player, int stallSlot, int numPurchases, BEVinconRegister shopRegister)
        {
            GenericTradeRequest request = new GenericTradeRequest(Api, player);

            ItemStack currencyStack = GetCurrencyForStall(stallSlot).Itemstack;
            request.WithShop(shopRegister, this, stallSlot, IsAdminShop);
            request.WithPurchases(numPurchases);
            request.WithCurrency(currencyStack, TradingUtil.GetAllValidSlotsFor(player, currencyStack), currencyStack.StackSize);
            request.WithProduct(TradingUtil.GetItemStackClone(FindFirstNonEmptyStockStack(stallSlot), 1), GetAggregateProductSlots(stallSlot), GetNumItemsPerPurchaseForStall(stallSlot));

            AggregatedSlots coupons = TradingUtil.GetCouponsSlotsFor(player, request.ProductStackNeeded, shopRegister);
            if (coupons.Slots.Count > 0)
            {
                request.WithCoupons(coupons.Slots[0]);
            }
            
            //request.WithTools(null, 1);
            if (shopRegister != null)
            {
                ItemStack tradePass = shopRegister.Inventory[0].Itemstack;
                if (tradePass != null)
                {
                    request.WithTradePass(tradePass, TradingUtil.GetAllValidSlotsFor(player, tradePass));
                }
            }


            request.Build();

            GenericTradeResult result = GenericTradeHandler.TryPurchaseItem(request);
            if (result.Error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.Error);
            } else
            {
                MarkDirty(true, null);
                UpdateMeshes();
            }
        }

        public virtual ItemStack GetProductForStall(int stallSlot)
        {
            return TradingUtil.GetItemStackClone(FindFirstNonEmptyStockStack(stallSlot), GetNumItemsPerPurchaseForStall(stallSlot)); ;
        }



        /// <summary>
        /// Consumes any tools needed to perform this trade. For Durability-based tools, it should subtract durability from the tool.
        /// For container-based tools like bowls, pots, or buckets, it should add it to the contents of that blockitem.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual bool ConsumeTool(GenericTradeResult result)
        {
            return true;
        }

        public bool CanAccess(IPlayer player)
        {
            if (player.PlayerUID == this.Owner)
                return true;

            ShopRegistration reg = modSystem.GetRegistry().GetShop(RegisterID);
            if (reg == null)
            {
                modSystem.Mod.Logger.Error("Couldnt find shop registration for register " + RegisterID);
                return false;
            }
            return reg.StallPermissions && reg.CanAccess(player);
        }

        public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(BlockSelection selection, IPlayer forPlayer)
        {
            List<WorldInteraction> interactions = new List<WorldInteraction>();
           
                int index = GetStallSlotForSelectionIndex(selection.SelectionBoxIndex);
                ItemStack product = FindFirstNonEmptyStockStack(index);
                ItemSlot currency = GetCurrencyForStall(index);

                //StallSlot slot = slots[selection.SelectionBoxIndex];

                if (Owner != forPlayer.PlayerUID || VinconomyCoreSystem.ShouldForceCustomerScreen)
                {
                    if (currency.Itemstack != null && product != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = [currency.Itemstack]

                        });

                        ItemStack fiveStack = currency.Itemstack.Clone();
                        fiveStack.StackSize = 5 * fiveStack.StackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase-bulk",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = ["sneak", "sprint"],
                            Itemstacks = [fiveStack]
                        });
                    }
                }
                else
                {
                    ItemStack firstSlot = product;
                    if (firstSlot != null)
                    {
                        ItemStack helpSlot = firstSlot.Clone();
                        helpSlot.StackSize = 1;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = [helpSlot]
                        });

                        ItemStack helpSlotStack = helpSlot.Clone();
                        helpSlotStack.StackSize = helpSlotStack.Collectible.MaxStackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = ["sneak", "sprint"],
                            Itemstacks = [helpSlotStack]
                        });

                        if (currency.Itemstack != null)
                        {
                            interactions.Add(new WorldInteraction
                            {
                                ActionLangCode = "vinconomy:stall-purchase",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCode = "sneak",
                                Itemstacks = [currency.Itemstack]

                            });

                            ItemStack fiveStack = currency.Itemstack.Clone();
                            fiveStack.StackSize = 5 * fiveStack.StackSize;
                            interactions.Add(new WorldInteraction
                            {
                                ActionLangCode = "vinconomy:stall-purchase-bulk",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCodes = ["sneak", "sprint"],
                                Itemstacks = [fiveStack]
                            });
                        }
                    }
                    else
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak"
                        });
                    }
                }

                interactions.Add(new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-open-menu",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                });

            return interactions.ToArray();
        }

    }
}
