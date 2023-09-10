using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory
{
    public class StallSlot
    {
        public ItemSlot[] slots;
        public ItemSlot currency;
        public int itemsPerPurchase;
        private ViconomyInventory inventory;
        private int numSlots;
        private int stallSlot;

        public StallSlot(ViconomyInventory inventory, int stallSlot, int numSlots, ItemSlot[] rawSlots)
        {
            this.inventory = inventory;
            this.numSlots = numSlots;
            this.stallSlot = stallSlot;

            //this.currency = new ViconCurrencySlot(inventory, stallSlot);
            this.slots = new ViconItemSlot[numSlots];

            int offset = stallSlot * (numSlots + 1);
            for (int i = 0; i < numSlots; i++)
            {
                //slots[i] = new ViconItemSlot(inventory, stallSlot, i);
                this.slots[i] = rawSlots[offset + i];
            }

            this.currency = rawSlots[offset + numSlots];

        }


        public void FromTreeAttributes(ICoreAPI api, int stallSlot, ITreeAttribute tree)
        {
            if (tree == null)
                return;

            numSlots = tree.GetAsInt("qslots");
            slots = new ViconItemSlot[numSlots]; 

            //Set up Stock Slots
            for (int j = 0; j < numSlots; j++)
            {
                slots[j] = new ViconItemSlot(inventory, stallSlot, j);
                ItemStack itemStack = tree.GetTreeAttribute("slots")?.GetItemstack(j.ToString() ?? "");
                slots[j].Itemstack = itemStack;
                if (api?.World == null)
                {
                    continue;
                }

                itemStack?.ResolveBlockOrItem(api.World);
                
            }

            //Set up our Currency
            currency = new ViconCurrencySlot(inventory, stallSlot);
            ItemStack currencyItem = tree.GetItemstack("currency");
            currency.Itemstack = currencyItem;
            if (api?.World != null)
            {
                currencyItem?.ResolveBlockOrItem(api.World); 
            }

            //Set Items Per Purchase
            this.itemsPerPurchase = tree.GetAsInt("itemsPerPurchase");
            

        }

        public void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("qslots", slots.Length);
            TreeAttribute treeAttribute = new TreeAttribute();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Itemstack != null)
                {
                    treeAttribute.SetItemstack(i.ToString() ?? "", slots[i].Itemstack.Clone());
                }

            }
            tree["slots"] = treeAttribute;

            if (currency.Itemstack != null)
            {
                tree.SetItemstack("currency", currency.Itemstack.Clone());
            }

            tree.SetInt("itemsPerPurchase", itemsPerPurchase);
        }
    }
}