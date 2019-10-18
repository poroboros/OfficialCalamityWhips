using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Weapons.Magic
{
    public class Apotheosis : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Apotheosis");
            Tooltip.SetDefault("Unleashes interdimensional projection magic\n" +
                "Eat worms");
        }

        public override void SetDefaults()
        {
            item.damage = 366;
            item.magic = true;
            item.mana = 42;
            item.width = 30;
            item.height = 34;
            item.useTime = 21;
            item.useAnimation = 21;
            item.useStyle = 5;
            item.useTurn = false;
            item.noMelee = true;
            item.knockBack = 4f;
            item.value = Item.buyPrice(5, 0, 0, 0);
            item.rare = 10;
            item.UseSound = SoundID.Item92;
            item.autoReuse = true;
            item.shoot = ModContent.ProjectileType<ApothMark>();
            item.shootSpeed = 15;
            item.Calamity().postMoonLordRarity = 16;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Vector2 origin = new Vector2(15f, 15f);
            spriteBatch.Draw(ModContent.GetTexture("CalamityMod/Items/Weapons/Magic/ApotheosisGlow"), item.Center - Main.screenPosition, null, Color.White, rotation, origin, 1f, SpriteEffects.None, 0f);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(null, "SubsumingVortex");
            recipe.AddIngredient(null, "CosmicDischarge");
            recipe.AddIngredient(null, "StaffoftheMechworm", 2);
            recipe.AddIngredient(null, "Excelsus", 2);
            recipe.AddIngredient(null, "DarksunFragment", 33);
            recipe.AddIngredient(null, "NightmareFuel", 33);
            recipe.AddIngredient(null, "EndothermicEnergy", 33);
            recipe.AddIngredient(null, "CosmiliteBar", 33);
            recipe.AddIngredient(null, "Phantoplasm", 33);
            recipe.AddIngredient(null, "ShadowspecBar", 5);
            recipe.AddTile(null, "DraedonsForge");
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
