
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class GachaStallCustomerTab : ModularTab
    {
        public const string CODE = "Vinconomy.GachaStallCustomer";
        public override string Code => CODE;
        public override string TabName => Lang.Get("vinconomy:tabname-generic-customer");

        DummyInventory DisplayInv;
        GachaShopInventory Inventory;
        IStallInventoryProvider StallProvider;
        IOwnableChild Ownable;

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            Inventory = entity?.GetBehavior<IInventoryProvider>()?.Inventory as GachaShopInventory;
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
            Ownable = entity?.GetBehavior<IOwnableChild>();
            DisplayInv = new DummyInventory(Inventory.Api, Inventory.StallSlots.Length+1);
            DisplayInv.TakeLocked = true;
            DisplayInv.PutLocked = true;
            
        }

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null && StallProvider != null && Ownable != null)
            {
                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);

                int desiredWidth = 350;
                int cols = (int)Math.Ceiling(Math.Sqrt(StallProvider.StallCount));
                int colWidth = (int)(cols * (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding));
                int neededPadding = (desiredWidth - colWidth) / cols;

                ElementBounds possibleProductLabel = ElementBounds.FixedSize(desiredWidth, 25).WithFixedOffset(0, GuiStyle.TitleBarHeight + 5);
                rootBounds.WithChild(possibleProductLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-possible-product"), labelTextFont, possibleProductLabel);

                ElementBounds lastUnder = possibleProductLabel;
                ElementBounds lastRightOf = null;

                // Currency Item
                DisplayInv[0].Itemstack = Inventory.InternalSlots[0].Itemstack?.Clone();


                for (int i = 0; i < StallProvider.StallCount; i++)
                {
                    if (i % cols == 0)
                    {
                        lastRightOf = null;
                    }


                    ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1);
                    if (lastUnder != null)
                    {
                        int offset = 20;
                        if (i / cols == 0)
                            offset = 0;
                        itemSlotBounds.FixedUnder(lastUnder,offset);
                    }
                    //If we dont have anything to our left, we want to offset it by half the needed padding to align things in the middle of the "area"
                    // ---[ ]--- where - is the padding and [ ] is the slot/label
                    // if we dont have anything to our left, we want just half so it lines up,
                    // ---[ ]
                    // and then if we have something to our left, we want the full padding amount (Previous right padding + left padding of our new element).
                    // --- ---[ ]
                    // This gives us the following
                    // ---[ ]--- ---[ ]--- ---[ ]---
                    if (lastRightOf != null)
                    {
                        itemSlotBounds.RightOf(lastRightOf, neededPadding);
                    } else
                    {
                        itemSlotBounds.WithFixedOffset(neededPadding/2, 0);
                    }

                    ElementBounds weightLabelBounds = ElementBounds.FixedSize(80, 25).FixedUnder(itemSlotBounds).WithFixedOffset(0, 5);

                    if (lastRightOf != null)
                    {
                        weightLabelBounds.RightOf(lastRightOf, neededPadding- 15);
                    }
                    else
                    {
                        weightLabelBounds.WithFixedOffset((neededPadding / 2)-15, 0);
                    }

                    lastRightOf = itemSlotBounds;

                    //If the next element would be the first in a new row, set lastUnder to the current weightLabelBounds
                    if ((i + 1) % cols == 0)
                    {
                        lastUnder = weightLabelBounds;
                    }
                    rootBounds.WithChildren(itemSlotBounds, weightLabelBounds);
                    DisplayInv[i+1].Itemstack = Inventory.GetStall(i).Product.Itemstack?.Clone();
                    composer.AddItemSlotGrid(DisplayInv, null, 1, new int[] { i +1}, itemSlotBounds);
                    composer.AddDynamicText(Lang.Get("vinconomy:gui-price"), labelTextFont, weightLabelBounds, "weight" + i);


                }


                ElementBounds currencyLabel = ElementBounds.FixedSize(60, 25).FixedUnder(lastUnder).WithFixedOffset(35, 15);
                rootBounds.WithChild(currencyLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-price"), smallText, currencyLabel);

                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel).WithFixedOffset(35, 0);
                rootBounds.WithChild(currencySlotBounds);
                composer.AddItemSlotGrid(DisplayInv, null, 1, new int[] { 0 }, currencySlotBounds);

                ElementBounds purchaseButtonBounds = ElementBounds.FixedSize(120, 40).FixedRightOf(currencySlotBounds).FixedUnder(currencyLabel).WithFixedOffset(40, 0);
                rootBounds.WithChild(purchaseButtonBounds);
                composer.AddButton(Lang.Get("vinconomy:gui-deal"), OnPurchase, purchaseButtonBounds, EnumButtonStyle.Small, "save");

                rootBounds.verticalSizing = ElementSizing.FitToChildren;

                UpdateWeights();


            }
            else if (StallProvider == null)
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 300).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("vinconomy:not-a-shop"), CairoFont.WhiteSmallText(), textBounds);
            }
            else
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 300).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("commercially:container-no-inventory"), CairoFont.WhiteSmallText(), textBounds);
            }
        }

        private void UpdateWeights()
        {
            int totalWeight = Inventory.GetTotalWeights();
            for (int i = 0; i < StallProvider.StallCount; i++)
            {
                int weight = StallProvider.GetStallSlot<GachaStallSlot>(i).GetStallWeight();

                float chance = ((float)weight / (float)totalWeight)*100;
                Gui.Composer.GetDynamicText("weight" + i).SetNewText($"{chance:F2}%");
            }
        }

        private bool OnPurchase()
        {
            //capi.Logger.Chat("Attempting to purchase item from slot " + curTab);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(0);
                writer.Write(1);

                data = ms.ToArray();

                ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.PURCHASE_ITEMS, data);
            }
            this.Gui.FullRecompose();
            return true;
        }

        public override bool IsVisible()
        {
            return true;
        }

        public override void OnGuiClosed()
        {
            
        }

        public override void OnGuiOpened()
        {
            
        }

        public override void OnRecievedData(byte[] data)
        {
            //TODO: Need a way to notify the UI that a purchase was completed and the weights should update
            // for now, we will just recompose the view.
        }

        public override byte[] OnSendData(BlockEntity entity, Caller caller, BlockSelection blockSel, string key)
        {
            return null;
        }

    }
}
