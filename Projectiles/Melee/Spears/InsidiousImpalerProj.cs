﻿using CalamityMod.Buffs.DamageOverTime;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Projectiles.BaseProjectiles;
namespace CalamityMod.Projectiles.Melee.Spears
{
    public class InsidiousImpalerProj : BaseSpearProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Insidious Impaler");
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.aiStyle = 19;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 90;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }

        public override float InitialSpeed => 3f;
        public override float ReelbackSpeed => 1.1f;
        public override float ForwardSpeed => 0.95f;
        public override Action<Projectile> EffectBeforeReelback => (proj) =>
        {
            Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), Projectile.Center + Projectile.velocity, Projectile.velocity * 3.5f, ModContent.ProjectileType<InsidiousHarpoon>(), (int)(Projectile.damage * 0.5), Projectile.knockBack * 0.85f, Projectile.owner);
        };

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 180);
            target.AddBuff(BuffID.Venom, 180);
        }
    }
}
