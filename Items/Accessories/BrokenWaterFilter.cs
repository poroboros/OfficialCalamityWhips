using CalamityMod.CalPlayer;
using Terraria;
using Terraria.ModLoader;
namespace CalamityMod.Items.Accessories
{
    public class BrokenWaterFilter : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Broken Water Filter");
            Tooltip.SetDefault("Disables natural Acid Rain spawns");
        }
        public override void SetDefaults()
        {
            item.width = 32;
            item.height = 34;
            item.value = CalamityGlobalItem.Rarity1BuyPrice;
            item.rare = 1;
            item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.noStupidNaturalARSpawns = true;
        }
    }
}
