using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.BaseProjectiles
{
    /// <summary>
    /// Base class for a whip that handles drawing, AI, and onHit. To make a simple whip, you only need to specify stats in setWhipStats
    /// This was originally based off the ExampleMod implementation, but with several changes for ease of subclassing
    /// </summary>
    public abstract class BaseWhipProjectile : ModProjectile
    {

        #region Overridable Properties

        
        //Visual and SFX related variables
        public virtual Color FishingLineColor => Color.White;
        public virtual Color? DrawColor => Color.White;
        public virtual Color LightingColor => Color.Transparent;
        public virtual int? SwingDust => null;
        public virtual int DustAmount => 1;

        public virtual SoundStyle? WhipCrackSound => SoundID.Item153;


        
        //Textures are set to InvisibleProj by default, but this should be changed if you want it to be visible.
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public abstract Texture2D WhipTipTexture { get;}
        public abstract Texture2D WhipSegmentTexture { get;}
        public abstract Texture2D WhipHandleTexture { get; }

        
        //Tag related variables
        public virtual int? TagBuffID => null;
        public virtual int TagDuration => 240;
        
        
        //Gameplay variables
        public virtual float? MultihitModifier => .8f;

        #endregion

        internal List<Vector2> whipPoints = new List<Vector2>();
        internal float Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        
        internal Vector2? GetTipPosition()
        {
            
            if (whipPoints != null && whipPoints.Count > 2)
                return whipPoints[whipPoints.Count - 2];
            return null;
        }



        #region ModProjectile Functions

        public override void SetStaticDefaults()
        {
            // This makes the projectile use whip collision detection and allows flasks to be applied to it.>
            ProjectileID.Sets.IsAWhip[Type] = true;
        }
        

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true; // This prevents the projectile from hitting through solid tiles.
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            

            SetWhipStats();
        }

        
        public override bool PreAI()
        {
            
            if (Timer % 2 < .001)
            {
                whipPoints.Clear();
                Projectile.FillWhipControlPoints(Projectile, whipPoints);
            }
            return true;
        }
        
        public override void AI()
        {
            WhipAIMotion();
            WhipSFX(LightingColor, SwingDust, DustAmount, WhipCrackSound);
        }

        #endregion


        #region Virtual Functions

                /// <summary>
        /// Function is use to control custom whip stats, called in the parent class's set defaults
        /// </summary>
        public virtual void SetWhipStats()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.WhipSettings.Segments = 30;
            Projectile.WhipSettings.RangeMultiplier = 1f;
        }
        

        // This method draws a line between all points of the whip, in case there's empty space between the sprites.

        public override bool PreDraw(ref Color lightColor)
        {
            return DrawWhip(FishingLineColor);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            WhipOnHit(target);
        }

        /// <summary>
        /// Applies tag buff if there is one, applies multihit penalty, and focuses minions on target. 
        /// Called in OnHitNPC
        /// </summary>
        /// <param name="target"></param>
        public virtual void WhipOnHit(NPC target)
        {
            if (TagBuffID != null)
            {
                target.AddBuff((int)TagBuffID, TagDuration);
            }
            Projectile.damage = (int)(Projectile.damage * MultihitModifier);
            if (Projectile.damage < 1)
            {
                Projectile.damage = 1;
            }
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
        }

        /// <summary>
        /// Draws whip based on example mod, override if you want custom. 
        /// Called in PreDraw
        /// </summary>
        /// <param name="lineColor"> What color the fishing line is</param>
        /// <returns></returns>
        public virtual bool DrawWhip(Color lineColor)
        {
            //Gets every segment of the whip
            if (whipPoints == null || whipPoints.Count < 1)
                return false;

            CalamityUtils.DrawLineBetweenPoints(whipPoints, lineColor);

            SpriteEffects flip = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Main.instance.LoadProjectile(Type);

            //Load projectiles using file paths
            var texture = WhipHandleTexture;

            //Sets the frame which will be displayed
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;

            


            Vector2 pos = whipPoints[0];
            //Repeats for each whip point
            for (int i = 0; i < whipPoints.Count - 1; i++)
            {

                float scale = 1;

                //Tip of the whip
                if (i == whipPoints.Count - 2)
                {
                    //Sets image to tip texture
                    texture = WhipTipTexture;

                    //Moves the frame with the animation
                    sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                    origin = sourceRectangle.Size() / 2f;

                    // For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 0)
                {

                    //Sets image to segment texture
                    texture = WhipSegmentTexture;
                    //sets the frame accordingly
                    sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                    origin = sourceRectangle.Size() / 2f;


                }

                Vector2 element = whipPoints[i];
                Vector2 diff = whipPoints[i + 1] - element;
                
                float rotation = diff.ToRotation();

                //Rotate the handle
                if (i == 0)
                {
                    //diff.toRotation makes it follow the rotation of the whip anim
                    //MathHelper.Pi can be subtracted / added to adjust where the handle is
                    //Use multiplication as needed to tweak that
                    rotation = diff.ToRotation();
                }
                
                Color color = Lighting.GetColor(element.ToTileCoordinates());
                if (DrawColor != null) {
                
                    color = (Color)DrawColor;
                }

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, sourceRectangle, color, rotation, origin, scale, flip, 0);
                pos += diff;
            }
            return false;
        }

        bool runOnce = true;

        /// <summary>
        /// Runs whip AI similar to example mod, but the center is now on the whip tip. Called in AI
        /// </summary>
        public virtual void WhipAIMotion()
        {
            Player owner = Main.player[Projectile.owner];
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            if (runOnce)
            {
                Projectile.WhipSettings.Segments = (int)((owner.whipRangeMultiplier + 1) * Projectile.WhipSettings.Segments);
                runOnce = false;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // Without PiOver2, the rotation would be off by 90 degrees counterclockwise.





            Projectile.Center = Vector2.Lerp(Projectile.Center, whipPoints[whipPoints.Count - 1], 1);


            // Vanilla uses Vector2.Dot(Projectile.velocity, Vector2.UnitX) here. Dot Product returns the difference between two vectors, 0 meaning they are perpendicular.
            // However, the use of UnitX basically turns it into a more complicated way of checking if the projectile's velocity is above or equal to zero on the X axis.
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Timer++;


            if (Timer >= swingTime || owner.itemAnimation <= 0)
            {

                Projectile.Kill();
                return;
            }


        }

        /// <summary>
        /// Plays sound and runs dust, all the parameters should be set in whip stats, though you can override them. 
        /// Called in AI
        /// </summary>
        /// <param name="lightingCol"></param>
        /// <param name="dustID"></param>
        /// <param name="dustNum"></param>
        /// <param name="sound"></param>
        public virtual void WhipSFX(Color lightingCol, int? dustID, int dustNum, SoundStyle? sound)
        {
            Player owner = Main.player[Projectile.owner];
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            //Main.NewText(lightingCol);



            owner.heldProj = Projectile.whoAmI;
            Vector2? tip = GetTipPosition();
            
            if(tip is null)
                return;
            if (Timer == swingTime / 2 && sound != null)
            {
                // Plays a whipcrack sound at the tip of the whip.
                SoundEngine.PlaySound(sound, tip);

            }
            if ((Timer >= swingTime * .5f))
            {
                if (dustID != null)
                {
                    for (int i = 0; i < dustNum; i++)
                    {
                        Dust.NewDust((Vector2)tip, 2, 2, (int)dustID, 0, 0, Scale: .5f);
                    }
                }
                if (lightingCol != Color.Transparent)
                {
                    Lighting.AddLight((Vector2)tip, lightingCol.R / 255f, lightingCol.G / 255f, lightingCol.B / 255f);
                }

            }
        }

        #endregion
        
        

        
    }
}
