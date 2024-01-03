using Cairo;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Viconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.Map
{
    public class ShopMapLayer : MapLayer
    {
        private ViconomyCoreSystem core;


        // Token: 0x04000684 RID: 1668
        private ICoreServerAPI sapi;


        // Token: 0x04000686 RID: 1670
        private List<MapComponent> wayPointComponents = new List<MapComponent>();

        // Token: 0x04000687 RID: 1671
        public MeshRef quadModel;


        // Token: 0x04000689 RID: 1673
        public Dictionary<string, LoadedTexture> texturesByIcon;

        public OrderedDictionary<string, CreateIconTextureDelegate> WaypointIcons { get; set; } = new OrderedDictionary<string, CreateIconTextureDelegate>();


        public ShopMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            core = api.ModLoader.GetModSystem<ViconomyCoreSystem>();
            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                List<IAsset> many = api.Assets.GetMany("textures/icons/worldmap/", null, false);
                
                using (List<IAsset>.Enumerator enumerator = many.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        IAsset icon = enumerator.Current;
                        string name = icon.Name.Substring(0, icon.Name.IndexOf("."));
                        name = Regex.Replace(name, "\\d+\\-", "");
                      
                        this.WaypointIcons[name] = delegate ()
                        {
                            int size = (int)Math.Ceiling((double)(20f * RuntimeEnv.GUIScale));
                            return capi.Gui.LoadSvg(icon.Location, size, size, size, size, new int?(-1));
                        };
                        capi.Gui.Icons.CustomIcons["wp" + name.UcFirst()] = delegate (Context ctx, int x, int y, float w, float h, double[] rgba)
                        {
                            int col = ColorUtil.ColorFromRgba(rgba);
                            capi.Gui.DrawSvg(icon, ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, new int?(col));
                        };
                        
                    }
                }
                this.quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
            }
        }

        public override bool RequireChunkLoaded
        {
            get
            {
                return false;
            }
        }

        public override string Title => "Shops";

        public override string LayerGroupCode => "VinconomyShops";

        public override EnumMapAppSide DataSide
        {
            get
            {
                return EnumMapAppSide.Client;
            }
        }

        public override void Render(GuiElementMap map, float dt)
        {

            if (!base.Active)
            {
                return;
            }
            foreach (ShopMapComponent mapComponent in this.wayPointComponents)
            {
                mapComponent.Render(map, dt);
            }

        }

        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            if (!base.Active)
            {
                return;
            }
            foreach (ShopMapComponent mapComponent in this.wayPointComponents)
            {
                mapComponent.OnMouseMove(args, mapElem, hoverText);
            }
        }



        private void RebuildMapComponents()
        {
            foreach(ShopRegistration shop in core.GetRegistry().GetAllShops())
            {
                if (shop.IsWaypointBroadcasted || true)
                {
                    ShopMapComponent comp = new ShopMapComponent(0, shop, this, (ICoreClientAPI)api);
                    wayPointComponents.Add(comp);
                }
               
            }
            
        }

        // Token: 0x060007CE RID: 1998 RVA: 0x00047338 File Offset: 0x00045538
        public void reloadIconTextures()
        {
            if (this.texturesByIcon != null)
            {
                foreach (KeyValuePair<string, LoadedTexture> val in this.texturesByIcon)
                {
                    val.Value.Dispose();
                }
            }
            this.texturesByIcon = null;
            this.ensureIconTexturesLoaded();
        }

        protected void ensureIconTexturesLoaded()
        {
            if (this.texturesByIcon != null)
            {
                return;
            }
            this.texturesByIcon = new Dictionary<string, LoadedTexture>();
            foreach (KeyValuePair<string, CreateIconTextureDelegate> val in this.WaypointIcons)
            {
                this.texturesByIcon[val.Key] = val.Value();
            }
        }
        // Token: 0x060007CD RID: 1997 RVA: 0x00047322 File Offset: 0x00045522
        public override void OnMapOpenedClient()
        {
            this.reloadIconTextures();
            this.RebuildMapComponents();
        }

        public override void OnMapClosedClient()
        {
            foreach (MapComponent val in this.wayPointComponents)
            {
                this.wayPointComponents.Remove(val);
            }
            this.wayPointComponents.Clear();
        }


        public override void Dispose()
        {
            if (this.texturesByIcon != null)
            {
                foreach (KeyValuePair<string, LoadedTexture> val in this.texturesByIcon)
                {
                    val.Value.Dispose();
                }
            }
            this.texturesByIcon = null;
            MeshRef meshRef = this.quadModel;
            if (meshRef != null)
            {
                meshRef.Dispose();
            }
            base.Dispose();
        }


        [HarmonyPatch(typeof(GuiDialogWorldMap), "Open")]
        private class GuiDialogWorldMapOpenPatch
        {
            private static void Postfix(EnumDialogType type)
            {
                /*if (ProspectorOverlayLayer._config.ShowGui && type == EnumDialogType.Dialog)
                {
                    ProspectorOverlayLayer._settingsDialog.TryOpen();
                    return;
                }
                ProspectorOverlayLayer._settingsDialog.TryClose();*/
            }
        }
    }
}
