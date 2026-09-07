using Commercially.Common.Interfaces;
using Commercially.Common.Registry;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
using Commercially.Vinconomy.Inventory.StallSlots;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI
{
    public class OwnableRegistryKeys
    {
        public string[] ShopNames;
        public string[] ShopKeys;
        public int CurrentSelectedIndex;
    }

    public static class GUIUtils
    {

        public static OwnableRegistryKeys GetOwnableDropdownListForOwner(IOwnableRegistry registry, IOwnableChild ownable)
        {
            OwnableRegistration[] ownables = registry.GetOwnablesForOwner(ownable.OwnerUID, ownable.GetAllowedParentTypes());
            int shopLength = ownables.Length;
            string[] shopsNames = new string[shopLength + 1];
            string[] shopsKeys = new string[shopLength + 1];

            OwnableRegistryKeys keys = new OwnableRegistryKeys();


            shopsNames[0] = Lang.Get("vinconomy:gui-none"); //"( None )";
            shopsKeys[0] = "-1";

            for (int i = 0; i < shopLength; i++)
            {
                shopsNames[i + 1] = ownables[i].Name ?? "Generic Shop";
                shopsKeys[i + 1] = ownables[i].ID.ToString();
                if (ownables[i].ID == ownable.ParentID)
                {
                    keys.CurrentSelectedIndex = i + 1;
                }
            }

            keys.ShopNames = shopsNames;
            keys.ShopKeys = shopsKeys;

            return keys;
        }

        public static int[] GetSlotIDsForStall(IStallInventoryProvider stallProvider, int stallSlot)
        {
            // Figure out the slot indexes for SlotGrid
            BaseStallSlot stall = stallProvider.GetStallSlot(stallSlot);

            int[] slotGridIDs = new int[stall.StockSlotCount];
            int stallSlotOffset = stall.Inventory.InternalSlots.Length;
            for (int i = 0; i < stallSlot; i++)
            {
                stallSlotOffset += stallProvider.GetStallSlot(i).TotalItemSlots;
            }


            for (int i = 0; i < slotGridIDs.Length; i++)
            {
                slotGridIDs[i] = stallSlotOffset + stall.InternalSlotCount + i;
            }

            return slotGridIDs;
        }

        public static int GetOffsetForStall(IStallInventoryProvider stallProvider, int stallSlot)
        {
            VinconBaseInventory inv = stallProvider.Inventory as VinconBaseInventory;

            int offset = inv.InternalSlots?.Length ?? 0;
            for (int i = 0; i < stallSlot; i++)
            {
                offset += inv.GetStall(i).TotalItemSlots;
            }
            return offset;
        }

        public static int GetCurrencySlotIdForStall(IStallInventoryProvider stallProvider, int stallSlot)
        {
            int offset = GetOffsetForStall(stallProvider, stallSlot);
            return offset;
        }
        public static int GetProductSlotIdForStall(IStallInventoryProvider stallProvider, int stallSlot)
        {
            int offset = GetOffsetForStall(stallProvider, stallSlot);
            return offset+1;
        }

        public static int GetInternalOffsetForStall(IStallInventoryProvider stallProvider, int stallSlot)
        {
            int offset = GetOffsetForStall(stallProvider, stallSlot);
            return offset + 2;
        }

        public static int GetProductOffsetForStall(IStallInventoryProvider stallProvider, int stallSlot)
        {
            int offset = GetOffsetForStall(stallProvider, stallSlot);
            return offset + stallProvider.GetStallSlot(stallSlot).InternalSlotCount;
        }

        public static bool IsCreativePlayer(IPlayer player)
        {
            return player.WorldData.CurrentGameMode == EnumGameMode.Creative && player.HasPrivilege("gamemode");
        }

        public static int[] GetArrayRange(int startingNumber, int length)
        {
            int[] result = new int[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = startingNumber + i;
            }
            return result;
        }
    }
}
