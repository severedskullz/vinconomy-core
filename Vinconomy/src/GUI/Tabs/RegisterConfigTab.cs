using Commercially.Common;
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.Util;
using Commercially.Vinconomy.ModSystems;
using Commercially.Vinconomy.Registry;
using Commercially.Vinconomy.Util;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class RegisterConfigTab : ModularTab
    {
        public const string CODE = "Vinconomy.RegisterConfig";
        private ElementBounds shortDescClipBounds;
        private ElementBounds descClipBounds;

        string Name;
        string Description;
        string ShortDescription;
        string WebHook;
        

        public override string Code => CODE;
        public override string TabName => Lang.Get("vinconomy:tabname-register-config");


        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            CairoFont labelFont = CairoFont.WhiteSmallishText();
            CairoFont hoverFont = CairoFont.WhiteDetailText();
            CairoFont inputFont = CairoFont.TextInput();

            ElementBounds shopNameLabelBounds = ElementBounds.Fixed(0, 35, 500, 25);
            ElementBounds shopNameInputBounds = ElementBounds.FixedSize(500, 25).FixedUnder(shopNameLabelBounds);

            ElementBounds shortDescriptionLabelBounds = shopNameInputBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(500, 25);

            ElementBounds shortDescInsetBounds = ElementBounds.FixedSize(480, 200).FixedUnder(shortDescriptionLabelBounds);
            shortDescClipBounds = shortDescInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding).FixedGrow(0, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.
            ElementBounds shortDescContainerBounds = shortDescInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds shortDescScrollbarBounds = shortDescInsetBounds.RightCopy().WithFixedWidth(20);



            // ElementBounds shortDescriptionBounds = shortDescriptionLabelBounds.BelowCopy().WithFixedSize(500, 200);
            ElementBounds shortDescriptionSizeLabelBounds = shortDescInsetBounds.BelowCopy().WithFixedSize(500, 25);

            ElementBounds descriptionLabelBounds = shortDescriptionSizeLabelBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(500, 25);
            //ElementBounds descriptionBounds = descriptionLabelBounds.BelowCopy().WithFixedSize(500, 200);
            ElementBounds descriptionInsetBounds = ElementBounds.FixedSize(480, 200).FixedUnder(descriptionLabelBounds);

            descClipBounds = descriptionInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding).FixedGrow(0, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.
            ElementBounds descriptionContainerBounds = descriptionInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds descriptionScrollbarBounds = descriptionInsetBounds.RightCopy().WithFixedWidth(20);

            ElementBounds descriptionSizeLabelBounds = descriptionInsetBounds.BelowCopy().WithFixedSize(500, 25);

            ElementBounds webhookLabelBounds = descriptionSizeLabelBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(500, 25);
            ElementBounds webhookBounds = webhookLabelBounds.BelowCopy().WithFixedOffset(0, 0).WithFixedSize(500, 25);

            ElementBounds saveButtonBounds = webhookBounds.BelowCopy().WithFixedSize(60, 20).WithFixedOffset(0, 10).WithAlignment(EnumDialogArea.RightTop);

            rootBounds.WithChildren(shopNameLabelBounds, shopNameInputBounds, shortDescInsetBounds, descriptionInsetBounds,
                shortDescScrollbarBounds, descriptionScrollbarBounds,
                shortDescriptionLabelBounds, shortDescriptionSizeLabelBounds,
                descriptionLabelBounds, descriptionSizeLabelBounds,
                webhookLabelBounds, webhookBounds, saveButtonBounds);



            composer
                .AddStaticText(Lang.Get("commercially:label-name"), labelFont, shopNameLabelBounds)
                .AddTextInput(shopNameInputBounds, null, inputFont, "shopName")

                .AddStaticText(Lang.Get("vinconomy:gui-description-short"), labelFont, shortDescriptionLabelBounds)
                .AddHoverText(Lang.Get("vinconomy:tooltip-description-short"), hoverFont, 500, shortDescriptionLabelBounds)

                .AddInset(shortDescInsetBounds, 3)
                .BeginClip(shortDescClipBounds);
            try
            {
                //composer.AddTextArea(shortDescContainerBounds, UpdateShortDesc, inputFont, "shortDescription");
                composer.AddTextInput(shortDescContainerBounds, UpdateShortDesc, inputFont, "shortDescription");
            }
            catch (Exception ex)
            {
                composer.AddRichtext(Lang.Get("vinconomy:gui-error-tell-the-dev") + ex.Message, labelFont, shortDescContainerBounds, "description");
            }
            composer.EndClip()
            //.AddVerticalScrollbar(OnNewShortDescScrollbarValue, shortDescScrollbarBounds, "shortdescription-scrollbar")
            .AddDynamicText("0 / 250", CairoFont.WhiteSmallishText(), shortDescriptionSizeLabelBounds, "shortDescriptionLength")

            .AddStaticText(Lang.Get("vinconomy:gui-description-long"), labelFont, descriptionLabelBounds)
            .AddHoverText(Lang.Get("vinconomy:tooltip-description-long"), hoverFont, 500, descriptionLabelBounds)
            //.AddTextArea(descriptionBounds, UpdateLongCount, CairoFont.TextInput(), "description")
            .AddInset(descriptionInsetBounds, 3)
            .BeginClip(descClipBounds);
            try
            {
                //composer.AddTextArea(descriptionContainerBounds, UpdateLongDesc, inputFont, "description");
                composer.AddTextInput(descriptionContainerBounds, UpdateLongDesc, inputFont, "description");
            }
            catch (Exception ex)
            {
                composer.AddRichtext("There was an error in the store's description. Exception " + ex.Message, labelFont, descriptionContainerBounds, "description");
            }
            composer.EndClip()
            //.AddVerticalScrollbar(OnNewDescriptionScrollbarValue, descriptionScrollbarBounds, "description-scrollbar")
            .AddDynamicText("0 / 2500", labelFont, descriptionSizeLabelBounds, "descriptionLength")

            .AddStaticText(Lang.Get("vinconomy:gui-webhook"), labelFont, webhookLabelBounds)
            .AddHoverText(Lang.Get("vinconomy:tooltip-webhook"), hoverFont, 500, webhookLabelBounds)
            .AddTextInput(webhookBounds, null, inputFont, "webhook")

            .AddButton(Lang.Get("vinconomy:gui-save"), OnSaveShopConfigPressed, saveButtonBounds, EnumButtonStyle.Small, "save");


            UpdateConfig();
           
            
            UpdateShortDescScrollbar();
            UpdateDescScrollbar();

        }

        public override bool IsVisible()
        {
            return CommUtils.IsLocalPlayerOwner(this.BlockEntity, ClientApi);
        }

        public override void OnGuiClosed()
        {

        }

        public override void OnGuiOpened()
        {

        }

        public void UpdateConfig()
        {
            int shortLength = ShortDescription == null ? 0 : ShortDescription.Length;
            int longLength = Description == null ? 0 : Description.Length;
            Gui.Composer.GetTextInput("shopName").SetValue(Name ?? "");

            //GuiElementTextArea shortDesc = Gui.Composer.GetTextArea("shortDescription");
            GuiElementTextInput shortDesc = Gui.Composer.GetTextInput("shortDescription");
            shortDesc.SetValue(ShortDescription ?? "");

            //GuiElementTextArea longDesc = Gui.Composer.GetTextArea("description");
            GuiElementTextInput longDesc = Gui.Composer.GetTextInput("description");
            longDesc.SetValue(Description ?? "");

            Gui.Composer.GetTextInput("webhook").SetValue(WebHook ?? "");
            Gui.Composer.GetDynamicText("shortDescriptionLength").SetNewText($"{shortLength} / 250");
            Gui.Composer.GetDynamicText("descriptionLength").SetNewText($"{longLength} / 1024");
        }

        public override void OnRecievedData(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                Description = reader.ReadString();
                ShortDescription = reader.ReadString();
                WebHook = reader.ReadString();
            }
        }

        public override byte[] OnSendData(BlockEntity entity, Caller caller, BlockSelection blockSel, string key)
        {
            IOwnableReference ownable = entity.GetBehavior<IOwnableReference>();
            if (ownable != null)
            {
                ShopConfiguration config = entity.Api.ModLoader.GetModSystem<VinconomyCoreSystem>().GetShopConfiguration(ownable.ID);
                if (config == null)
                    return null;

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(config.Description);
                    writer.Write(config.ShortDescription);
                    writer.Write(config.WebHook);
                    data = ms.ToArray();
                }
                return data;

            }
            return null;
        }

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            IOwnableReference ownable = entity.GetBehavior<IOwnableReference>();
            if (ownable != null)
            {
                Name = ownable.Name;
            }
        }

        private bool OnSaveShopConfigPressed()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(Gui.Composer.GetTextInput("shopName").GetText());
                writer.Write(Gui.Composer.GetTextInput("description").GetText());
                writer.Write(Gui.Composer.GetTextInput("shortDescription").GetText());
                writer.Write(Gui.Composer.GetTextInput("webhook").GetText());
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_CONFIGURATION, data);
            return true;
        }

        private void UpdateShortDescScrollbar()
        {
            float descScrollVisibleHeight = (float)descClipBounds.fixedHeight;
            //double descScrollTotalHeight = Gui.Composer.GetTextArea("shortDescription").Bounds.fixedHeight;
            double descScrollTotalHeight = Gui.Composer.GetTextInput("shortDescription").Bounds.fixedHeight;
            //Gui.Composer.GetScrollbar("shortdescription-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);
        }

        private void UpdateDescScrollbar()
        {
            float descScrollVisibleHeight = (float)descClipBounds.fixedHeight;
            //double descScrollTotalHeight = Gui.Composer.GetTextArea("description").Bounds.fixedHeight;
            double descScrollTotalHeight = Gui.Composer.GetTextInput("description").Bounds.fixedHeight;
            //Gui.Composer.GetScrollbar("description-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);
        }

        private void OnNewDescriptionScrollbarValue(float value)
        {
            ElementBounds bounds = Gui.Composer.GetTextArea("description").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void OnNewShopAccessScrollbarValue(float value)
        {
            ElementBounds bounds = Gui.Composer.GetStaticText("container").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }


        private void OnNewShortDescScrollbarValue(float value)
        {
            ElementBounds bounds = Gui.Composer.GetTextArea("shortDescription").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void UpdateShortDesc(string obj)
        {
            if (!Gui.Composer.Composed) return;
            GuiElementDynamicText desc = Gui.Composer.GetDynamicText("shortDescriptionLength");

            desc.SetNewText($"{obj.Length} / 250");

            if (obj.Length > 250)
            {
                //GuiElementTextArea area = Gui.Composer.GetTextArea("shortDescription");
                GuiElementTextInput area = Gui.Composer.GetTextInput("shortDescription");
                area.SetValue(obj.Substring(0, 250));
            }

            if (obj.Length >= 250)
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 0;
                desc.Font.Color[2] = 0;
            }
            else
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 255;
                desc.Font.Color[2] = 255;
            }
            desc.RecomposeText();
            UpdateShortDescScrollbar();
        }

        private void UpdateLongDesc(string obj)
        {
            if (!Gui.Composer.Composed) return;
            GuiElementDynamicText desc = Gui.Composer.GetDynamicText("descriptionLength");
            if (obj.Length > 1024)
            {
                //GuiElementTextArea area = Gui.Composer.GetTextArea("description");
                GuiElementTextInput area = Gui.Composer.GetTextInput("description");
                area.SetValue(obj.Substring(0, 1024));
            }

            desc.SetNewText($"{obj.Length} / 1024");
            if (obj.Length >= 1024)
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 0;
                desc.Font.Color[2] = 0;
            }
            else
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 255;
                desc.Font.Color[2] = 255;
            }
            desc.RecomposeText();
            UpdateDescScrollbar();
        }
    }
}
