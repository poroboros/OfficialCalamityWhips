using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Items.Placeables.FurnitureCosmilite
{
    public class CosmiliteBed : ModItem
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
            item.createTile = ModContent.TileType<Tiles.CosmiliteBed>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<CosmiliteBrick>(), 15);
            recipe.AddIngredient(ItemID.Silk, 5);
            recipe.SetResult(this, 1);
            recipe.AddTile(null, "DraedonsForge");
            recipe.AddRecipe();
        }
    }
}
