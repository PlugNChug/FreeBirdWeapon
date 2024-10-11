using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;

namespace FreeBirdWeapon.Content
{
    internal class Recipes : ModSystem
    {
        public override void AddRecipeGroups()
        {
            RecipeGroup group1 = new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(ItemID.CopperBar),
            [
                ItemID.CopperBar,
                ItemID.TinBar,
            ]);
            RecipeGroup.RegisterGroup("CopperBar", group1);
        }
    }
}
