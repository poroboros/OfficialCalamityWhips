using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Weapons.Ranged
{
    public class ThePack : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Pack");
            Tooltip.SetDefault("Fires large homing rockets that explode into more homing mini rockets when in proximity to an enemy");
        }

        public override void SetDefaults()
        {
            item.damage = 3000;
            item.crit += 10;
            item.ranged = true;
            item.width = 96;
            item.height = 40;
            item.useTime = 50;
            item.useAnimation = 50;
            item.useStyle = 5;
            item.noMelee = true;
            item.knockBack = 7.5f;
            item.value = Item.buyPrice(1, 80, 0, 0);
            item.rare = 10;
            item.Calamity().postMoonLordRarity = 21;
            item.UseSound = SoundID.Item11;
            item.autoReuse = true;
            item.shootSpeed = 24f;
            item.shoot = ModContent.ProjectileType<ThePackMissile>();
            item.useAmmo = 771;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-40, 0);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            Projectile.NewProjectile(position.X, position.Y, speedX, speedY, ModContent.ProjectileType<ThePackMissile>(), damage, knockBack, player.whoAmI, 0.0f, 0.0f);
            return false;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<Scorpion>());
            recipe.AddIngredient(ItemID.MarbleBlock, 50);
            recipe.AddIngredient(ModContent.ItemType<CosmiliteBar>(), 10);
            recipe.AddIngredient(ModContent.ItemType<ArmoredShell>(), 4);
            recipe.AddTile(null, "DraedonsForge");
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
