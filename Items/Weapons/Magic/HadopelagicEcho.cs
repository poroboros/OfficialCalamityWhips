using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Magic;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Magic
{
    public class HadopelagicEcho : ModItem
    {
        private int counter = 0;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hadopelagic Echo");
            Tooltip.SetDefault("Fires a string of bouncing sound waves that become stronger as they travel\n" +
            "Sound waves echo additional sound waves on enemy hits");
        }

        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 15;
            Item.width = 60;
            Item.height = 60;
            Item.useTime = 8;
            Item.reuseDelay = 20;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(2, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.shootSpeed = 10f;
            Item.shoot = ModContent.ProjectileType<HadopelagicEchoSoundwave>();
            Item.Calamity().customRarity = CalamityRarity.Violet;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-5, 0);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, counter);
            counter++;
            if (counter >= 5)
                counter = 0;
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ModContent.ItemType<EidolicWail>()).AddIngredient(ModContent.ItemType<ReaperTooth>(), 20).AddIngredient(ModContent.ItemType<DepthCells>(), 20).AddIngredient(ModContent.ItemType<Lumenite>(), 20).AddIngredient(ModContent.ItemType<AuricBar>(), 5).AddTile(ModContent.TileType<CosmicAnvil>()).Register();
        }
    }
}
