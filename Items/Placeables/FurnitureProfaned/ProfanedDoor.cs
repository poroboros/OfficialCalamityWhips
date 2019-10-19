using Terraria.ModLoader;
namespace CalamityMod.Items.Placeables.FurnitureProfaned
{
    public class ProfanedDoor : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            item.width = 14;
            item.height = 28;
            item.maxStack = 99;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = 1;
            item.consumable = true;
            item.createTile = ModContent.TileType<ProfanedDoorClosed>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<ProfanedRock>(), 6);
            recipe.SetResult(this, 1);
            recipe.AddTile(null, "ProfanedBasin");
            recipe.AddRecipe();
        }
    }
}
