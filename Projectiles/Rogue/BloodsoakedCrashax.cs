using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Rogue
{
    public class BloodsoakedCrashax : ModProjectile
    {
        private int bounce = 3; //number of times it bounces
        private int grind = 0; //used to know when to slow down

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bloodsoaked Crasher");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.friendly = true;
            projectile.penetrate = 10;
            projectile.aiStyle = 2;
            projectile.timeLeft = 600; //10 seconds and counting
            aiType = ProjectileID.ThrowingKnife; //Throwing Knife AI
            projectile.Calamity().rogue = true;
            projectile.usesIDStaticNPCImmunity = true;
            projectile.idStaticNPCHitCooldown = 5;
            projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            grind--;
            if (grind <= 0)
            {
                grind = 0;
            }
            if (grind >= 1)
            {
                projectile.extraUpdates = 0; //stop, you're touching an enemy
                projectile.velocity.X *= 0.75f;
                projectile.velocity.Y *= 0.75f;
            }
            else
            {
                projectile.velocity.X *= 1.005f; //you broke up, time to yeet yourself out
                projectile.velocity.Y *= 1.005f;
                if (projectile.velocity.X > 16f)
                {
                    projectile.velocity.X = 16f;
                }
                if (projectile.velocity.Y > 16f)
                {
                    projectile.velocity.Y = 16f;
                }
                projectile.extraUpdates = 1;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bounce--;
            if (bounce <= 0)
            {
                projectile.Kill(); //you can only bounce so much 'til death
            }
            else
            {
                if (projectile.velocity.X != oldVelocity.X)
                {
                    projectile.velocity.X = -oldVelocity.X;
                }
                if (projectile.velocity.Y != oldVelocity.Y)
                {
                    projectile.velocity.Y = -oldVelocity.Y;
                }
            }
            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            grind += 5; //THE GRIND NEVER STOPS
            if (grind >= 10)
            {
                grind = 10;
            }
            Player player = Main.player[projectile.owner];
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
            if (projectile.ai[1] == 1f && projectile.owner == Main.myPlayer && grind >= 1) //stealth strike attack
            {
                int stealth = Projectile.NewProjectile(projectile.Center.X, projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<Blood>(), projectile.damage, projectile.knockBack, projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
                Main.projectile[stealth].Calamity().forceRogue = true;
            }
            if (!target.canGhostHeal) //canGhostHeal be like lol
            {
                return;
            }
            if (Main.rand.NextBool(2))
            {
                player.statLife += 1; //Trello said 2 hp per hit. Sounds like a fat balancing problem.
                player.HealEffect(1);
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit)
        {
            grind += 5; //THE GRIND NEVER STOPS
            if (grind >= 10)
            {
                grind = 10;
            }
            Player player = Main.player[projectile.owner];
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
            if (projectile.ai[1] == 1f && projectile.owner == Main.myPlayer && grind >= 1) //stealth strike attack
            {
                int stealth = Projectile.NewProjectile(projectile.Center.X, projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<Blood>(), projectile.damage, projectile.knockBack, projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
                Main.projectile[stealth].Calamity().forceRogue = true;
            }
            if (Main.rand.NextBool(2))
            {
                player.statLife += 1; //Trello said 2 hp per hit. Sounds like a fat balancing problem.
                player.HealEffect(1);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) //afterimages
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }
    }
}
