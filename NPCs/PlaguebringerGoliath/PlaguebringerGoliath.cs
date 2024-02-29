﻿using System;
using System.IO;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor.Vanity;
using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Pets;
using CalamityMod.Items.Placeables.Furniture.BossRelics;
using CalamityMod.Items.Placeables.Furniture.DevPaintings;
using CalamityMod.Items.Placeables.Furniture.Trophies;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.PlaguebringerGoliath
{
    [AutoloadBossHead]
    public class PlaguebringerGoliath : ModNPC
    {
        private int biomeEnrageTimer = CalamityGlobalNPC.biomeEnrageTimerMax;
        private const float MissileAngleSpread = 60;
        private const int MissileProjectiles = 8;
        private int MissileCountdown = 0;
        private int despawnTimer = 120;
        private int chargeDistance = 0;
        private bool charging = false;
        private bool halfLife = false;
        private bool canDespawn = false;
        private bool flyingFrame2 = false;
        private int curTex = 1;

        public static readonly SoundStyle NukeWarningSound = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGNukeWarning");
        public static readonly SoundStyle AttackSwitchSound = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitch", 2);
        public static readonly SoundStyle DashSound = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGDash");
        public static readonly SoundStyle BarrageLaunchSound = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGBarrageLaunch");

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.4f,
                PortraitScale = 0.5f,
                PortraitPositionXOverride = -56f,
                PortraitPositionYOverride = -8f,
                SpriteDirection = -1
            };
            value.Position.X -= 48f;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.GetNPCDamage();
            NPC.npcSlots = 64f;
            NPC.width = 198;
            NPC.height = 198;
            NPC.defense = 50;
            NPC.DR_NERD(0.3f);
            NPC.LifeMaxNERB(87500, 105000, 370000);
            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.boss = true;
            NPC.value = Item.buyPrice(0, 75, 0, 0);
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.Calamity().VulnerableToSickness = false;
            NPC.Calamity().VulnerableToElectricity = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Jungle,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundJungle,
                new FlavorTextBestiaryInfoElement("Mods.CalamityMod.Bestiary.PlaguebringerGoliath")
            });
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(biomeEnrageTimer);
            writer.Write(halfLife);
            writer.Write(canDespawn);
            writer.Write(flyingFrame2);
            writer.Write(MissileCountdown);
            writer.Write(despawnTimer);
            writer.Write(chargeDistance);
            writer.Write(charging);
            for (int i = 0; i < 4; i++)
                writer.Write(NPC.Calamity().newAI[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            biomeEnrageTimer = reader.ReadInt32();
            halfLife = reader.ReadBoolean();
            canDespawn = reader.ReadBoolean();
            flyingFrame2 = reader.ReadBoolean();
            MissileCountdown = reader.ReadInt32();
            despawnTimer = reader.ReadInt32();
            chargeDistance = reader.ReadInt32();
            charging = reader.ReadBoolean();
            for (int i = 0; i < 4; i++)
                NPC.Calamity().newAI[i] = reader.ReadSingle();
        }

        public override void AI()
        {
            CalamityGlobalNPC calamityGlobalNPC = NPC.Calamity();

            // Drawcode adjustments for the new sprite
            NPC.gfxOffY = charging ? -40 : -50;
            NPC.width = NPC.frame.Width / 2;
            NPC.height = (int)(NPC.frame.Height * (charging ? 1.5f : 1.8f));

            // Mode variables
            bool bossRush = BossRushEvent.BossRushActive;
            bool death = CalamityWorld.death || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;
            bool expertMode = Main.expertMode || bossRush;

            // Percent life remaining
            float lifeRatio = NPC.life / (float)NPC.lifeMax;

            // Phases
            bool phase2 = lifeRatio < 0.75f;
            bool phase3 = lifeRatio < 0.5f;
            bool phase4 = lifeRatio < 0.25f;
            bool phase5 = lifeRatio < 0.1f;

            // Adjusts how 'challenging' the projectiles and enemies are to deal with
            float challengeAmt = (1f - lifeRatio) * 100f;
            float nukeBarrageChallengeAmt = (0.5f - lifeRatio) * 200f;

            if (Main.getGoodWorld)
            {
                challengeAmt *= 1.5f;
                nukeBarrageChallengeAmt *= 1.5f;
            }

            // Adjust slowing debuff immunity
            bool immuneToSlowingDebuffs = NPC.ai[0] == 0f || NPC.ai[0] == 4f;
            NPC.buffImmune[ModContent.BuffType<GlacialState>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<TemporalSadness>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<KamiFlu>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<Eutrophication>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<TimeDistortion>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<GalvanicCorrosion>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<Vaporfied>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[BuffID.Slow] = immuneToSlowingDebuffs;
            NPC.buffImmune[BuffID.Webbed] = immuneToSlowingDebuffs;

            // Light
            Lighting.AddLight((int)((NPC.position.X + (NPC.width / 2)) / 16f), (int)((NPC.position.Y + (NPC.height / 2)) / 16f), 0.3f, 0.7f, 0f);

            // Show message
            if (!halfLife && phase3 && expertMode)
            {
                string key = "Mods.CalamityMod.Status.Boss.PlagueBossText";
                Color messageColor = Color.Lime;
                CalamityUtils.DisplayLocalizedText(key, messageColor);
                SoundEngine.PlaySound(NukeWarningSound, NPC.Center);

                halfLife = true;
            }

            // Missile countdown
            if (halfLife && MissileCountdown == 0)
                MissileCountdown = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 300 : 600;
            if (MissileCountdown > 1)
                MissileCountdown--;

            Vector2 vectorCenter = NPC.Center;

            // Count nearby players
            int activePlayers = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead && (vectorCenter - Main.player[i].Center).Length() < 1000f)
                    activePlayers++;
            }

            // Get a target
            if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            // Despawn safety, make sure to target another player if the current player target is too far away
            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
                NPC.TargetClosest();

            Player player = Main.player[NPC.target];

            // Distance from target
            Vector2 distFromPlayer = player.Center - vectorCenter;

            // Enrage
            if (!player.ZoneJungle && !bossRush)
            {
                if (biomeEnrageTimer > 0)
                    biomeEnrageTimer--;
            }
            else
                biomeEnrageTimer = CalamityGlobalNPC.biomeEnrageTimerMax;

            bool biomeEnraged = biomeEnrageTimer <= 0 || bossRush;

            float enrageScale = death ? 0.5f : 0f;
            if (biomeEnraged)
            {
                NPC.Calamity().CurrentlyEnraged = !bossRush;
                enrageScale += 1.5f;
            }

            if (enrageScale > 1.5f)
                enrageScale = 1.5f;

            if (Main.getGoodWorld)
                enrageScale += 0.5f;

            if (bossRush)
                enrageScale = 2f;

            bool diagonalDash = (revenge && phase2) || bossRush;

            if (NPC.ai[0] != 0f && NPC.ai[0] != 4f)
                NPC.rotation = NPC.velocity.X * 0.02f;

            // Despawn
            if (!player.active || player.dead || Vector2.Distance(player.Center, vectorCenter) > 5600f)
            {
                NPC.TargetClosest(false);
                player = Main.player[NPC.target];
                if (!player.active || player.dead || Vector2.Distance(player.Center, vectorCenter) > 5600f)
                {
                    if (despawnTimer > 0)
                        despawnTimer--;
                }
            }
            else
                despawnTimer = 120;

            canDespawn = despawnTimer <= 0;
            if (canDespawn)
            {
                // Avoid cheap bullshit
                NPC.damage = 0;

                if (NPC.velocity.Y > 3f)
                    NPC.velocity.Y = 3f;
                NPC.velocity.Y -= 0.2f;
                if (NPC.velocity.Y < -16f)
                    NPC.velocity.Y = -16f;

                if (NPC.timeLeft > 60)
                    NPC.timeLeft = 60;

                if (NPC.ai[0] != -1f)
                {
                    NPC.ai[0] = -1f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    MissileCountdown = 0;
                    chargeDistance = 0;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Always start in enemy spawning phase
            if (calamityGlobalNPC.newAI[3] == 0f)
            {
                calamityGlobalNPC.newAI[3] = 1f;
                NPC.ai[0] = 2f;
                NPC.netUpdate = true;
            }

            // Phase switch
            if (NPC.ai[0] == -1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int attackSwitch;
                    do attackSwitch = MissileCountdown == 1 ? 4 : Main.rand.Next(4);
                    while (attackSwitch == NPC.ai[1] || attackSwitch == 1);

                    if (attackSwitch == 0 && diagonalDash && distFromPlayer.Length() < 1800f)
                    {
                        do
                        {
                            switch (Main.rand.Next(3))
                            {
                                case 0:
                                    chargeDistance = 0;
                                    break;
                                case 1:
                                    chargeDistance = 400;
                                    break;
                                case 2:
                                    chargeDistance = -400;
                                    break;
                            }
                        }
                        while (chargeDistance == NPC.ai[3]);

                        NPC.ai[3] = -chargeDistance;
                    }
                    NPC.ai[0] = attackSwitch;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.TargetClosest();
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;

                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);
                }
            }

            // Charge phase
            else if (NPC.ai[0] == 0f)
            {
                float chargeSpeed = revenge ? 28f : 26f;
                if (phase2)
                    chargeSpeed += 1f;
                if (phase3)
                    chargeSpeed += 1f;
                if (phase4)
                    chargeSpeed += 1f;
                if (phase5)
                    chargeSpeed += 1f;

                chargeSpeed += 2f * enrageScale;

                int phaseSwitchTimer = (int)Math.Ceiling(2f + enrageScale);
                if ((NPC.ai[1] > (2 * phaseSwitchTimer) && NPC.ai[1] % 2f == 0f) || distFromPlayer.Length() > 1800f)
                {
                    NPC.ai[0] = -1f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;

                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);

                    return;
                }

                // Charge
                if (NPC.ai[1] % 2f == 0f)
                {
                    // Avoid cheap bullshit
                    NPC.damage = 0;

                    float playerLocation = vectorCenter.X - player.Center.X;

                    float chargeThresholdY = 20f;
                    chargeThresholdY += 20f * enrageScale;

                    if (Math.Abs(NPC.Center.Y - (player.Center.Y - chargeDistance)) < chargeThresholdY)
                    {
                        // Set damage
                        NPC.damage = NPC.defDamage;

                        if (diagonalDash)
                        {
                            switch (Main.rand.Next(3))
                            {
                                case 0:
                                    chargeDistance = 0;
                                    break;
                                case 1:
                                    chargeDistance = 400;
                                    break;
                                case 2:
                                    chargeDistance = -400;
                                    break;
                            }
                        }

                        charging = true;
                        NPC.frameCounter = 4;

                        NPC.ai[1] += 1f;
                        NPC.ai[2] = 0f;

                        float targetX = player.position.X + (player.width / 2) - vectorCenter.X;
                        float targetY = player.position.Y + (player.height / 2) - vectorCenter.Y;
                        float targetDistance = (float)Math.Sqrt(targetX * targetX + targetY * targetY);

                        targetDistance = chargeSpeed / targetDistance;
                        NPC.velocity.X = targetX * targetDistance;
                        NPC.velocity.Y = targetY * targetDistance;
                        NPC.rotation = (float)Math.Atan2(NPC.velocity.Y, NPC.velocity.X);

                        calamityGlobalNPC.newAI[1] = NPC.velocity.X;
                        calamityGlobalNPC.newAI[2] = NPC.velocity.Y;

                        NPC.direction = playerLocation < 0 ? 1 : -1;
                        NPC.spriteDirection = NPC.direction;
                        if (NPC.spriteDirection != 1)
                            NPC.rotation += (float)Math.PI;

                        NPC.netUpdate = true;
                        NPC.netSpam -= 5;

                        SoundEngine.PlaySound(DashSound, NPC.Center);
                        return;
                    }

                    NPC.rotation = NPC.velocity.X * 0.02f;
                    charging = false;

                    float maxLineUpSpeedY = revenge ? 14f : 12f;
                    float lineUpAccelY = revenge ? 0.25f : 0.22f;
                    if (phase2)
                    {
                        maxLineUpSpeedY += 1f;
                        lineUpAccelY += 0.05f;
                    }
                    if (phase4)
                    {
                        maxLineUpSpeedY += 1f;
                        lineUpAccelY += 0.05f;
                    }
                    maxLineUpSpeedY += 1.5f * enrageScale;
                    lineUpAccelY += 0.25f * enrageScale;

                    if (vectorCenter.Y < (player.Center.Y - chargeDistance))
                        NPC.velocity.Y += lineUpAccelY;
                    else
                        NPC.velocity.Y -= lineUpAccelY;

                    if (NPC.velocity.Y < -maxLineUpSpeedY)
                        NPC.velocity.Y = -maxLineUpSpeedY;
                    if (NPC.velocity.Y > maxLineUpSpeedY)
                        NPC.velocity.Y = maxLineUpSpeedY;

                    if (Math.Abs(vectorCenter.X - player.Center.X) > 650f)
                        NPC.velocity.X += lineUpAccelY * NPC.direction;
                    else if (Math.Abs(vectorCenter.X - player.Center.X) < 500f)
                        NPC.velocity.X -= lineUpAccelY * NPC.direction;
                    else
                        NPC.velocity.X *= 0.8f;

                    if (NPC.velocity.X < -maxLineUpSpeedY)
                        NPC.velocity.X = -maxLineUpSpeedY;
                    if (NPC.velocity.X > maxLineUpSpeedY)
                        NPC.velocity.X = maxLineUpSpeedY;

                    NPC.direction = playerLocation < 0 ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;

                    NPC.netUpdate = true;
                    NPC.netSpam -= 5;
                }

                // Slow down after charge
                else
                {
                    // Set damage
                    NPC.damage = NPC.defDamage;

                    if (NPC.velocity.X < 0f)
                        NPC.direction = -1;
                    else
                        NPC.direction = 1;

                    NPC.spriteDirection = NPC.direction;

                    int stopChargeXDist = revenge ? 525 : 550;
                    if (phase4)
                        stopChargeXDist = revenge ? 450 : 475;
                    else if (phase3)
                        stopChargeXDist = revenge ? 475 : 500;
                    else if (phase2)
                        stopChargeXDist = revenge ? 500 : 525;
                    stopChargeXDist -= (int)(25f * enrageScale);

                    int chargeDirectionXSign = 1;
                    if (vectorCenter.X < player.Center.X)
                        chargeDirectionXSign = -1;

                    if (NPC.direction == chargeDirectionXSign && (Math.Abs(vectorCenter.X - player.Center.X) > stopChargeXDist || Math.Abs(vectorCenter.Y - player.Center.Y) > stopChargeXDist))
                        NPC.ai[2] = 1f;

                    if (enrageScale > 0 && NPC.ai[2] == 1f)
                        NPC.velocity *= 0.95f;

                    if (NPC.ai[2] != 1f)
                    {
                        charging = true;
                        NPC.frameCounter = 4;

                        // Velocity fix if PBG slowed
                        if (NPC.velocity.Length() < chargeSpeed)
                            NPC.velocity = new Vector2(calamityGlobalNPC.newAI[1], calamityGlobalNPC.newAI[2]);

                        calamityGlobalNPC.newAI[0] += 1f;
                        if (calamityGlobalNPC.newAI[0] > 90f)
                            NPC.velocity *= 1.01f;

                        // Spawn honey in legendary rev+
                        if (CalamityWorld.LegendaryMode && CalamityWorld.revenge && calamityGlobalNPC.newAI[0] % 6f == 0f)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                try
                                {
                                    int tilePositionX = (int)(NPC.Center.X / 16f);
                                    int tilePositionY = (int)(NPC.Center.Y / 16f);
                                    if (!WorldGen.SolidTile(tilePositionX, tilePositionY) && Main.tile[tilePositionX, tilePositionY].LiquidAmount == 0)
                                    {
                                        Main.tile[tilePositionX, tilePositionY].LiquidAmount = (byte)Main.rand.Next(50, 150);
                                        Main.tile[tilePositionX, tilePositionY].Get<LiquidData>().LiquidType = LiquidID.Honey;
                                        WorldGen.SquareTileFrame(tilePositionX, tilePositionY);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }

                        NPC.netUpdate = true;
                        return;
                    }

                    // Avoid cheap bullshit
                    NPC.damage = 0;

                    float playerLocation = vectorCenter.X - player.Center.X;
                    NPC.direction = playerLocation < 0 ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;

                    NPC.rotation = NPC.velocity.X * 0.02f;
                    charging = false;

                    NPC.velocity *= 0.9f;
                    float slowedVelocityThreshold = revenge ? 0.12f : 0.1f;
                    if (phase2)
                    {
                        NPC.velocity *= 0.98f;
                        slowedVelocityThreshold += 0.05f;
                    }
                    if (phase3)
                    {
                        NPC.velocity *= 0.98f;
                        slowedVelocityThreshold += 0.05f;
                    }
                    if (phase4)
                    {
                        NPC.velocity *= 0.98f;
                        slowedVelocityThreshold += 0.05f;
                    }
                    if (enrageScale > 0)
                        NPC.velocity *= 0.95f;

                    if (Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y) < slowedVelocityThreshold)
                    {
                        NPC.ai[2] = 0f;
                        NPC.ai[1] += 1f;
                        calamityGlobalNPC.newAI[0] = 0f;
                    }
                }
            }

            // Move closer if too far away
            else if (NPC.ai[0] == 2f)
            {
                // Avoid cheap bullshit
                NPC.damage = 0;

                float playerLocation = vectorCenter.X - player.Center.X;
                NPC.direction = playerLocation < 0 ? 1 : -1;
                NPC.spriteDirection = NPC.direction;

                float playerXDist = player.position.X + (player.width / 2) - vectorCenter.X;
                float playerYDist = player.position.Y + (player.height / 2) - 200f - vectorCenter.Y;
                float playerTotalDist = (float)Math.Sqrt(playerXDist * playerXDist + playerYDist * playerYDist);

                calamityGlobalNPC.newAI[0] += 1f;
                if (playerTotalDist < 600f || calamityGlobalNPC.newAI[0] >= 180f)
                {
                    NPC.ai[0] = (phase3 || bossRush) ? 5f : 1f;
                    NPC.ai[1] = 0f;
                    calamityGlobalNPC.newAI[0] = 0f;
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;

                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);

                    return;
                }

                // Move closer
                Movement(100f, 350f, 450f, player, enrageScale);
            }

            // Spawn less missiles
            else if (NPC.ai[0] == 1f)
            {
                // Avoid cheap bullshit
                NPC.damage = 0;

                charging = false;
                Vector2 missileSpawnPos = new Vector2(NPC.direction == 1 ? NPC.getRect().BottomLeft().X : NPC.getRect().BottomRight().X, NPC.getRect().Bottom().Y + 20f);
                missileSpawnPos.X += NPC.direction * 120;
                float playerMissileX = player.position.X + (player.width / 2) - vectorCenter.X;
                float playerMissileY = player.position.Y + (player.height / 2) - vectorCenter.Y;
                float playerMissileDist = (float)Math.Sqrt(playerMissileX * playerMissileX + playerMissileY * playerMissileY);

                NPC.ai[1] += 1f;
                NPC.ai[1] += activePlayers / 2;
                if (phase2)
                    NPC.ai[1] += 0.5f;

                bool shouldSpawnMissiles = false;
                if (NPC.ai[1] > 40f - 12f * enrageScale)
                {
                    NPC.ai[1] = 0f;
                    NPC.ai[2] += 1f;
                    shouldSpawnMissiles = true;
                }

                if (shouldSpawnMissiles)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit8, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (expertMode && NPC.CountNPCS(ModContent.NPCType<PlagueMine>()) < 2)
                            NPC.NewNPC(NPC.GetSource_FromAI(), (int)missileSpawnPos.X, (int)missileSpawnPos.Y, ModContent.NPCType<PlagueMine>(), 0, 0f, 0f, 0f, challengeAmt);

                        float npcSpeed = (revenge ? 9f : 7f) + enrageScale * 2f;

                        float projXDist = player.position.X + player.width * 0.5f - missileSpawnPos.X;
                        float projYDist = player.position.Y + player.height * 0.5f - missileSpawnPos.Y;
                        float projDistance = (float)Math.Sqrt(projXDist * projXDist + projYDist * projYDist);

                        projDistance = npcSpeed / projDistance;
                        projXDist *= projDistance;
                        projYDist *= projDistance;

                        int plagueMissile = NPC.NewNPC(NPC.GetSource_FromAI(), (int)missileSpawnPos.X, (int)missileSpawnPos.Y, ModContent.NPCType<PlagueHomingMissile>(), 0, 0f, 0f, 0f, challengeAmt);
                        Main.npc[plagueMissile].velocity.X = projXDist;
                        Main.npc[plagueMissile].velocity.Y = projYDist;
                        Main.npc[plagueMissile].netUpdate = true;
                    }
                }

                // Move closer if too far away
                if (playerMissileDist > 600f)
                    Movement(100f, 350f, 450f, player, enrageScale);
                else
                    NPC.velocity *= 0.9f;

                float playerLocation = vectorCenter.X - player.Center.X;
                NPC.direction = playerLocation < 0 ? 1 : -1;
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[2] > 3f)
                {
                    NPC.ai[0] = -1f;
                    NPC.ai[1] = 2f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;

                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);
                }
            }

            // Missile spawn
            else if (NPC.ai[0] == 5f)
            {
                // Avoid cheap bullshit
                NPC.damage = 0;

                charging = false;
                Vector2 missileSpawnPos = new Vector2(NPC.direction == 1 ? NPC.getRect().BottomLeft().X : NPC.getRect().BottomRight().X, NPC.getRect().Bottom().Y + 20f);
                missileSpawnPos.X += NPC.direction * 120;
                float playerMissileX = player.position.X + (player.width / 2) - vectorCenter.X;
                float playerMissileY = player.position.Y + (player.height / 2) - vectorCenter.Y;
                float playerMissileDist = (float)Math.Sqrt(playerMissileX * playerMissileX + playerMissileY * playerMissileY);

                NPC.ai[1] += 1f;
                NPC.ai[1] += activePlayers / 2;
                bool shouldSpawnMissiles = false;
                if (phase4)
                    NPC.ai[1] += 0.5f;
                if (phase5)
                    NPC.ai[1] += 0.5f;

                if (NPC.ai[1] % 20f == 19f)
                    NPC.netUpdate = true;

                if (NPC.ai[1] > 30f - 12f * enrageScale)
                {
                    NPC.ai[1] = 0f;
                    NPC.ai[2] += 1f;
                    shouldSpawnMissiles = true;
                }

                if (shouldSpawnMissiles)
                {
                    SoundEngine.PlaySound(SoundID.Item88, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (expertMode && NPC.CountNPCS(ModContent.NPCType<PlagueMine>()) < 3)
                            NPC.NewNPC(NPC.GetSource_FromAI(), (int)missileSpawnPos.X, (int)missileSpawnPos.Y, ModContent.NPCType<PlagueMine>(), 0, 0f, 0f, 0f, challengeAmt);

                        float npcSpeed = (revenge ? 11f : 9f) + enrageScale * 2f;

                        float projXDist = player.position.X + player.width * 0.5f - missileSpawnPos.X;
                        float projYDist = player.position.Y + player.height * 0.5f - missileSpawnPos.Y;
                        float projDistance = (float)Math.Sqrt(projXDist * projXDist + projYDist * projYDist);

                        projDistance = npcSpeed / projDistance;
                        projXDist *= projDistance;
                        projYDist *= projDistance;
                        projXDist += Main.rand.Next(-20, 21) * 0.05f;
                        projYDist += Main.rand.Next(-20, 21) * 0.05f;

                        int plagueMissile = NPC.NewNPC(NPC.GetSource_FromAI(), (int)missileSpawnPos.X, (int)missileSpawnPos.Y, ModContent.NPCType<PlagueHomingMissile>(), 0, 0f, 0f, 0f, challengeAmt);
                        Main.npc[plagueMissile].velocity.X = projXDist;
                        Main.npc[plagueMissile].velocity.Y = projYDist;
                        Main.npc[plagueMissile].netUpdate = true;
                    }
                }

                // Move closer if too far away
                if (playerMissileDist > 600f)
                    Movement(100f, 350f, 450f, player, enrageScale);
                else
                    NPC.velocity *= 0.9f;

                float playerLocation = vectorCenter.X - player.Center.X;
                NPC.direction = playerLocation < 0 ? 1 : -1;
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[2] > ((CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 3f : 5f))
                {
                    NPC.ai[0] = -1f;
                    NPC.ai[1] = 2f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;

                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);
                }
            }

            // Stinger phase
            else if (NPC.ai[0] == 3f)
            {
                // Avoid cheap bullshit
                NPC.damage = 0;

                Vector2 stingerSpawnPos = new Vector2(NPC.direction == 1 ? NPC.getRect().BottomLeft().X : NPC.getRect().BottomRight().X, NPC.getRect().Bottom().Y + 20f);
                stingerSpawnPos.X += NPC.direction * 120;

                NPC.ai[1] += 1f;
                int stingerFireDelay = phase5 ? 20 : (phase3 ? 25 : 30);
                stingerFireDelay -= (int)Math.Ceiling(5f * enrageScale);

                if (NPC.ai[1] % stingerFireDelay == (stingerFireDelay - 1) && vectorCenter.Y < player.position.Y)
                {
                    SoundEngine.PlaySound(SoundID.Item42, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float projectileSpeed = revenge ? 6f : 5f;
                        projectileSpeed += 2f * enrageScale;

                        float projXDist = player.position.X + player.width * 0.5f - stingerSpawnPos.X;
                        float projYDist = player.position.Y + player.height * 0.5f - stingerSpawnPos.Y;
                        float projDistance = (float)Math.Sqrt(projXDist * projXDist + projYDist * projYDist);
                        projDistance = projectileSpeed / projDistance;
                        projXDist *= projDistance;
                        projYDist *= projDistance;

                        int type = ModContent.ProjectileType<PlagueStingerGoliathV2>();
                        switch ((int)NPC.ai[2])
                        {
                            case 0:
                            case 1:
                                break;
                            case 2:
                            case 3:
                                if (expertMode)
                                    type = ModContent.ProjectileType<PlagueStingerGoliath>();
                                break;
                            case 4:
                                type = ModContent.ProjectileType<HiveBombGoliath>();
                                break;
                        }

                        if (Main.zenithWorld)
                        {
                            type = ModContent.ProjectileType<HiveBombGoliath>();
                        }

                        int damage = NPC.GetProjectileDamage(type);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), stingerSpawnPos.X, stingerSpawnPos.Y, projXDist, projYDist, type, damage, 0f, Main.myPlayer, challengeAmt, player.position.Y);
                        NPC.netUpdate = true;
                    }

                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] > 4f)
                        NPC.ai[2] = 0f;
                }

                Movement(100f, 400f, 500f, player, enrageScale);

                float playerLocation = vectorCenter.X - player.Center.X;
                NPC.direction = playerLocation < 0 ? 1 : -1;
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] > stingerFireDelay * 10f)
                {
                    NPC.ai[0] = -1f;
                    NPC.ai[1] = 3f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;

                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);
                }
            }

            // Missile charge
            else if (NPC.ai[0] == 4f)
            {
                float chargeSpeed = revenge ? 28f : 26f;

                chargeSpeed += 3f * enrageScale;

                int phaseSwitchTimer = (int)Math.Ceiling(2f + enrageScale);
                if (NPC.ai[1] > (2 * phaseSwitchTimer) && NPC.ai[1] % 2f == 0f)
                {
                    MissileCountdown = 0;
                    NPC.ai[0] = -1f;
                    NPC.ai[1] = -1f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;

                    // Prevent netUpdate from being blocked by the spam counter.
                    // A phase switch sync is a critical operation that must be synced.
                    if (NPC.netSpam >= 10)
                        NPC.netSpam = 9;
                    SoundEngine.PlaySound(AttackSwitchSound, NPC.Center);
                    return;
                }

                // Charge
                if (NPC.ai[1] % 2f == 0f)
                {
                    // Avoid cheap bullshit
                    NPC.damage = 0;

                    float playerLocation = vectorCenter.X - player.Center.X;

                    float chargeThresholdY = 20f;
                    chargeThresholdY += 20 * enrageScale;

                    if (Math.Abs(vectorCenter.Y - (player.Center.Y - 500f)) < chargeThresholdY)
                    {
                        // Set damage
                        NPC.damage = NPC.defDamage;

                        if (MissileCountdown == 1)
                        {
                            SoundEngine.PlaySound(BarrageLaunchSound, NPC.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float speed = revenge ? 6f : 5f;
                                speed += 2f * enrageScale;

                                bool gaussMode = false;

                                int type = ModContent.ProjectileType<HiveBombGoliath>();
                                int damage = NPC.GetProjectileDamage(type);

                                Vector2 baseVelocity = player.Center - vectorCenter;
                                baseVelocity.Normalize();
                                baseVelocity *= speed;

                                if (Main.rand.NextBool(10) && Main.zenithWorld)
                                {
                                    type = ModContent.ProjectileType<AresGaussNukeProjectile>();
                                    baseVelocity *= 0.75f;
                                    gaussMode = true;
                                }
                                else if (Main.rand.NextBool() && Main.zenithWorld)
                                {
                                    type = ModContent.ProjectileType<PeanutRocket>();
                                    baseVelocity *= 0.4f;
                                }

                                int missiles = bossRush ? 16 : MissileProjectiles;
                                int spread = bossRush ? 18 : 24;
                                if (!gaussMode)
                                {
                                    for (int i = 0; i < missiles; i++)
                                    {
                                        Vector2 spawn = vectorCenter; // Normal = 96, Boss Rush = 144
                                        spawn.X += i * (int)(spread * 1.125) - (missiles * (spread / 2)); // Normal = -96 to 93, Boss Rush = -144 to 156
                                        Vector2 velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-MissileAngleSpread / 2 + (MissileAngleSpread * i / missiles)));
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, velocity, type, damage, 0f, Main.myPlayer, nukeBarrageChallengeAmt, player.position.Y);
                                    }
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, baseVelocity, type, damage, 0f, Main.myPlayer);
                                }
                            }
                        }

                        charging = true;

                        NPC.ai[1] += 1f;
                        NPC.ai[2] = 0f;

                        float targetX = player.position.X + (player.width / 2) - vectorCenter.X;
                        float targetY = player.position.Y - 500f + (player.height / 2) - vectorCenter.Y;
                        float targetDistance = (float)Math.Sqrt(targetX * targetX + targetY * targetY);

                        targetDistance = chargeSpeed / targetDistance;
                        NPC.velocity.X = targetX * targetDistance;
                        NPC.velocity.Y = targetY * targetDistance;
                        NPC.rotation = (float)Math.Atan2(NPC.velocity.Y, NPC.velocity.X);

                        NPC.direction = playerLocation < 0 ? 1 : -1;
                        NPC.spriteDirection = NPC.direction;
                        if (NPC.spriteDirection != 1)
                            NPC.rotation += (float)Math.PI;

                        NPC.netUpdate = true;

                        return;
                    }

                    NPC.rotation = NPC.velocity.X * 0.02f;
                    charging = false;

                    float maxLineUpSpeedY = revenge ? 16f : 14f;
                    float lineUpAccelY = revenge ? 0.2f : 0.18f;
                    maxLineUpSpeedY += 1.5f * enrageScale;
                    lineUpAccelY += 0.25f * enrageScale;

                    if (vectorCenter.Y < player.Center.Y - 500f)
                        NPC.velocity.Y += lineUpAccelY;
                    else
                        NPC.velocity.Y -= lineUpAccelY;

                    if (NPC.velocity.Y < -maxLineUpSpeedY)
                        NPC.velocity.Y = -maxLineUpSpeedY;
                    if (NPC.velocity.Y > maxLineUpSpeedY)
                        NPC.velocity.Y = maxLineUpSpeedY;

                    if (Math.Abs(vectorCenter.X - player.Center.X) > 600f)
                        NPC.velocity.X += lineUpAccelY * NPC.direction;
                    else if (Math.Abs(vectorCenter.X - player.Center.X) < 300f)
                        NPC.velocity.X -= lineUpAccelY * NPC.direction;
                    else
                        NPC.velocity.X *= 0.8f;

                    if (NPC.velocity.X < -maxLineUpSpeedY)
                        NPC.velocity.X = -maxLineUpSpeedY;
                    if (NPC.velocity.X > maxLineUpSpeedY)
                        NPC.velocity.X = maxLineUpSpeedY;

                    NPC.direction = playerLocation < 0 ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                }

                // Slow down after charge
                else
                {
                    // Set damage
                    NPC.damage = NPC.defDamage;

                    if (NPC.velocity.X < 0f)
                        NPC.direction = -1;
                    else
                        NPC.direction = 1;

                    NPC.spriteDirection = NPC.direction;

                    int stopChargeXDist = 600;
                    int chargeDirectionXSign = 1;

                    if (vectorCenter.X < player.Center.X)
                        chargeDirectionXSign = -1;
                    if (NPC.direction == chargeDirectionXSign && Math.Abs(vectorCenter.X - player.Center.X) > stopChargeXDist)
                        NPC.ai[2] = 1f;
                    if (enrageScale > 0 && NPC.ai[2] == 1f)
                        NPC.velocity *= 0.95f;

                    if (NPC.ai[2] != 1f)
                    {
                        charging = true;

                        // Velocity fix if PBG slowed
                        if (NPC.velocity.Length() < chargeSpeed)
                            NPC.velocity.X = chargeSpeed * NPC.direction;

                        calamityGlobalNPC.newAI[0] += 1f;
                        if (calamityGlobalNPC.newAI[0] > 90f)
                            NPC.velocity.X *= 1.01f;

                        return;
                    }

                    // Avoid cheap bullshit
                    NPC.damage = 0;

                    NPC.rotation = NPC.velocity.X * 0.02f;
                    charging = false;

                    NPC.velocity *= 0.9f;
                    float slowedVelocityThreshold = revenge ? 0.12f : 0.1f;
                    if (phase3)
                    {
                        NPC.velocity *= 0.9f;
                        slowedVelocityThreshold += 0.05f;
                    }
                    if (phase4)
                    {
                        NPC.velocity *= 0.9f;
                        slowedVelocityThreshold += 0.05f;
                    }
                    if (phase5)
                    {
                        NPC.velocity *= 0.9f;
                        slowedVelocityThreshold += 0.05f;
                    }
                    if (enrageScale > 0)
                        NPC.velocity *= 0.95f;

                    if (Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y) < slowedVelocityThreshold)
                    {
                        NPC.ai[2] = 0f;
                        NPC.ai[1] += 1f;
                        calamityGlobalNPC.newAI[0] = 0f;
                    }
                }
            }
        }

        private void Movement(float xPos, float yPos, float yPos2, Player player, float enrageScale)
        {
            Vector2 acceleration = new Vector2(0.1f, 0.15f);
            Vector2 velocity = new Vector2(8f, 5f);
            float deceleration = 0.9f;

            acceleration *= 0.1f * enrageScale + 1f;
            velocity *= 1f - enrageScale * 0.1f;
            if (BossRushEvent.BossRushActive)
                velocity *= 0.5f;
            deceleration *= 1f - enrageScale * 0.1f;

            if (NPC.position.Y > player.position.Y - yPos)
            {
                if (NPC.velocity.Y > 0f)
                    NPC.velocity.Y *= deceleration;
                NPC.velocity.Y -= acceleration.Y;
                if (NPC.velocity.Y > velocity.Y)
                    NPC.velocity.Y = velocity.Y;
            }
            else if (NPC.position.Y < player.position.Y - yPos2)
            {
                if (NPC.velocity.Y < 0f)
                    NPC.velocity.Y *= deceleration;
                NPC.velocity.Y += acceleration.Y;
                if (NPC.velocity.Y < -velocity.Y)
                    NPC.velocity.Y = -velocity.Y;
            }

            if (NPC.position.X + (NPC.width / 2) > player.position.X + (player.width / 2) + xPos)
            {
                if (NPC.velocity.X > 0f)
                    NPC.velocity.X *= deceleration;
                NPC.velocity.X -= acceleration.X;
                if (NPC.velocity.X > velocity.X)
                    NPC.velocity.X = velocity.X;
            }
            if (NPC.position.X + (NPC.width / 2) < player.position.X + (player.width / 2) - xPos)
            {
                if (NPC.velocity.X < 0f)
                    NPC.velocity.X *= deceleration;
                NPC.velocity.X += acceleration.X;
                if (NPC.velocity.X < -velocity.X)
                    NPC.velocity.X = -velocity.X;
            }
        }

        public override bool CheckActive() => canDespawn;

        // Can only hit the target if within certain distance
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            Rectangle targetHitbox = target.Hitbox;

            float hitboxTopLeft = Vector2.Distance(NPC.Center, targetHitbox.TopLeft());
            float hitboxTopRight = Vector2.Distance(NPC.Center, targetHitbox.TopRight());
            float hitboxBotLeft = Vector2.Distance(NPC.Center, targetHitbox.BottomLeft());
            float hitboxBotRight = Vector2.Distance(NPC.Center, targetHitbox.BottomRight());

            float minDist = hitboxTopLeft;
            if (hitboxTopRight < minDist)
                minDist = hitboxTopRight;
            if (hitboxBotLeft < minDist)
                minDist = hitboxBotLeft;
            if (hitboxBotRight < minDist)
                minDist = hitboxBotRight;

            return minDist <= 100f * NPC.scale;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int k = 0; k < 2; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, (int)CalamityDusts.Plague, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 1; i < 7; i++)
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("PlaguebringerGoliathGore" + i).Type, NPC.scale);
                }
                NPC.position.X = NPC.position.X + (NPC.width / 2);
                NPC.position.Y = NPC.position.Y + (NPC.height / 2);
                NPC.width = 200;
                NPC.height = 200;
                NPC.position.X = NPC.position.X - (NPC.width / 2);
                NPC.position.Y = NPC.position.Y - (NPC.height / 2);
                for (int i = 0; i < 40; i++)
                {
                    int plagueDust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, (int)CalamityDusts.Plague, 0f, 0f, 100, default, 2f);
                    Main.dust[plagueDust].velocity *= 3f;
                    if (Main.rand.NextBool())
                    {
                        Main.dust[plagueDust].scale = 0.5f;
                        Main.dust[plagueDust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int j = 0; j < 70; j++)
                {
                    int plagueDust2 = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, (int)CalamityDusts.Plague, 0f, 0f, 100, default, 3f);
                    Main.dust[plagueDust2].noGravity = true;
                    Main.dust[plagueDust2].velocity *= 5f;
                    plagueDust2 = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, (int)CalamityDusts.Plague, 0f, 0f, 100, default, 2f);
                    Main.dust[plagueDust2].velocity *= 2f;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathGlow").Value;
            if (curTex != (charging ? 2 : 1))
            {
                NPC.frame.X = 0;
                NPC.frame.Y = 0;
            }
            if (charging)
            {
                curTex = 2;
                texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTex").Value;
                glowTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/PlaguebringerGoliath/PlaguebringerGoliathChargeTexGlow").Value;
            }
            else
            {
                curTex = 1;
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            int frameCount = 3;
            Rectangle rectangle = new Rectangle(NPC.frame.X, NPC.frame.Y, texture.Width / 2, texture.Height / frameCount);
            Vector2 halfSizeTexture = rectangle.Size() / 2f;
            Vector2 posOffset = new Vector2(charging ? 175 : 125, 0);
            int afterimageAmt = 10;
            if (NPC.ai[0] != 0f && NPC.ai[0] != 4f)
                afterimageAmt = 7;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int j = 1; j < afterimageAmt; j += 2)
                {
                    Color afterimageColor = drawColor;
                    afterimageColor = Color.Lerp(afterimageColor, Color.White, 0.5f);
                    afterimageColor = NPC.GetAlpha(afterimageColor);
                    afterimageColor *= (afterimageAmt - j) / 15f;
                    Vector2 afterimagePos = NPC.oldPos[j] + new Vector2(NPC.width, NPC.height) / 2f - screenPos;
                    afterimagePos -= new Vector2(texture.Width, texture.Height / frameCount) * NPC.scale / 2f;
                    afterimagePos += halfSizeTexture * NPC.scale + posOffset;
                    spriteBatch.Draw(texture, afterimagePos, new Rectangle?(rectangle), afterimageColor, NPC.rotation, halfSizeTexture, NPC.scale, spriteEffects, 0f);
                }
            }

            Vector2 drawLocation = NPC.Center - screenPos;
            drawLocation -= new Vector2(texture.Width, texture.Height / frameCount) * NPC.scale / 2f;
            drawLocation += halfSizeTexture * NPC.scale + posOffset;
            spriteBatch.Draw(texture, drawLocation, new Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, halfSizeTexture, NPC.scale, spriteEffects, 0f);

            Color redLerpColor = Color.Lerp(Color.White, Color.Red, 0.5f);

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int k = 1; k < afterimageAmt; k++)
                {
                    Color otherAfterimageColor = redLerpColor;
                    otherAfterimageColor = Color.Lerp(otherAfterimageColor, Color.White, 0.5f);
                    otherAfterimageColor *= (afterimageAmt - k) / 15f;
                    Vector2 otherAfterimagePos = NPC.oldPos[k] + new Vector2(NPC.width, NPC.height) / 2f - screenPos;
                    otherAfterimagePos -= new Vector2(glowTexture.Width, glowTexture.Height / frameCount) * NPC.scale / 2f;
                    otherAfterimagePos += halfSizeTexture * NPC.scale + posOffset;
                    spriteBatch.Draw(glowTexture, otherAfterimagePos, new Rectangle?(rectangle), otherAfterimageColor, NPC.rotation, halfSizeTexture, NPC.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(glowTexture, drawLocation, new Rectangle?(rectangle), redLerpColor, NPC.rotation, halfSizeTexture, NPC.scale, spriteEffects, 0f);

            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            int width = !charging ? (532 / 2) : (644 / 2);
            int height = !charging ? (768 / 3) : (636 / 3);
            NPC.frameCounter += 1.0;

            if (NPC.frameCounter > 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + height;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= height * 3)
            {
                NPC.frame.Y = 0;
                NPC.frame.X = NPC.frame.X == 0 ? width : 0;
                if (charging)
                {
                    flyingFrame2 = !flyingFrame2;
                }
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void OnKill()
        {
            CalamityGlobalNPC.SetNewBossJustDowned(NPC);

            // Mark PBG as dead
            DownedBossSystem.downedPlaguebringer = true;
            CalamityNetcode.SyncWorld();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<PlaguebringerGoliathBag>()));

            // Normal drops: Everything that would otherwise be in the bag
            var normalOnly = npcLoot.DefineNormalOnlyDropSet();
            {
                // Weapons
                int[] weapons = new int[]
                {
                    ModContent.ItemType<Virulence>(),
                    ModContent.ItemType<DiseasedPike>(),
                    ModContent.ItemType<Pandemic>(),
                    ModContent.ItemType<Malevolence>(),
                    ModContent.ItemType<PestilentDefiler>(),
                    ModContent.ItemType<TheHive>(),
                    ModContent.ItemType<BlightSpewer>(),
                    ModContent.ItemType<PlagueStaff>(),
                    ModContent.ItemType<FuelCellBundle>(),
                    ModContent.ItemType<InfectedRemote>(),
                    ModContent.ItemType<TheSyringe>(),
                };
                normalOnly.Add(DropHelper.CalamityStyle(DropHelper.NormalWeaponDropRateFraction, weapons));
                normalOnly.Add(ModContent.ItemType<Malachite>(), 10);

                // Materials
                normalOnly.Add(ItemID.Stinger, 1, 3, 5);
                normalOnly.Add(ModContent.ItemType<PlagueCellCanister>(), 1, 15, 20);
                normalOnly.Add(DropHelper.PerPlayer(ModContent.ItemType<InfectedArmorPlating>(), 1, 25, 30));

                // Equipment
                normalOnly.Add(DropHelper.PerPlayer(ModContent.ItemType<ToxicHeart>()));

                // Vanity
                normalOnly.Add(ModContent.ItemType<PlaguebringerGoliathMask>(), 7);
                normalOnly.Add(ModContent.ItemType<PlagueCaller>(), 10);
                normalOnly.Add(ModContent.ItemType<ThankYouPainting>(), ThankYouPainting.DropInt);
            }

            npcLoot.Add(ModContent.ItemType<PlaguebringerGoliathTrophy>(), 10);

            // Relic
            npcLoot.DefineConditionalDropSet(DropHelper.RevAndMaster).Add(ModContent.ItemType<PlaguebringerGoliathRelic>());

            // GFB Honey Bucket drop
            npcLoot.DefineConditionalDropSet(DropHelper.GFB).Add(ItemID.BottomlessHoneyBucket, hideLootReport: true);

            // Lore
            npcLoot.AddConditionalPerPlayer(() => !DownedBossSystem.downedPlaguebringer, ModContent.ItemType<LorePlaguebringerGoliath>(), desc: DropHelper.FirstKillText);
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance * bossAdjustment);
            NPC.damage = (int)(NPC.damage * NPC.GetExpertDamageMultiplier());
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (hurtInfo.Damage > 0)
            {
                if (Main.zenithWorld) // it is the plague, you get very sick.
                {
                    target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 480, true);
                    target.AddBuff(BuffID.Poisoned, 480, true);
                    target.AddBuff(BuffID.Venom, 480, true);
                }
                target.AddBuff(ModContent.BuffType<Plague>(), 240, true);
            }
        }
    }
}
