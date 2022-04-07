﻿using CalamityMod.Items.Weapons.Typeless;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace CalamityMod.Projectiles.Typeless
{
    public class AeroExplosive : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Typeless/AeroDynamite";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Aeroboom");
        }

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 2)
            {
                Projectile.damage = AeroDynamite.Damage; // Like most explosives, not boosted by damage boosts
                Projectile.knockBack = AeroDynamite.Knockback;
            }

            if (Projectile.timeLeft % 4 == 0 && Projectile.timeLeft < 270)
                Projectile.ai[0]++;
            Projectile.velocity *= 0.999f - Projectile.ai[0] * Main.rand.NextFloat(0.00075f, 0.00125f);
            Projectile.rotation += Projectile.velocity.Length() * 0.09f * Projectile.direction;

            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, 187, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 100, new Color(53, Main.DiscoG, 255));
            }
            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, 16, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            }

            if (Main.rand.NextBool())
            {
                int smoke = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1f);
                Main.dust[smoke].scale = 0.1f + Main.rand.NextFloat(0f, 0.5f);
                Main.dust[smoke].fadeIn = 1.5f + Main.rand.NextFloat(0f, 0.5f);
                Main.dust[smoke].noGravity = true;
                Main.dust[smoke].position = Projectile.Center + new Vector2(0f, -Projectile.height / 2f).RotatedBy(Projectile.rotation, default) * 1.1f;
                int fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 100, default, 1f);
                Main.dust[fire].scale = 1f + Main.rand.NextFloat(0f, 0.5f);
                Main.dust[fire].noGravity = true;
                Main.dust[fire].position = Projectile.Center + new Vector2(0f, -Projectile.height / 2f).RotatedBy(Projectile.rotation, default) * 1.1f;
            }
        }

        // Makes the projectile bounce infinitely, but lose a ton of speed on bounce.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X * 0.1f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y * 0.1f;
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            CalamityGlobalProjectile.ExpandHitboxBy(Projectile, 200);
            Projectile.maxPenetrate = Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int d = 0; d < 40; d++)
            {
                int smoke = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                Main.dust[smoke].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[smoke].scale = 0.5f;
                    Main.dust[smoke].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                }
            }
            for (int d = 0; d < 70; d++)
            {
                int fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 100, default, 3f);
                Main.dust[fire].noGravity = true;
                Main.dust[fire].velocity *= 5f;
                fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 100, default, 2f);
                Main.dust[fire].velocity *= 2f;
            }
            CalamityUtils.ExplosionGores(Projectile.Center, 3);

            CalamityGlobalProjectile.ExpandHitboxBy(Projectile, 15);

            if (Projectile.owner == Main.myPlayer)
            {
                CalamityUtils.ExplodeandDestroyTiles(Projectile, 7, true, new List<int>() { }, new List<int>() { });
            }
        }
    }
}
