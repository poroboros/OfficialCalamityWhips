﻿using CalamityMod.Events;
using CalamityMod.World;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.SummonItems.Invasion
{
    public class CausticTear : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Caustic Tear");
            Tooltip.SetDefault("Toggles the acid rain in the Sulphurous Sea");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 18;
            item.maxStack = 20;
            item.rare = 6;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = 4;
            item.consumable = true;
        }

        public override bool UseItem(Player player)
        {
            if (!CalamityWorld.rainingAcid)
            {
                AcidRainEvent.TryStartEvent();
            }
            else
            {
                Main.invasionSize = 0;
                AcidRainEvent.UpdateInvasion();
            }
            return true;
        }
    }
}
