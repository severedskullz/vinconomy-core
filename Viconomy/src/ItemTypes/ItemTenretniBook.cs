using System.Text;
using System.Threading.Tasks;
using Vinconomy.GUI;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vinconomy.ItemTypes
{
    public class ItemTenretniBook : Item
    {
        Task task;
        //GuiDialogGeneric ledgerGUI;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
          
            if (this.api.Side == EnumAppSide.Client && task == null || task.IsCompleted)
            {
               ITreeAttribute attrs = slot.Itemstack.Attributes;
                if (attrs.GetString("Archive") != null)
                {
                    VinconomyCoreSystem modSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();

                    string baseUrl = attrs.GetString("BaseURL");
                    string ID = attrs.GetString("ID");
                    task = VinUtils.GetAsync(baseUrl + ID, postHandlder);
                } else
                {
                    GuiVinconTenretniUnwritten gui = new GuiVinconTenretniUnwritten("Blank Tenretni Book", (ICoreClientAPI) api);
                    gui.TryOpen();
                }
               
                
                
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            return itemStack.Attributes.GetString("Name", "Tenretni Book");
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {

            ITreeAttribute attrs = inSlot.Itemstack.Attributes;
            if (attrs.GetString("Archive") != null) {
                dsc.AppendLine("Archive:" + attrs.GetString("Archive", "Unknown"));
                dsc.AppendLine("Databank Address: " + attrs.GetString("ID", "Unknown"));
                dsc.AppendLine("Scribe: " + attrs.GetString("Scribe", "Unknown"));

            }

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        private void postHandlder(CompletedArgs args)
        {
            api.Event.EnqueueMainThreadTask(() => { //Console.WriteLine(args.Response.ToString());
                GuiVinconTenretniWritten gui = new GuiVinconTenretniWritten("Tenretni Book", args.Response, (ICoreClientAPI)api);
                gui.TryOpen();
                task = null;
            }, "HttpRequestCallback");
           
        }

        
    }
}
