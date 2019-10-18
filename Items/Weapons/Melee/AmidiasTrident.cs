using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace CalamityMod.Items.Weapons.Melee
{
    public class AmidiasTrident : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Amidias' Trident");
            Tooltip.SetDefault("Shoots homing whirlpools");
        }

        public override void SetDefaults()
        {
            item.width = 44;
            item.damage = 12;
            item.melee = true;
            item.noMelee = true;
            item.useTurn = true;
            item.noUseGraphic = true;
            item.useAnimation = 17;
            item.useStyle = 5;
            item.useTime = 17;
            item.knockBack = 4.5f;
            item.UseSound = SoundID.Item1;
            item.autoReuse = true;
            item.height = 44;
            item.value = Item.buyPrice(0, 2, 0, 0);
            item.rare = 2;
            item.shoot = ModContent.ProjectileType<AmidiasTridentProj>();
            item.shootSpeed = 6f;
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < 1000; ++i)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == Main.myPlayer && Main.projectile[i].type == item.shoot)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
