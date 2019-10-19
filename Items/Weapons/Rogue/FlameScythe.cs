using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Weapons.Rogue
{
    public class FlameScythe : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Flame Scythe");
            Tooltip.SetDefault("Throws a scythe that explodes on enemy hits");
        }

        public override void SafeSetDefaults()
        {
            item.width = 50;
            item.damage = 130;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.autoReuse = true;
            item.useAnimation = 19;
            item.useStyle = 1;
            item.useTime = 19;
            item.knockBack = 8.5f;
            item.UseSound = SoundID.Item1;
            item.height = 48;
            item.value = Item.buyPrice(0, 80, 0, 0);
            item.rare = 8;
            item.shoot = ModContent.ProjectileType<FlameScytheProjectile>();
            item.shootSpeed = 16f;
            item.Calamity().rogue = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<CruptixBar>(), 9);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
