using CalamityMod.CalPlayer;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityMod.Items.Accessories
{
    public class CheatTestThing : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("lul");
            Tooltip.SetDefault("Instantly kills you...\n" +
                "Unless...?"); //This is mainly for the wiki, blank tooltips on accessories is bad.
        }

        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 26;
            item.value = 0; // lul intentionally has zero value
            item.Calamity().customRarity = CalamityRarity.HotPink;
            item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            bool canUse = player.name == "Fabsol" || player.name == "Totalbiscuit" || player.name == "TotalBiscuit" || player.name == "Total Biscuit" || player.name == "Total biscuit";
            if (canUse)
            {
                modPlayer.lol = true;
            }
            else if (!player.immune)
            {
                player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " isn't worthy."), 1000.0, 0, false);
            }
        }
    }
}
