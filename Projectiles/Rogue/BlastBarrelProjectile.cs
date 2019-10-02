﻿using CalamityMod.Items.CalamityCustomThrowingDamage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityMod.Projectiles.Rogue
{
    public class BlastBarrelProjectile : ModProjectile
    {
        public float cooldown = 0f;
        public float oldVelocityX = 0f; 
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Barrel");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.friendly = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 480;
			projectile.GetGlobalProjectile<CalamityGlobalProjectile>(mod).rogue = true;
        }
        //Jesus christ, why isn't this in the Entity class instead of just NPC???
        //Negative check is so that it doesn't register a bounce as a collision
        public bool collideX => projectile.oldPosition.X == projectile.position.X;
        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                projectile.ai[1] = projectile.ai[0] == 0 ? 1 : 3;
                projectile.localAI[0] = 1f;
            }
            projectile.rotation += Math.Sign(projectile.velocity.X) * MathHelper.ToRadians(8f);
            if (projectile.velocity.Y < 15f)
            {
                projectile.velocity.Y += 0.3f;
            }
            if (collideX && cooldown == 0)
            {
                BounceEffects();
                projectile.velocity.X = -oldVelocityX;
            }
            else if (cooldown > 0)
            {
                cooldown -= 1f;
            }
            if (projectile.velocity.X != 0f)
            {
                oldVelocityX = Math.Sign(projectile.velocity.X) * 12f;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity) => false;
        public void BounceEffects()
        {
            int projectileCount = 12;
            //aka can bounce multiple times
            if (projectile.ai[0] != 0f)
            {
                projectileCount += (3 - (int)projectile.ai[0]) * 2; //more shit the closer we are to death
            }
            for (int i = 0; i < projectileCount; i++)
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 shrapnelVelocity = (Vector2.UnitY * (-16f + Main.rand.NextFloat(-3,12f))).RotatedByRandom((double)MathHelper.ToRadians(30f));
                    Projectile.NewProjectile(projectile.Center, projectile.velocity + shrapnelVelocity,
                        mod.ProjectileType("BarrelShrapnel"), BlastBarrel.BaseDamage, 3f, projectile.owner);
                }
                else
                {
                    Vector2 fireVelocity = (Vector2.UnitY * (-16f + Main.rand.NextFloat(-3, 12f))).RotatedByRandom((double)MathHelper.ToRadians(40f));
                    int fireIndex = Projectile.NewProjectile(projectile.Center, projectile.velocity + fireVelocity,
                        Main.rand.Next(ProjectileID.MolotovFire, ProjectileID.MolotovFire3 + 1), 
                        BlastBarrel.BaseDamage, 1f, projectile.owner);
                    Main.projectile[fireIndex].thrown = false;
                    Main.projectile[fireIndex].GetGlobalProjectile<CalamityGlobalProjectile>(mod).rogue = true;
                }
            }
            projectile.ai[1]--;
            cooldown = 15;
            if (projectile.ai[1] <= 0)
            {
                projectile.Kill();
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }
    }
}