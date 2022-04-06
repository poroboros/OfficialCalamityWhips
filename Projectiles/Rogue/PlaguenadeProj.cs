﻿using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
namespace CalamityMod.Projectiles.Rogue
{
    public class PlaguenadeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/Plaguenade";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plaguenade");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 180;
            Projectile.Calamity().rogue = true;
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Projectile.velocity.X > -0.01f && Projectile.velocity.X < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = oldVelocity.X * -0.5f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 1f)
            {
                Projectile.velocity.Y = oldVelocity.Y * -0.5f;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Player player = Main.player[Projectile.owner];
                int projAmt = Projectile.Calamity().stealthStrike ? 28 : 20;
                for (int projIndex = 0; projIndex < projAmt; projIndex++)
                {
                    float speedX = (float)Main.rand.Next(-35, 36) * 0.02f;
                    float speedY = (float)Main.rand.Next(-35, 36) * 0.02f;
                    Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), Projectile.position.X, Projectile.position.Y, speedX, speedY, ModContent.ProjectileType<PlaguenadeBee>(), player.beeDamage(Projectile.damage), player.beeKB(0f), Projectile.owner, 0f, 0f);
                }
                SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
                if (Projectile.Calamity().stealthStrike)
                {
                    Projectile.position = Projectile.Center;
                    Projectile.width = Projectile.height = 75;
                }
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.maxPenetrate = -1;
                Projectile.penetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 10;
                Projectile.Damage();
                for (int i = 0; i < 10; i++)
                {
                    int smoke = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 31, 0f, 0f, 100, default, 2f);
                    Main.dust[smoke].velocity *= 3f;
                    if (Main.rand.NextBool(2))
                    {
                        Main.dust[smoke].scale = 0.5f;
                        Main.dust[smoke].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int j = 0; j < 15; j++)
                {
                    int plague = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 89, 0f, 0f, 100, default, 3f);
                    Main.dust[plague].noGravity = true;
                    Main.dust[plague].velocity *= 5f;
                    int fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 100, default, 2f);
                    Main.dust[fire].velocity *= 2f;
                }
                CalamityUtils.ExplosionGores(Projectile.Center, 3);
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 240);
        }

        public override void OnHitPvp(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 240);
        }
    }
}
