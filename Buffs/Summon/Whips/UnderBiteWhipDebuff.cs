using System;
using System.Linq;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Buffs.Summon.Whips
{
    public class UnderBiteWhipDebuff : ModBuff
    {
        public override string Texture => "CalamityMod/Buffs/Summon/Whips/SentinalLash";

        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            var whipBuffs = new int[]
            {
                BuffID.BlandWhipEnemyDebuff, BuffID.FlameWhipEnemyDebuff, BuffID.BoneWhipNPCDebuff,
                BuffID.ScytheWhipEnemyDebuff, BuffID.CoolWhipNPCDebuff, BuffID.MaceWhipNPCDebuff,
                BuffID.RainbowWhipNPCDebuff, BuffID.SwordWhipNPCDebuff, BuffID.ThornWhipNPCDebuff
            };
            
            if (npc.Calamity().underbiteTag < npc.buffTime[buffIndex])
                npc.Calamity().underbiteTag = npc.buffTime[buffIndex];

            //kill whip stacking for psc purposes
            // 29SEP2023: Ozzatron: this won't kill stacking with other mod whips. need a generalized system for this
            for (int buff = 0; buff < NPC.maxBuffs; buff++)
            {
                int buffID = npc.buffType[buff];
                if (npc.buffTime[buff] > 0 && whipBuffs.Contains(buffID) && npc.buffType[buff] != Type)
                    npc.RequestBuffRemoval(npc.buffType[buff]);
            }
        }

        internal static void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D chomper = TextureAssets.Projectile[ModContent.ProjectileType<UnderBiteSkull>()].Value;

            int frameHeight = chomper.Height / Main.projFrames[ModContent.ProjectileType<UnderBiteSkull>()];

            int frame = (npc.Calamity().underbiteTag/5) % 3;
            int startY = frameHeight * frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, chomper.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            float scale = 1;

            SpriteEffects flip = npc.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            

            Vector2 chomperOffset = npc.Calamity().UnderBiteSkullOffset;
            float rotation = chomperOffset.ToRotation() + MathF.PI;

            spriteBatch.Draw(chomper, npc.Center + (chomperOffset / 2) - screenPos, sourceRectangle, drawColor, rotation, origin, scale, SpriteEffects.None, 0);
            
        }
    }
}
