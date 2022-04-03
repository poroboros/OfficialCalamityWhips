using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;

namespace CalamityMod.Projectiles.Rogue
{
    public class SealedSingularityGore : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sealed Singularity Fragment");
        }

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = Projectile.height = 25;
            Projectile.Calamity().rogue = true;
            Projectile.timeLeft = 300;
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
            SoundEngine.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y);
            //Dust effect
            int splash = 0;
            while (splash < 4)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 31, -Projectile.velocity.X * 0.15f, -Projectile.velocity.Y * 0.10f, 150, default, 0.9f);
                splash += 1;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture;
            switch (Projectile.ai[0])
            {
                case 1f: texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/SealedSingularityGore2");
                         break;
                case 2f: texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/SealedSingularityGore3");
                         break;
                default: texture = ModContent.Request<Texture2D>(Texture).Value;
                         break;
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, new Rectangle?(new Rectangle(0, 0, texture.Width, texture.Height)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
