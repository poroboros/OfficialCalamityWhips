using CalamityMod.Buffs.Summon.Whips;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Summon
{
    public class UnderBiteProjectile : BaseWhipProjectile, ILocalizedModType
    {

        public new string LocalizationCategory => "Projectiles.Summon";
        
        #region Internal Variables

        private Texture2D activeTipTex => ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/UnderBiteProjectileTip", AssetRequestMode.ImmediateLoad).Value;
        private Texture2D inactiveTipTex => ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj", AssetRequestMode.ImmediateLoad).Value;
        
        bool hasSkull = true;

        float tipRot = 0;

        #endregion


        #region Whip Properties
        
        public override Color FishingLineColor => Color.SandyBrown;
        public override int DustAmount => 2;
        public override int? SwingDust => (int)Dusts.CalamityDusts.SulphurousSeaAcid;
        public override int? TagBuffID => null;
        public override SoundStyle? WhipCrackSound => SoundID.DD2_SkeletonHurt;
        public override float? MultihitModifier => .8f;
        
        public override Texture2D WhipTipTexture => hasSkull ? activeTipTex : inactiveTipTex;
        public override Texture2D WhipSegmentTexture => ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/UnderBiteProjectileSegment", AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D WhipHandleTexture => ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/UnderBiteProjectileHandle", AssetRequestMode.ImmediateLoad).Value;

        public override void SetWhipStats()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.WhipSettings.Segments = 20;
            Projectile.WhipSettings.RangeMultiplier = 1.1f;
        }

        #endregion

        #region Overridden Functions
        
        
        
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;

            Vector2 tip = GetTipPosition() ?? Vector2.Zero;
            owner.heldProj = Projectile.whoAmI;


            Vector2 diff = whipPoints[^2] + whipPoints[^1];
            
            tipRot = diff.ToRotation() + (MathHelper.PiOver2 / 6) * Projectile.spriteDirection;
            if (Timer >= swingTime * .65f && hasSkull)
            {
                int proj = Projectile.NewProjectile(Projectile.InheritSource(Projectile), tip, Projectile.velocity * 1.5f, ModContent.ProjectileType<UnderBiteSkull>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].rotation = tipRot;
                hasSkull = false;
            }
            
            base.AI();
        }

        #endregion
        
    }


    public class UnderBiteSkull : ModProjectile, ILocalizedModType
    {

        public new string LocalizationCategory => "Projectiles.Summon";
        private int frameSpeed = 5;

        //public override string Texture => this.GetPath().Replace("Orb", "") + "Projectile_Segment";


        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            //Projectile Stats
            Projectile.width = 34;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.aiStyle = 1;
            AIType = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Summon;
        }


        public override void AI()
        {

            //Animates the projectile's sprites
            

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (++Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = ++Projectile.frame % Main.projFrames[Projectile.type];
            }
            



        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
            target.AddBuff(ModContent.BuffType<UnderBiteWhipDebuff>(), 240);
            target.Calamity().UnderBiteSkullOffset = Projectile.Center - target.Center;
            // CatalystTag.ApplyTagDebuff(target, CatalystTagID.Underbite, 240);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, Projectile.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
            for (int i = 0; i < Main.rand.Next(3, 7); i++)
            {

                Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height,
                    (int)Dusts.CalamityDusts.SulphurousSeaAcid, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3),
                    25, default, .7f);

                int gore1 = Gore.NewGore(Projectile.InheritSource(Projectile), Projectile.position,
                    Projectile.velocity * 0.5f, Mod.Find<ModGore>("UnderbiteGore1").Type, 1f);
                Main.gore[gore1].timeLeft = 60;
                int gore2 = Gore.NewGore(Projectile.InheritSource(Projectile), Projectile.position,
                    Projectile.velocity * 0.5f, Mod.Find<ModGore>("UnderbiteGore2").Type, 1f);
                Main.gore[gore2].timeLeft = 60;
                int gore3 = Gore.NewGore(Projectile.InheritSource(Projectile), Projectile.position,
                    Projectile.velocity * 0.5f, Mod.Find<ModGore>("UnderbiteGore3").Type, 1f);
                Main.gore[gore3].timeLeft = 60;
            }

            return true;
        }
    }

}
