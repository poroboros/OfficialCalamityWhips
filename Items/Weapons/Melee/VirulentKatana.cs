using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Melee
{
    public class VirulentKatana : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Virulence");
            Tooltip.SetDefault("Fires a plague cloud");
        }

        public override void SetDefaults()
        {
            item.width = 48;
            item.damage = 96;
            item.melee = true;
            item.useAnimation = 15;
            item.useStyle = 1;
            item.useTime = 15;
            item.useTurn = true;
            item.knockBack = 5.5f;
            item.UseSound = SoundID.Item1;
            item.autoReuse = true;
            item.height = 62;
            item.value = Item.buyPrice(0, 80, 0, 0);
            item.rare = 8;
            item.shoot = ModContent.ProjectileType<PlagueDust>();
            item.shootSpeed = 9f;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 240);
        }

        public override void OnHitPvp(Player player, Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 240);
        }
    }
}
