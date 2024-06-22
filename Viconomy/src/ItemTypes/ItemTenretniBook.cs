using System.Text;
using System.Threading.Tasks;
using Viconomy.GUI;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Viconomy.ItemTypes
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
                    ViconomyCoreSystem modSystem = api.ModLoader.GetModSystem<ViconomyCoreSystem>();

                    string baseUrl = attrs.GetString("BaseURL");
                    string ID = attrs.GetString("ID");
                    task = VinUtils.GetAsync(baseUrl + ID, postHandlder);
                } else
                {
                    GuiViconTenretniUnwritten gui = new GuiViconTenretniUnwritten("Blank Tenretni Book", (ICoreClientAPI) api);
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
                GuiViconTenretniWritten gui = new GuiViconTenretniWritten("Tenretni Book", args.Response, (ICoreClientAPI)api);
                gui.TryOpen();
                task = null;
            }, "HttpRequestCallback");
           
        }

        
    }
}
