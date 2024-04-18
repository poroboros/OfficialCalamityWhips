﻿using CalamityMod.CalPlayer;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    [AutoloadEquip(EquipType.Shoes)]
    public class IronBoots : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AnechoicPlating>();
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.ironBoots = true;
        }
    }
}
