using System;
using Viconomy.BlockEntities;
using Vintagestory.API.Client;

namespace Viconomy.Renderer
{
    public class DistanceRenderer : IRenderer, IDisposable
    {
        BEVinconBase viconBase;
        ICoreClientAPI api;
        int range = 30;
        public DistanceRenderer(BEVinconBase viconBase)
        {
            this.viconBase = viconBase;
            api = ((ICoreClientAPI)this.viconBase.Api);
            api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "viconDistanceRenderer");
        }

        public double RenderOrder => 0.5;

        public int RenderRange => range;

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            bool shouldRender = api.World.Player.Entity.CameraPos.DistanceTo(viconBase.Pos.ToVec3d()) < range;
            if (shouldRender != viconBase.shouldRenderInventory)
            {
                viconBase.shouldRenderInventory = shouldRender;
                this.viconBase.MarkDirty(true, null);
                viconBase.UpdateMeshes();
            }
        }
    }
}
