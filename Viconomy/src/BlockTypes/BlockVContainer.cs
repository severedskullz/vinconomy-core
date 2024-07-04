using System;
using System.Collections.Generic;
using System.Linq;
using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.BlockTypes
{
    public class BlockVContainer : Block, ITexPositionSource
    {
        private ITexPositionSource tmpTextureSource;

        public Size2i AtlasSize { get; private set; }

        /**
         * ITexPositionSource returns a given material for a specified texture code when applied to the tesselator.
         * What this means in laymens terms is whenever the Shape has a given "texture" code defined, it will swap out whats in the JSON
         * with what is provided by the Getter here.
         * 
         * As an example, saw we have the following in a JSON shapefile:
            ...
            "textures": {
		        "primary": "game:block/cloth/linen/white",
		        "secondary": "game:block/wood/planks/oak1"
	        },
	        "elements": [
		        {
			        "name": "Mat",
			        "from": [ 0.0, 0.0, 0.0 ],
			        "to": [ 16.0, 1.0, 16.0 ],
			        "faces": {
				        "north": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "east": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "south": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "west": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "up": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },
				        "down": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 16.0 ] }
			        },
             ...
         * 
         *  Here, "primary" is a texture code. without ITextPositionSource implemented, it would always return the white linen texture.
         *  However, with the implementation, we can change this base on an attribute or other variable. For instance, we could return
         *  one of two texture paths based off of a random value, or the position of the block in the world.
         */
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode == "primary")
                {
                    return this.tmpTextureSource[this.PrimaryMaterial];
                }
                if (textureCode == "secondary")
                {
                    return this.tmpTextureSource[this.SecondaryMaterial];
                }
                if (textureCode == "deco")
                {
                    return this.tmpTextureSource[this.DecoMaterial];
                }
                return this.tmpTextureSource[textureCode];
            }
        }

        private string PrimaryMaterial;
        private string SecondaryMaterial;
        private string DecoMaterial;

        public BlockVContainer()
        {
            this.PlacedPriorityInteract = true;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {

                PrimaryMaterial = byItemStack.Attributes.GetString("PrimaryMaterial", "default");
                SecondaryMaterial = byItemStack.Attributes.GetString("SecondaryMaterial", "default");
                DecoMaterial = byItemStack.Attributes.GetString("DecoMaterial", "default");


                BEViconBase viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconBase;
                if (viconBlock != null)
                {
                    viconBlock.SetOwner(byPlayer);
                    viconBlock.PrimaryMaterial = PrimaryMaterial;
                    viconBlock.SecondaryMaterial = SecondaryMaterial;
                    viconBlock.DecoMaterial = DecoMaterial;
                }
            }

            return result;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(world.GetBlock(base.CodeWithParts("north")), 1);
            BEViconBase be = world.BlockAccessor.GetBlockEntity(pos) as BEViconBase;
            if (be != null)
            {
                stack.Attributes.SetString("PrimaryMaterial", be.PrimaryMaterial);
                stack.Attributes.SetString("SecondaryMaterial", be.SecondaryMaterial);
                stack.Attributes.SetString("DecoMaterial", be.DecoMaterial);
            }
            else
            {
                stack.Attributes.SetString("PrimaryMaterial", "default");
                stack.Attributes.SetString("SecondaryMaterial", "default");
                stack.Attributes.SetString("DecoMaterial", "default");
            }
            return stack;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, "viconStallGuiMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
            string primary = itemstack.Attributes.GetString("PrimaryMaterial", "default");
            string secondary = itemstack.Attributes.GetString("SecondaryMaterial", "default");
            string deco = itemstack.Attributes.GetString("DecoMaterial", "default");
            string key = $"{itemstack.Collectible.Code.FirstCodePart()}-{primary}-{secondary}-{deco}";
            MultiTextureMeshRef meshref;
            if (!meshrefs.TryGetValue(key, out meshref))
            {
                AssetLocation shapeloc = this.Shape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
                MeshData mesh = this.GenMesh(capi, primary, secondary, deco, shape, null);
                meshrefs[key] = capi.Render.UploadMultiTextureMesh(mesh);
                meshref = meshrefs[key];
            }
            renderinfo.ModelRef = meshref;
            renderinfo.CullFaces = false;
        }


        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //Console.WriteLine(api.Side + ": On interaction start was called!");
            BEViconBase be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconBase;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return false;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BEViconBase be = world.BlockAccessor.GetBlockEntity(selection.Position) as BEViconBase;
            List<WorldInteraction> interactions = new List<WorldInteraction>();
            if (be != null)
            {
                int index = selection.SelectionBoxIndex;
                ItemSlot product = be.FindFirstNonEmptyStockSlotForStall(index);
                ItemSlot currency = be.GetCurrencyForStall(index);

                //StallSlot slot = slots[selection.SelectionBoxIndex];

                if (be.Owner != forPlayer.PlayerUID)
                {
                    if (currency.Itemstack != null && product != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = new ItemStack[] { currency.Itemstack }

                        });

                        ItemStack fiveStack = currency.Itemstack.Clone();
                        fiveStack.StackSize = 5 * fiveStack.StackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase-bulk",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = new string[] { "sneak", "sprint" },
                            Itemstacks = new ItemStack[] { fiveStack }
                        });
                    }
                } else {
                    ItemSlot firstSlot = product;
                    if (firstSlot != null)
                    {
                        ItemStack helpSlot = firstSlot.Itemstack.Clone();
                        helpSlot.StackSize = 1;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = new ItemStack[] { helpSlot }
                        });

                        ItemStack helpSlotStack = helpSlot.Clone();
                        helpSlotStack.StackSize = helpSlotStack.Collectible.MaxStackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = new string[] { "sneak", "sprint" },
                            Itemstacks = new ItemStack[] { helpSlotStack }
                        });

                        if (currency.Itemstack != null)
                        {
                            interactions.Add(new WorldInteraction
                            {
                                ActionLangCode = "vinconomy:stall-purchase",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCode = "sneak",
                                Itemstacks = new ItemStack[] { currency.Itemstack }

                            });

                            ItemStack fiveStack = currency.Itemstack.Clone();
                            fiveStack.StackSize = 5 * fiveStack.StackSize;
                            interactions.Add(new WorldInteraction
                            {
                                ActionLangCode = "vinconomy:stall-purchase-bulk",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCodes = new string[] { "sneak", "sprint" },
                                Itemstacks = new ItemStack[] { fiveStack }
                            });
                        }
                    } else {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak"
                        });
                    }
                }

                interactions.Add(new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-open-menu",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                });
                
            }

            return interactions.ToArray();
        }





        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            ViconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
            if (modSystem != null)
            {
                modSystem.BlockPlaced(this.Code, world, blockPos, byItemStack);
            }
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            ViconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
            if (modSystem != null && !modSystem.TryPlaceBlock(world, byPlayer, itemstack, blockSel))
            {
                failureCode = "__ignore__";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public MeshData GenMesh(ICoreClientAPI capi, string primaryMaterial, string secondaryMaterial, string decoMaterial, Shape shape = null, ITesselatorAPI tesselator = null)
        {
            if (tesselator == null)
            {
                tesselator = capi.Tesselator;
            }
            this.tmpTextureSource = tesselator.GetTextureSource(this, 0, false);
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(capi, "vinconomy:shapes/" + this.Shape.Base.Path + ".json");
            }
            if (shape == null)
            {
                return null;
            }
            this.AtlasSize = capi.BlockTextureAtlas.Size;
            this.PrimaryMaterial = primaryMaterial;
            this.SecondaryMaterial = secondaryMaterial;
            this.DecoMaterial = decoMaterial;
            //Block glassBlock = capi.World.GetBlock(new AssetLocation("glass-" + glassMaterial));
            //this.glassTextureSource = tesselator.GetTextureSource(glassBlock, 0, false);
            MeshData mesh;
            tesselator.TesselateShape("viconStall", shape, out mesh, this, new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ), 0, 0, 0, null, null);
            return mesh;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null)
            {
                return;
            }
            object obj;
            if (capi.ObjectCache.TryGetValue("viconStallGuiMeshRefs", out obj))
            {
                foreach (KeyValuePair<string, MultiTextureMeshRef> val in (obj as Dictionary<string, MultiTextureMeshRef>))
                {
                    val.Value.Dispose();
                }
                capi.ObjectCache.Remove("viconStallGuiMeshRefs");
            }
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            //base.GetDropsForHandbook (handbookStack, forPlayer);
            return new BlockDropItemStack[]
            {
                new BlockDropItemStack(handbookStack, 1f)
            };
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer == null)
                return;

            BEViconBase vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEViconBase;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                ViconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
                if (modSystem != null && !modSystem.BlockBroken(this.Code, world, pos, byPlayer, dropQuantityMultiplier))
                {
                    return;
                }
                DoOnBlockBroken(world, pos, byPlayer);
            }

            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.CommandError, null);
        }

        //This is mostly just Vanilla "Lantern" code with a few redundant checks removed.
        private void DoOnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            bool preventDefault = false;
            foreach (BlockBehavior blockBehavior in this.BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                blockBehavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault)
                {
                    preventDefault = true;
                }
                if (handled == EnumHandling.PreventSubsequent)
                {
                    return;
                }
            }
            if (preventDefault)
            {
                return;
            }
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack drops = this.OnPickBlock(world, pos);
                world.SpawnItemEntity(drops, new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5), null);
                world.PlaySoundAt(this.Sounds.GetBreakSound(byPlayer), (double)pos.X, (double)pos.Y, (double)pos.Z, byPlayer, true, 32f, 1f);
            }
            if (this.EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken(byPlayer);
                }
            }
            world.BlockAccessor.SetBlock(0, pos);
        }


    }

}
