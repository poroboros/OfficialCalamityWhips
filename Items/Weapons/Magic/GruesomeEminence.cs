﻿using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Magic
{
    public class GruesomeEminence : ModItem
    {
        public const string PoeticTooltipLine = "The spirits of the amalgam could never pass on to their desired afterlife,\n" +
            "Tainted and melded by rage as they were.";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gruesome Eminence");
            Tooltip.SetDefault("Summons a gaseous conglomeration of spirits near the cursor\n" +
                "At first, the spirits will fly wildly. This can hurt enemies and players\n" +
                "However, over time they will begin to accumulate to create a single, controllable monstrosity\n" +
                CalamityUtils.ColorMessage(PoeticTooltipLine, CalamityGlobalItem.ExhumedTooltipColor));
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 888;
            Item.DamageType = DamageClass.Magic;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.mana = 8;
            Item.width = 42;
            Item.height = 74;
            Item.useTime = 27;
            Item.useAnimation = 27;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.shootSpeed = 9f;
            Item.shoot = ModContent.ProjectileType<GruesomeEminenceHoldout>();

            Item.value = CalamityGlobalItem.Rarity15BuyPrice;
            Item.Calamity().customRarity = CalamityRarity.Violet;
        }

        // This weapon uses a holdout projectile.
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
