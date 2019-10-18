using CalamityMod.CalPlayer;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
namespace CalamityMod.Items
{
    [AutoloadEquip(EquipType.Head)]
    public class MolluskShellmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mollusk Shellmet");
            Tooltip.SetDefault("5% increased damage and 4% increased critical strike chance\n" +
                               "You can move freely through liquids");
        }

        public override void SetDefaults()
        {
            item.width = 22;
            item.height = 22;
            item.value = Item.buyPrice(0, 25, 0, 0);
            item.rare = 5;
            item.defense = 18;
        }

        public override void UpdateEquip(Player player)
        {
            player.ignoreWater = true;
            player.allDamage += 0.05f;
            player.Calamity().AllCritBoost(4);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MolluskShellplate>() && legs.type == ModContent.ItemType<MolluskShelleggings>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "Two shellfishes aid you in combat\n" +
                              "10% increased damage\n" +
                              "Your horizontal movement is slowed";
            CalamityPlayer modPlayer = player.Calamity();
            player.allDamage += 0.1f;
            modPlayer.molluskSet = true;
            player.maxMinions += 4;
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.FindBuffIndex(ModContent.BuffType<ShellfishBuff>()) == -1)
                {
                    player.AddBuff(ModContent.BuffType<ShellfishBuff>(), 3600, true);
                }
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Shellfish>()] < 2)
                {
                    Projectile.NewProjectile(player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Shellfish>(), (int)((double)1500 * player.minionDamage), 0f, Main.myPlayer, 0f, 0f);
                }
            }
            player.Calamity().wearingRogueArmor = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<SeaPrism>(), 15);
            recipe.AddIngredient(ModContent.ItemType<MolluskHusk>(), 6);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
