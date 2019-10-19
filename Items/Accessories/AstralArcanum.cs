﻿using CalamityMod.CalPlayer;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityMod.Buffs.DamageOverTime;

namespace CalamityMod.Items.Accessories
{
    public class AstralArcanum : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Arcanum");
            Tooltip.SetDefault("Taking damage drops astral stars from the sky\n" +
                               "Provides immunity to the astral infection debuff\n" +
                               "You have a 5% chance to reflect projectiles when they hit you\n" +
                               "If this effect triggers you get healed for the projectile's damage\n" +
                               "Boosts life regen even while under the effects of a damaging debuff\n" +
                               "While under the effects of a damaging debuff you will gain 20 defense\n" +
                               "Press O to toggle teleportation UI");
        }

        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 26;
            item.value = Item.buyPrice(0, 90, 0, 0);
            item.accessory = true;
            item.Calamity().postMoonLordRarity = 13;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.astralArcanum = true;
            modPlayer.aBulwark = true;
            modPlayer.projRef = true;
            player.buffImmune[ModContent.BuffType<AstralInfectionDebuff>()] = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<CelestialJewel>());
            recipe.AddIngredient(ModContent.ItemType<AstralBulwark>());
            recipe.AddIngredient(ModContent.ItemType<ArcanumoftheVoid>());
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
