
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;

namespace CalamityMod.Projectiles.Rogue
{
    public class MetalShard : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Metal Shard");
        }

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.Calamity().rogue = true;
            Projectile.penetrate = 10;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void AI()
        {
            //Rotation
            if (Projectile.ai[0] == 0f)
                Projectile.rotation += 0.1f;
            //Gravity
            Projectile.velocity.Y += 0.1f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
            //Sticky Behaviour
            Projectile.StickyProjAI(15);
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Projectile.ModifyHitNPCSticky(8 , true);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
            {
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
            }
            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture;
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Main.rand.Next(1, 4);
            switch (Projectile.localAI[1])
            {

                case 2f: texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/MetalShard2");
                         break;
                case 3f: texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/MetalShard3");
                         break;
                default: texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/MetalShard");
                         break;
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, new Rectangle?(new Rectangle(0, 0, texture.Width, texture.Height)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.Lead, 0f, 0f, 0, default, 1f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            return true;
        }
    }
}
