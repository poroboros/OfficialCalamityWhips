using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Summon
{

    public class UnderBite : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Summon";

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = 80;
            Item.knockBack = 2;

            Item.shoot = ModContent.ProjectileType<UnderBiteProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item152;

            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = CalamityGlobalItem.GetBuyPrice(Item.rare);

        }

        // Makes the whip receive melee prefixes
        public override bool MeleePrefix()
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return player.ownedProjectileCounts[type] < 1;
        }
    
        public override void AddRecipes()
        {
            _ = CreateRecipe()
                .AddIngredient<CorrodedFossil>(10)
                .AddIngredient(ItemID.BoneWhip)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

    }
}
