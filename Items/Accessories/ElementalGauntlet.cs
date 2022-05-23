﻿using CalamityMod.CalPlayer;
using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    [AutoloadEquip(new EquipType[] { EquipType.HandsOn, EquipType.HandsOff } )]
    public class ElementalGauntlet : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Elemental Gauntlet");
            Tooltip.SetDefault("Melee attacks and projectiles inflict on fire, frostburn and holy flames\n" +
                "15% increased melee speed, damage, and 5% increased melee critical strike chance\n" +
                "20% increased true melee damage\n" +
                "Temporary immunity to lava\n" +
                "Increased melee knockback\n" +
                "Enables auto swing for melee weapons\n" +
                "Increases the size of melee weapons");
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 38;
            Item.value = CalamityGlobalItem.Rarity14BuyPrice;
            Item.accessory = true;
            Item.Calamity().customRarity = CalamityRarity.DarkBlue;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.eGauntlet = true;
            player.autoReuseGlove = true;
            player.meleeScaleGlove = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.FireGauntlet).
                AddIngredient<YharimsInsignia>().
                AddIngredient(ItemID.LunarBar, 8).
                AddIngredient<GalacticaSingularity>(4).
                AddIngredient<AscendantSpiritEssence>(4).
                AddTile<CosmicAnvil>().
                Register();
        }
    }
}
