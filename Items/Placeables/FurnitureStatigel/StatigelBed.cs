using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Items.Placeables.FurnitureStatigel
{
    public class StatigelBed : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 20;
            item.maxStack = 99;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = 1;
            item.consumable = true;
            item.createTile = ModContent.TileType<Tiles.StatigelBed>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<StatigelBlock>(), 15);
            recipe.AddIngredient(ItemID.Silk, 5);
            recipe.SetResult(this, 1);
            recipe.AddTile(null, "StaticRefiner");
            recipe.AddRecipe();
        }
    }
}
