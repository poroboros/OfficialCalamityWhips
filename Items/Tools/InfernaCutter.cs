using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
namespace CalamityMod.Items.Tools
{
    public class InfernaCutter : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Inferna Cutter");
            Tooltip.SetDefault("Critical hits with the blade cause small explosions\n" +
                "Generates a number of small sparks when swung");
        }

        public override void SetDefaults()
        {
            item.damage = 85;
            item.melee = true;
            item.width = 60;
            item.height = 46;
            item.useTime = 16;
            item.useAnimation = 16;
            item.useTurn = true;
            item.axe = 27;
            item.useStyle = 1;
            item.knockBack = 7f;
            item.value = Item.buyPrice(0, 36, 0, 0);
            item.rare = 5;
            item.UseSound = SoundID.Item1;
            item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(null, "PurityAxe");
            recipe.AddIngredient(ItemID.SoulofFright, 8);
            recipe.AddIngredient(null, "EssenceofChaos", 3);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.1) ||
                    player.itemAnimation == (int)((double)player.itemAnimationMax * 0.3) ||
                    player.itemAnimation == (int)((double)player.itemAnimationMax * 0.5) ||
                    player.itemAnimation == (int)((double)player.itemAnimationMax * 0.7) ||
                    player.itemAnimation == (int)((double)player.itemAnimationMax * 0.9))
                {
                    float num339 = 0f;
                    float num340 = 0f;
                    float num341 = 0f;
                    float num342 = 0f;
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.9))
                    {
                        num339 = -7f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.7))
                    {
                        num339 = -6f;
                        num340 = 2f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.5))
                    {
                        num339 = -4f;
                        num340 = 4f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.3))
                    {
                        num339 = -2f;
                        num340 = 6f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.1))
                    {
                        num340 = 7f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.7))
                    {
                        num342 = 26f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.3))
                    {
                        num342 -= 4f;
                        num341 -= 20f;
                    }
                    if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.1))
                    {
                        num341 += 6f;
                    }
                    if (player.direction == -1)
                    {
                        if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.9))
                        {
                            num342 -= 8f;
                        }
                        if (player.itemAnimation == (int)((double)player.itemAnimationMax * 0.7))
                        {
                            num342 -= 6f;
                        }
                    }
                    num339 *= 1.5f;
                    num340 *= 1.5f;
                    num342 *= (float)player.direction;
                    num341 *= player.gravDir;
                    Projectile.NewProjectile((float)(hitbox.X + hitbox.Width / 2) + num342, (float)(hitbox.Y + hitbox.Height / 2) + num341, (float)player.direction * num340, num339 * player.gravDir, ProjectileID.Spark, (int)((float)item.damage * 0.1f * player.meleeDamage), 0f, player.whoAmI, 0f, 0f);
                }
            }
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 6);
            }
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockback, bool crit)
        {
            if (crit)
            {
                int boom = Projectile.NewProjectile(target.Center.X, target.Center.Y, 0f, 0f, ModContent.ProjectileType<FuckYou>(), (int)((float)item.damage * player.meleeDamage), knockback, player.whoAmI, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
                Main.projectile[boom].Calamity().forceMelee = true;
            }
            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
        }
    }
}
