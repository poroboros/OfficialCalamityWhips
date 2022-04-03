using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Summon
{
    public class PolypLauncherShrapnel : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Polyp Shrapnel");
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 13;
            Projectile.height = 13;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
        }

        public override void AI()
        {
            //Rotation and gravity
            Projectile.rotation += 0.6f * Projectile.direction;
            Projectile.velocity.Y += 0.27f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
        }

        public override void Kill(int timeLeft)
        {
            //Dust effect
            int splash = 0;
            while (splash < 4)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 225, -Projectile.velocity.X * 0.15f, -Projectile.velocity.Y * 0.10f, 150, default, 0.7f);
                splash += 1;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Changes the texture of the projectile
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (Projectile.ai[0] == 1f)
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/PolypLauncherShrapnel2");
            }
            if (Projectile.ai[0] == 2f)
            {
                texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/PolypLauncherShrapnel3");
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, new Rectangle?(new Rectangle(0, 0, texture.Width, texture.Height)), Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
