﻿using Terraria.DataStructures;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Rogue
{
    public class LeadTomahawk : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lead Tomahawk");
            Tooltip.SetDefault("The tomahawks have more damage for a short time when initially thrown\n" +
                               "Stealth strikes pierce up to 5 times");
            SacrificeTotal = 99;
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.damage = 14;
            Item.noMelee = true;
            Item.consumable = true;
            Item.noUseGraphic = true;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.knockBack = 1f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.height = 36;
            Item.maxStack = 999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
            Item.shoot = ModContent.ProjectileType<LeadTomahawkProj>();
            Item.shootSpeed = 12f;
            Item.DamageType = RogueDamageClass.Instance;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.Calamity().StealthStrikeAvailable())
            {
                int p = Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, player.whoAmI, 0f, 1f);
                if (p.WithinBounds(Main.maxProjectiles))
				{
                    Main.projectile[p].Calamity().stealthStrike = true;
                    Main.projectile[p].penetrate = 5;
				}
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe(100).
                AddIngredient(ItemID.Wood).
                AddIngredient(ItemID.LeadBar).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
