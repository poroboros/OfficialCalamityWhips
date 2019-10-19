using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Items.Placeables.FurnitureBotanic
{
    public class BotanicBench : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 20;
            item.maxStack = 999;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = 1;
            item.value = 0;
            item.consumable = true;
            item.createTile = ModContent.TileType<Tiles.BotanicBench>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<UelibloomBrick>(), 5);
            recipe.AddIngredient(ItemID.Vine, 2);
            recipe.SetResult(this, 1);
            recipe.AddTile(null, "BotanicPlanter");
            recipe.AddRecipe();
        }
    }
}
