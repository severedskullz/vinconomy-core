using Commercially.Common.ModSystems;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors.DisplayProviders
{
    public class DisplayPurchaseContentsBehavior : BaseDisplayContentsBehavior
    {
        TransformationData[][] CtfData;

        public DisplayPurchaseContentsBehavior(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _InventoryProvider = this.GetComponent<IStallInventoryProvider>();
            CommerciallyCore = api.ModLoader.GetModSystem<CommerciallyModSystem>();

            TransformationData gTransform = properties["globalTransform"]?.AsObject(new TransformationData());



            JsonObject[] stallTransforms = properties["currencyTransforms"]?.AsArray();
            if (stallTransforms != null)
            {
                CtfData = new TransformationData[_InventoryProvider.StallCount][];
                for (int i = 0; i < _InventoryProvider.StallCount; i++)
                {
                    JsonObject[] currencyTransforms = stallTransforms[i]?.AsArray();
                    int currencyCount = currencyTransforms?.Length ?? 0;

                    CtfData[i] = new TransformationData[currencyCount];
                    for (int j = 0; j < currencyCount; j++)
                    {
                        TransformationData tdata;
                        if (stallTransforms != null && i < stallTransforms.Length)
                        {

                            tdata = currencyTransforms[j]?.AsObject(new TransformationData());
                        }
                        else
                        {
                            tdata = new TransformationData();
                            tdata.Reset();
                        }

                        if (gTransform != null)
                        {
                            tdata.globalTransform = gTransform;
                        }

                        tdata.preRotate += (float)((Block.Shape.rotateY));
                        CtfData[i][j] = tdata;
                    }

                }
            }
            
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (mesher == null)
                return;


            if (ShouldRenderInventory)
            {
                MeshData mesh = null;
                ItemSlot slot = null;
                for (int i = 0; i < _InventoryProvider.StallCount; i++)
                {
                    try
                    {
                        BaseStallSlot stall = _InventoryProvider.GetStallSlot(i);
                        if (stall.GetNumPurchasesRemaining() <= 0)
                        {
                            continue;
                        }

                        slot = stall.Product;

                        if (slot?.Itemstack != null && TfData != null)
                        {
                            mesh = GetOrCreateMesh(slot, i);
                            if (mesh != null)
                            {
                                float[] matricies = TfData[i].BuildMatrix();
                                mesher.AddMeshData(mesh, matricies);
                            }
                        }

                        slot = stall.Currency;

                        if (slot?.Itemstack != null && CtfData != null)
                        {
                            for (int j = 0; j < CtfData[i].Length; j++)
                            {
                                mesh = GetOrCreateMesh(slot, i);
                                if (mesh != null)
                                {
                                    float[] matricies = CtfData[i][j].BuildMatrix();
                                    mesher.AddMeshData(mesh, matricies);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CommerciallyCore.Mod.Logger.Error($"Had some trouble rendering mesh in a stall @ {Pos.X} {Pos.Y} {Pos.Z} for slot {i}. Exception was {e.Message}");
                    }

                }
            }

        }

    }
    
}
