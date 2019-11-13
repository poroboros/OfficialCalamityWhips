﻿using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Melee
{
    public class OmegaBiomeOrb : ModProjectile
    {
		private int dustType = 3;
		Color color = default;

		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Omega Biome Orb");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 20;
			projectile.aiStyle = 27;
			aiType = 156;
			projectile.friendly = true;
            projectile.penetrate = 1;
            projectile.timeLeft = 240;
            projectile.melee = true;
        }

		public override void AI()
		{
			Player player = Main.player[projectile.owner];
			bool jungle = player.ZoneJungle;
			bool snow = player.ZoneSnow;
			bool beach = player.ZoneBeach;
			bool corrupt = player.ZoneCorrupt;
			bool crimson = player.ZoneCrimson;
			bool dungeon = player.ZoneDungeon;
			bool desert = player.ZoneDesert;
			bool glow = player.ZoneGlowshroom;
			bool hell = player.ZoneUnderworldHeight;
			bool sky = player.ZoneSkyHeight;
			bool holy = player.ZoneHoly;
			bool nebula = player.ZoneTowerNebula;
			bool stardust = player.ZoneTowerStardust;
			bool solar = player.ZoneTowerSolar;
			bool vortex = player.ZoneTowerVortex;
			if (jungle)
			{
				dustType = 39;
				color = new Color(128, 255, 128, projectile.alpha);
			}
			else if (snow)
			{
				dustType = 51;
				color = new Color(128, 255, 255, projectile.alpha);
			}
			else if (beach)
			{
				dustType = 33;
				color = new Color(0, 0, 128, projectile.alpha);
			}
			else if (corrupt)
			{
				dustType = 14;
				color = new Color(128, 64, 255, projectile.alpha);
			}
			else if (crimson)
			{
				dustType = 5;
				color = new Color(128, 0, 0, projectile.alpha);
			}
			else if (dungeon)
			{
				dustType = 29;
				color = new Color(64, 0, 128, projectile.alpha);
			}
			else if (desert)
			{
				dustType = 32;
				color = new Color(255, 255, 128, projectile.alpha);
			}
			else if (glow)
			{
				dustType = 56;
				color = new Color(0, 255, 255, projectile.alpha);
			}
			else if (hell)
			{
				dustType = 6;
				color = new Color(255, 128, 0, projectile.alpha);
			}
			else if (sky)
			{
				dustType = 213;
				color = new Color(255, 255, 255, projectile.alpha);
			}
			else if (holy)
			{
				dustType = 57;
				color = new Color(255, 255, 0, projectile.alpha);
			}
			else if (nebula)
			{
				dustType = 242;
				color = new Color(255, 0, 255, projectile.alpha);
			}
			else if (stardust)
			{
				dustType = 206;
				color = new Color(0, 255, 255, projectile.alpha);
			}
			else if (solar)
			{
				dustType = 244;
				color = new Color(255, 128, 0, projectile.alpha);
			}
			else if (vortex)
			{
				dustType = 107;
				color = new Color(0, 255, 0, projectile.alpha);
			}
			else
			{
				color = new Color(0, 128, 0, projectile.alpha);
			}
			int num458 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, dustType, 0f, 0f, 100, default, 1.2f);
			Main.dust[num458].noGravity = true;
			Main.dust[num458].velocity *= 0.5f;
			Main.dust[num458].velocity += projectile.velocity * 0.1f;

			float num472 = projectile.Center.X;
			float num473 = projectile.Center.Y;
			float num474 = 400f;
			bool flag17 = false;
			for (int num475 = 0; num475 < 200; num475++)
			{
				if (Main.npc[num475].CanBeChasedBy(projectile, false) && Collision.CanHit(projectile.Center, 1, 1, Main.npc[num475].Center, 1, 1))
				{
					float num476 = Main.npc[num475].position.X + (float)(Main.npc[num475].width / 2);
					float num477 = Main.npc[num475].position.Y + (float)(Main.npc[num475].height / 2);
					float num478 = Math.Abs(projectile.position.X + (float)(projectile.width / 2) - num476) + Math.Abs(projectile.position.Y + (float)(projectile.height / 2) - num477);
					if (num478 < num474)
					{
						num474 = num478;
						num472 = num476;
						num473 = num477;
						flag17 = true;
					}
				}
			}
			if (flag17)
			{
				float num483 = 15f;
				Vector2 vector35 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
				float num484 = num472 - vector35.X;
				float num485 = num473 - vector35.Y;
				float num486 = (float)Math.Sqrt((double)(num484 * num484 + num485 * num485));
				num486 = num483 / num486;
				num484 *= num486;
				num485 *= num486;
				projectile.velocity.X = (projectile.velocity.X * 20f + num484) / 21f;
				projectile.velocity.Y = (projectile.velocity.Y * 20f + num485) / 21f;
			}
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return color;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if (projectile.timeLeft > 235)
				return false;

			CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.Item10, projectile.position);
			int num3;
			for (int num795 = 4; num795 < 31; num795 = num3 + 1)
			{
				float num796 = projectile.oldVelocity.X * (30f / (float)num795);
				float num797 = projectile.oldVelocity.Y * (30f / (float)num795);
				int num798 = Dust.NewDust(new Vector2(projectile.oldPosition.X - num796, projectile.oldPosition.Y - num797), 8, 8, dustType, projectile.oldVelocity.X, projectile.oldVelocity.Y, 100, default, 1.8f);
				Main.dust[num798].noGravity = true;
				Dust dust = Main.dust[num798];
				dust.velocity *= 0.5f;
				num798 = Dust.NewDust(new Vector2(projectile.oldPosition.X - num796, projectile.oldPosition.Y - num797), 8, 8, dustType, projectile.oldVelocity.X, projectile.oldVelocity.Y, 100, default, 1.4f);
				dust = Main.dust[num798];
				dust.velocity *= 0.05f;
				num3 = num795;
			}
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Player player = Main.player[projectile.owner];
            bool jungle = player.ZoneJungle;
            bool snow = player.ZoneSnow;
            bool beach = player.ZoneBeach;
            bool dungeon = player.ZoneDungeon;
            bool desert = player.ZoneDesert;
            bool glow = player.ZoneGlowshroom;
            bool hell = player.ZoneUnderworldHeight;
            bool holy = player.ZoneHoly;
            bool bloodMoon = Main.bloodMoon;
            bool snowMoon = Main.snowMoon;
            bool pumpkinMoon = Main.pumpkinMoon;
            if (bloodMoon)
            {
                player.AddBuff(BuffID.Battle, 600);
            }
            if (snowMoon)
            {
                player.AddBuff(BuffID.RapidHealing, 600);
            }
            if (pumpkinMoon)
            {
                player.AddBuff(BuffID.WellFed, 600);
            }
            if (jungle)
            {
                target.AddBuff(ModContent.BuffType<Plague>(), 600);
            }
            else if (snow)
            {
                target.AddBuff(ModContent.BuffType<GlacialState>(), 600);
            }
            else if (beach)
            {
                target.AddBuff(ModContent.BuffType<CrushDepth>(), 600);
            }
            else if (dungeon)
            {
                target.AddBuff(BuffID.Frostburn, 600);
            }
            else if (desert)
            {
                target.AddBuff(ModContent.BuffType<HolyFlames>(), 600);
            }
            else if (glow)
            {
                target.AddBuff(ModContent.BuffType<TemporalSadness>(), 600);
            }
            else if (hell)
            {
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 600);
            }
            else if (holy)
            {
                target.AddBuff(ModContent.BuffType<HolyFlames>(), 600);
            }
            else
            {
                target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 600);
            }
        }
    }
}
