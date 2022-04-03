using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Rogue
{
    public class StealthNimbusCloud : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/ShadeNimbusCloud";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nimbus");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 25;
            Projectile.Calamity().rogue = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (Projectile.ai[0] == 1f)
                texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/StealthNimbusCloud2");
            int height = texture.Height / Main.projFrames[Projectile.type];
            int frameHeight = height * Projectile.frame;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(0, frameHeight, texture.Width, height)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2((float)texture.Width / 2f, (float)height / 2f), Projectile.scale, spriteEffects, 0f);
            return false;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 3)
                {
                    Projectile.frame = 0;
                    return;
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.Center.X, Projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<StealthNimbus>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0], 0f);
            }
        }

        public override bool CanDamage() => false;
    }
}
