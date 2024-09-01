using Cairo;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Viconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.Map
{
    public class ShopMapLayer : MapLayer
    {
        private VinconomyCoreSystem core;
        private List<MapComponent> wayPointComponents = new List<MapComponent>();
        public MeshRef quadModel;
        public Dictionary<string, LoadedTexture> texturesByIcon;

        public OrderedDictionary<string, CreateIconTextureDelegate> WaypointIcons { get; set; } = new OrderedDictionary<string, CreateIconTextureDelegate>();
        public List<int> WaypointColors;
        private static string[] hexcolors = new string[]
        {
            "#F9D0DC",
            "#F179AF",
            "#F15A4A",
            "#ED272A",
            "#A30A35",
            "#FFDE98",
            "#EFFD5F",
            "#F6EA5E",
            "#FDBB3A",
            "#C8772E",
            "#F47832",
            "C3D941",
            "#9FAB3A",
            "#94C948",
            "#47B749",
            "#366E4F",
            "#516D66",
            "93D7E3",
            "#7698CF",
            "#20909E",
            "#14A4DD",
            "#204EA2",
            "#28417A",
            "#C395C4",
            "#92479B",
            "#8E007E",
            "#5E3896",
            "D9D4CE",
            "#AFAAA8",
            "#706D64",
            "#4F4C2B",
            "#BF9C86",
            "#9885530",
            "#5D3D21",
            "#FFFFFF",
            "#080504"
        };

        public ShopMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            core = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            if (api.Side == EnumAppSide.Client)
            {
                core.ShopMapLayer = this;
                ICoreClientAPI capi = api as ICoreClientAPI;

                this.WaypointColors = new List<int>();
                for (int i = 0; i < hexcolors.Length; i++)
                {
                    this.WaypointColors.Add(ColorUtil.Hex2Int(hexcolors[i]));
                }


                List<IAsset> many = api.Assets.GetMany("textures/icons/map/", "vinconomy", false);
                
                using (List<IAsset>.Enumerator enumerator = many.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        IAsset icon = enumerator.Current;
                        // Adding in check to see if the . is the first file or doesnt exist.
                        // Got a report that someone is crashing due to name.UcFirst() with the string being empty
                        // We couldnt figure out how/why, but its specific to their machine.
                        string name = icon.Name;
                        int fileLength = icon.Name.IndexOf(".");
                        if (fileLength > 0)
                        {
                            name = icon.Name.Substring(0, fileLength);
                        } else
                        {
                            api.Logger.Warning("Could not find file extension for file '" + icon.Name + "' Using the entire file name for the delegate...");
                        }
 
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



        public void RebuildMapComponents()
        {
            wayPointComponents.Clear();
            foreach(ShopRegistration shop in core.GetRegistry().GetAllShops())
            {
                if (shop.IsWaypointBroadcasted)
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

        public override void OnMapOpenedClient()
        {
            this.reloadIconTextures();
            this.RebuildMapComponents();
        }

        public override void OnMapClosedClient()
        {
            foreach (MapComponent val in this.wayPointComponents)
            {
                val.Dispose();
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
