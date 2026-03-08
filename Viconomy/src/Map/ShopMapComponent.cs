using System;
using System.Linq;
using System.Text;
using Vinconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vinconomy.Map
{
    public class ShopMapComponent : MapComponent
    {
        private Vec2f viewPos = new Vec2f();
        private Vec4f color = new Vec4f();
        private ShopRegistration waypoint;
        private int waypointIndex;
        private Matrixf mvMat = new Matrixf();
        private ShopMapLayer wpLayer;
        private bool mouseOver;
        public static float IconScale = 0.85f;

        public ShopMapComponent(int waypointIndex, ShopRegistration waypoint, ShopMapLayer wpLayer, ICoreClientAPI capi) : base(capi)
        {
            this.waypointIndex = waypointIndex;
            this.waypoint = waypoint;
            this.wpLayer = wpLayer;
            ColorUtil.ToRGBAVec4f(this.waypoint.WaypointColor, ref this.color);
            color.A = 1;
        }

        public override void Render(GuiElementMap map, float dt)
        {
            if (waypoint == null || !waypoint.IsWaypointBroadcasted) {
                return;
            }

            Vec3d pos = new Vec3d(this.waypoint.X, this.waypoint.Y, this.waypoint.Z);
            map.TranslateWorldPosToViewPos(pos, ref this.viewPos);
             if (this.viewPos.X < -10f || this.viewPos.Y < -10f || (double)this.viewPos.X > map.Bounds.OuterWidth + 10.0 || (double)this.viewPos.Y > map.Bounds.OuterHeight + 10.0)
            {
                return;
            }
            float x = (float)(map.Bounds.renderX + (double)this.viewPos.X);
            float y = (float)(map.Bounds.renderY + (double)this.viewPos.Y);
            ICoreClientAPI api = map.Api;
            IShaderProgram prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
            prog.Uniform("rgbaIn", this.color);
            prog.Uniform("extraGlow", 0);
            prog.Uniform("applyColor", 0);
            prog.Uniform("noTexture", 0f);
            float hover = (float)(this.mouseOver ? 12 : 0) - 1.5f * Math.Max(1.5f, 1f / map.ZoomLevel);
            LoadedTexture tex;
            if (!this.wpLayer.texturesByIcon.TryGetValue(this.waypoint.WaypointIcon, out tex))
            {
                this.waypoint.WaypointIcon = "genericViconShop";
                this.wpLayer.texturesByIcon.TryGetValue("genericViconShop", out tex);
            }
            if (tex != null)
            {
                prog.BindTexture2D("tex2d", tex.TextureId, 0);
                prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
                this.mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)tex.Width + hover, (float)tex.Height + hover, 0f).Scale(0.5f * WaypointMapComponent.IconScale, 0.5f * WaypointMapComponent.IconScale, 0f);
                Matrixf shadowMvMat = this.mvMat.Clone().Scale(1.25f, 1.25f, 1.25f);
                prog.Uniform("rgbaIn", new Vec4f(0f, 0f, 0f, 0.6f));
                prog.UniformMatrix("modelViewMatrix", shadowMvMat.Values);
                api.Render.RenderMesh(this.wpLayer.quadModel);
                prog.Uniform("rgbaIn", this.color);
                prog.UniformMatrix("modelViewMatrix", this.mvMat.Values);
                api.Render.RenderMesh(this.wpLayer.quadModel);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            //Somehow this is still triggering even when pos is null - prevent us from crashing...
            if (this.waypoint.Position == null)
            {
                return;
            }

            Vec2f viewPos = new Vec2f();
            mapElem.TranslateWorldPosToViewPos(this.waypoint.Position.ToVec3d(), ref viewPos);
            double x = (double)viewPos.X + mapElem.Bounds.renderX;
            double y = (double)viewPos.Y + mapElem.Bounds.renderY;
           
            double dX = (double)args.X - x;
            double dY = (double)args.Y - y;
            float size = RuntimeEnv.GUIScale * 8f;
            if (this.mouseOver = (Math.Abs(dX) < (double)size && Math.Abs(dY) < (double)size))
            {
                string text = Lang.Get("vinconomy:wp-" + waypoint.WaypointIcon) + ": " + this.waypoint.Name;

                hoverText.AppendLine(text);
            }
        }

        public override void OnMouseUpOnElement(MouseEvent args, GuiElementMap mapElem)
        {
            /*
            if (args.Button == EnumMouseButton.Right)
            {
                Vec2f viewPos = new Vec2f();
                mapElem.TranslateWorldPosToViewPos(this.waypoint.Position, ref viewPos);
                double x = (double)viewPos.X + mapElem.Bounds.renderX;
                double y = (double)viewPos.Y + mapElem.Bounds.renderY;
                if (this.waypoint.Pinned)
                {
                    mapElem.ClampButPreserveAngle(ref viewPos, 2);
                    x = (double)viewPos.X + mapElem.Bounds.renderX;
                    y = (double)viewPos.Y + mapElem.Bounds.renderY;
                    x = (double)((float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2.0, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2.0));
                    y = (double)((float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2.0, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2.0));
                }
                double value = (double)args.X - x;
                double dY = (double)args.Y - y;
                float size = RuntimeEnv.GUIScale * 8f;
                if (Math.Abs(value) < (double)size && Math.Abs(dY) < (double)size)
                {
                    if (this.editWpDlg != null)
                    {
                        this.editWpDlg.TryClose();
                        this.editWpDlg.Dispose();
                    }
                    GuiDialogWorldMap mapdlg = this.capi.ModLoader.GetModSystem<WorldMapManager>(true).worldMapDlg;
                    this.editWpDlg = new GuiDialogEditWayPoint(this.capi, mapdlg.MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer, this.waypoint, this.waypointIndex);
                    this.editWpDlg.TryOpen();
                    this.editWpDlg.OnClosed += delegate ()
                    {
                        this.capi.Gui.RequestFocus(mapdlg);
                    };
                    args.Handled = true;
                }
            }*/
        }


    }
}
