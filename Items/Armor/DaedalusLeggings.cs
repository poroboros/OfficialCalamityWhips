﻿using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class DaedalusLeggings : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Daedalus Leggings");
            Tooltip.SetDefault("3% increased critical strike chance\n" +
                "10% increased movement speed");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.defense = 15; //41
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance<GenericDamageClass>() += 3;
            player.moveSpeed += 0.1f;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<VerstaltiteBar>(10).
                AddIngredient(ItemID.CrystalShard, 4).
                AddIngredient<EssenceofEleum>(2).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
