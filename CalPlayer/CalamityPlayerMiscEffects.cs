﻿using CalamityMod.Buffs.Alcohol;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.Potions;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Buffs.Summon;
using CalamityMod.Cooldowns;
using CalamityMod.CustomRecipes;
using CalamityMod.DataStructures;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Items;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Dyes;
using CalamityMod.Items.Fishing.AstralCatches;
using CalamityMod.Items.Fishing.BrimstoneCragCatches;
using CalamityMod.Items.Fishing.FishingRods;
using CalamityMod.Items.Mounts.Minecarts;
using CalamityMod.Items.Potions;
using CalamityMod.Items.Potions.Alcohol;
using CalamityMod.Items.VanillaArmorChanges;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.Astral;
using CalamityMod.NPCs.Crags;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Other;
using CalamityMod.NPCs.PlagueEnemies;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Magic;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Rogue;
using CalamityMod.Projectiles.Summon;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.UI;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using Terraria.Audio;
using Terraria.DataStructures;
using CalamityMod.EntitySources;
using ReLogic.Content;
using Terraria.GameContent;

namespace CalamityMod.CalPlayer
{
    public partial class CalamityPlayer : ModPlayer
    {
        #region Post Update Misc Effects
        public override void PostUpdateMiscEffects()
        {
            // No category

            // Give the player a 24% jump speed boost while wings are equipped
            if (Player.wingsLogic > 0)
                Player.jumpSpeedBoost += 1.2f;

            // Decrease the counter on Fearmonger set turbo regeneration
            if (fearmongerRegenFrames > 0)
                fearmongerRegenFrames--;

            // Reduce the expert debuff time multiplier to the normal mode multiplier
            if (CalamityConfig.Instance.NerfExpertDebuffs)
            {
                var copy = Main.RegisteredGameModes[GameModeID.Expert];
                copy.DebuffTimeMultiplier = 1f;
                Main.RegisteredGameModes[GameModeID.Expert] = copy;
            }

            // Bool for any existing bosses, true if any boss NPC is active
            areThereAnyDamnBosses = CalamityUtils.AnyBossNPCS();

            // Bool for any existing events, true if any event is active
            areThereAnyDamnEvents = CalamityGlobalNPC.AnyEvents(Player);

            // Go through the old positions for the player.
            for (int i = Player.Calamity().OldPositions.Length - 1; i > 0; i--)
            {
                if (OldPositions[i - 1] == Vector2.Zero)
                    OldPositions[i - 1] = Player.position;
                OldPositions[i] = OldPositions[i - 1];
            }
            OldPositions[0] = Player.position;

            // Hurt the nearest NPC to the mouse if using the burning mouse.
            if (blazingCursorDamage)
                HandleBlazingMouseEffects();

            // Revengeance effects
            RevengeanceModeMiscEffects();

            // Abyss effects
            AbyssEffects();

            // Misc effects, because I don't know what else to call it
            MiscEffects();

            // Max life and mana effects
            MaxLifeAndManaEffects();

            // Standing still effects
            StandingStillEffects();

            // Elysian Aegis effects
            ElysianAegisEffects();

            // Other buff effects
            OtherBuffEffects();

            // Defense manipulation (Mostly defense damage, but also Bloodflare Core and others)
            DefenseEffects();

            // Limits
            Limits();

            // Stat Meter
            UpdateStatMeter();

            // Double Jumps
            DoubleJumps();

            // Potions (Quick Buff && Potion Sickness)
            HandlePotions();

            // Check if schematics are present on the mouse, for the sake of registering their recipes.
            CheckIfMouseItemIsSchematic();

            // Update all particle sets for items.
            // This must be done here instead of in the item logic because these sets are not properly instanced
            // in the global classes. Attempting to update them there will cause multiple updates to one set for multiple items.
            CalamityGlobalItem.UpdateAllParticleSets();
            BiomeBlade.UpdateAllParticleSets();
            TrueBiomeBlade.UpdateAllParticleSets();
            OmegaBiomeBlade.UpdateAllParticleSets();

            // Update the gem tech armor set.
            GemTechState.Update();

            // Regularly sync player stats & mouse control info during multiplayer
            if (Player.whoAmI == Main.myPlayer && Main.netMode == NetmodeID.MultiplayerClient)
            {
                packetTimer++;
                if (packetTimer == GlobalSyncPacketTimer)
                {
                    packetTimer = 0;
                    StandardSync();
                }

                if (syncMouseControls)
                {
                    syncMouseControls = false;
                    MouseControlsSync();
                }
            }


            // After everything else, if Daawnlight Spirit Origin is equipped, set ranged crit to the base 4%.
            // Store all the crit so it can be used in damage calculations.
            if (spiritOrigin)
            {
                // player.rangedCrit already contains the crit stat of the held item, no need to grab it separately.
                // Don't store the base 4% because you're not removing it.
                spiritOriginConvertedCrit = (int)(Player.GetCritChance(DamageClass.Ranged) - 4);
                Player.GetCritChance(DamageClass.Ranged) = 4;
            }

            if (Player.ActiveItem().type == ModContent.ItemType<GaelsGreatsword>())
                heldGaelsLastFrame = true;

            // De-equipping Gael's Greatsword deletes all rage.
            else if (heldGaelsLastFrame)
            {
                heldGaelsLastFrame = false;
                rage = 0f;
            }
        }
        #endregion

        #region Revengeance Effects
        private void RevengeanceModeMiscEffects()
        {
            if (CalamityWorld.revenge || BossRushEvent.BossRushActive)
            {
                // Adjusts the life steal cap in rev/death
                float lifeStealCap = (CalamityWorld.malice || BossRushEvent.BossRushActive) ? 30f : CalamityWorld.death ? 45f : 60f;
                if (Player.lifeSteal > lifeStealCap)
                    Player.lifeSteal = lifeStealCap;

                if (Player.whoAmI == Main.myPlayer)
                {
                    // Hallowed Armor nerf
                    if (Player.onHitDodge)
                    {
                        for (int l = 0; l < Player.MaxBuffs; l++)
                        {
                            int hasBuff = Player.buffType[l];
                            if (Player.buffTime[l] > 360 && hasBuff == BuffID.ShadowDodge)
                                Player.buffTime[l] = 360;
                        }
                    }

                    // Immunity Frames nerf
                    int immuneTimeLimit = 150;
                    if (Player.immuneTime > immuneTimeLimit)
                        Player.immuneTime = immuneTimeLimit;

                    for (int k = 0; k < Player.hurtCooldowns.Length; k++)
                    {
                        if (Player.hurtCooldowns[k] > immuneTimeLimit)
                            Player.hurtCooldowns[k] = immuneTimeLimit;
                    }

                    // Adrenaline and Rage
                    if (CalamityWorld.revenge)
                        UpdateRippers();
                }
            }

            // If Revengeance Mode is not active, then set rippers to zero
            else if (Player.whoAmI == Main.myPlayer)
            {
                rage = 0;
                adrenaline = 0;
            }
        }

        private void UpdateRippers()
        {
            // Figure out Rage's current duration based on boosts.
            if (rageBoostOne)
                RageDuration += RageDurationPerBooster;
            if (rageBoostTwo)
                RageDuration += RageDurationPerBooster;
            if (rageBoostThree)
                RageDuration += RageDurationPerBooster;

            // Tick down "Rage Combat Frames". When they reach zero, Rage begins fading away.
            if (rageCombatFrames > 0)
                --rageCombatFrames;

            // Tick down the Rage gain cooldown.
            if (rageGainCooldown > 0)
                --rageGainCooldown;

            // This is how much Rage will be changed by this frame.
            float rageDiff = 0;

            // If the player equips multiple rage generation accessories they get the max possible effect without stacking any of them.
            {
                float rageGen = 0f;

                // Shattered Community provides constant rage generation (stronger than Heart of Darkness).
                if (shatteredCommunity)
                {
                    float scRageGen = rageMax * ShatteredCommunity.RagePerSecond / 60f;
                    if (rageGen < scRageGen)
                        rageGen = scRageGen;
                }
                // Heart of Darkness grants constant rage generation.
                else if (heartOfDarkness)
                {
                    float hodRageGen = rageMax * HeartofDarkness.RagePerSecond / 60f;
                    if (rageGen < hodRageGen)
                        rageGen = hodRageGen;
                }

                rageDiff += rageGen;
            }

            // Holding Gael's Greatsword grants constant rage generation.
            if (heldGaelsLastFrame)
                rageDiff += rageMax * GaelsGreatsword.RagePerSecond / 60f;

            // Calculate and grant proximity rage.
            // Regular enemies can give up to 1x proximity rage. Bosses can give up to 3x. Multiple regular enemies don't stack.
            // Proximity rage is maxed out when within 10 blocks (160 pixels) of the enemy's hitbox.
            // Its max range is 50 blocks (800 pixels), at which you get zero proximity rage.
            // Proximity rage does not generate while Rage Mode is active.
            if (!rageModeActive)
            {
                float bossProxRageMultiplier = 3f;
                float minProxRageDistance = 160f;
                float maxProxRageDistance = 800f;
                float enemyDistance = maxProxRageDistance + 1f;
                float bossDistance = maxProxRageDistance + 1f;

                for (int i = 0; i < Main.maxNPCs; ++i)
                {
                    NPC npc = Main.npc[i];
                    if (npc is null || !npc.IsAnEnemy() || npc.Calamity().DoesNotGenerateRage)
                        continue;

                    // Take the longer of the two directions for the NPC's hitbox to be generous.
                    float generousHitboxWidth = Math.Max(npc.Hitbox.Width / 2f, npc.Hitbox.Height / 2f);
                    float hitboxEdgeDist = npc.Distance(Player.Center) - generousHitboxWidth;

                    // If this enemy is closer than the previous, reduce the current minimum proximity distance.
                    if (enemyDistance > hitboxEdgeDist)
                    {
                        enemyDistance = hitboxEdgeDist;

                        // If they're a boss, reduce the boss distance.
                        // Boss distance will always be >= enemy distance, so there's no need to do another check.
                        // Worm boss body and tail segments are not counted as bosses for this calculation.
                        if (npc.IsABoss() && !CalamityLists.noRageWormSegmentList.Contains(npc.type))
                            bossDistance = hitboxEdgeDist;
                    }
                }

                // Helper function to implement proximity rage formula
                float ProxRageFromDistance(float dist)
                {
                    // Adjusted distance with the 160 grace pixels added in. If you're closer than that it counts as zero.
                    float d = Math.Max(dist - minProxRageDistance, 0f);

                    // The first term is exponential decay which reduces rage gain significantly over distance.
                    // The second term is a linear component which allows a baseline but weak rage generation even at far distances.
                    // This function takes inputs from 0.0 to 640.0 and returns a value from 1.0 to 0.0.
                    float r = 1f / (0.034f * d + 2f) + (590.5f - d) / 1181f;
                    return MathHelper.Clamp(r, 0f, 1f);
                }

                // If anything is close enough then provide proximity rage.
                // You can only get proximity rage from one target at a time. You gain rage from whatever target would give you the most rage.
                if (enemyDistance <= maxProxRageDistance)
                {
                    // If the player is close enough to get proximity rage they are also considered to have rage combat frames.
                    // This prevents proximity rage from fading away unless you run away without attacking for some reason.
                    rageCombatFrames = Math.Max(rageCombatFrames, 3);

                    float proxRageFromEnemy = ProxRageFromDistance(enemyDistance);
                    float proxRageFromBoss = 0f;
                    if (bossDistance <= maxProxRageDistance)
                        proxRageFromBoss = bossProxRageMultiplier * ProxRageFromDistance(bossDistance);

                    float finalProxRage = Math.Max(proxRageFromEnemy, proxRageFromBoss);

                    // 300% proximity rage (max possible from a boss) will fill the Rage meter in 15 seconds.
                    // 100% proximity rage (max possible from an enemy) will fill the Rage meter in 45 seconds.
                    rageDiff += finalProxRage * rageMax / CalamityUtils.SecondsToFrames(45f);
                }
            }

            bool rageFading = rageCombatFrames <= 0 && !heartOfDarkness && !shatteredCommunity;

            // If Rage Mode is currently active, you smoothly lose all rage over the duration.
            if (rageModeActive)
                rageDiff -= rageMax / RageDuration;

            // If out of combat and NOT using Heart of Darkness or Shattered Community, Rage fades away.
            else if (!rageModeActive && rageFading)
                rageDiff -= rageMax / RageFadeTime;

            // Apply the rage change and cap rage in both directions.
            rage += rageDiff;
            if (rage < 0)
                rage = 0;

            if (rage >= rageMax)
            {
                // If Rage is not active, it is capped at 100%.
                if (!rageModeActive)
                    rage = rageMax;

                // If using the Shattered Community, Rage is capped at 200% while it's active.
                // This prevents infinitely stacking rage before a fight by standing on spikes/lava with a regen build or the Nurse handy.
                else if (shatteredCommunity && rage >= 2f * rageMax)
                    rage = 2f * rageMax;

                // Play a sound when the Rage Meter is full
                if (playFullRageSound)
                {
                    playFullRageSound = false;
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(Mod, "Sounds/Custom/AbilitySounds/FullRage"), (int)Player.position.X, (int)Player.position.Y);
                }
            }
            else
                playFullRageSound = true;

            // This is how much Adrenaline will be changed by this frame.
            float adrenalineDiff = 0;
            bool SCalAlive = NPC.AnyNPCs(ModContent.NPCType<SupremeCalamitas>());
            bool wofAndNotHell = Main.wofNPCIndex >= 0 && Player.position.Y < (float)((Main.maxTilesY - 200) * 16);

            // If Adrenaline Mode is currently active, you smoothly lose all adrenaline over the duration.
            if (adrenalineModeActive)
                adrenalineDiff = -adrenalineMax / AdrenalineDuration;
            else
            {
                // If any boss is alive (or you are between DoG phases or Boss Rush is active), you gain adrenaline smoothly.
                // EXCEPTION: Wall of Flesh is alive and you are not in hell. Then you don't get anything.
                if ((areThereAnyDamnBosses || CalamityWorld.DoGSecondStageCountdown > 0 || BossRushEvent.BossRushActive) &&
                    !wofAndNotHell)
                {
                    adrenalineDiff += adrenalineMax / AdrenalineChargeTime;
                }

                // If you aren't actively in a boss fight, adrenaline rapidly fades away.
                else
                    adrenalineDiff = -adrenalineMax / AdrenalineFadeTime;
            }

            // In the SCal fight, adrenaline charges 33% slower (meaning it takes 50% longer to fully charge it).
            if (SCalAlive && adrenalineDiff > 0f)
                adrenalineDiff *= 0.67f;

            // Apply the adrenaline change and cap adrenaline in both directions.
            adrenaline += adrenalineDiff;
            if (adrenaline < 0)
                adrenaline = 0;

            if (adrenaline >= adrenalineMax)
            {
                adrenaline = adrenalineMax;

                // Play a sound when the Adrenaline Meter is full
                if (playFullAdrenalineSound)
                {
                    playFullAdrenalineSound = false;
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(Mod, "Sounds/Custom/AbilitySounds/FullAdrenaline"), (int)Player.position.X, (int)Player.position.Y);
                }
            }
            else
                playFullAdrenalineSound = true;
        }
        #endregion

        #region Misc Effects

        private void HandleBlazingMouseEffects()
        {
            // The sigil's brightness slowly fades away every frame if not incinerating anything.
            blazingMouseAuraFade = MathHelper.Clamp(blazingMouseAuraFade - 0.025f, 0.25f, 1f);

            // miscCounter is used to limit Calamity's hit rate.
            int framesPerHit = 60 / Calamity.HitsPerSecond;
            if (Player.miscCounter % framesPerHit != 1)
                return;

            Rectangle sigilHitbox = Utils.CenteredRectangle(Main.MouseWorld, new Vector2(35f, 62f));
            int sigilDamage = (int)(Player.AverageDamage() * Calamity.BaseDamage);
            bool brightenedSigil = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                if (!target.active || !target.Hitbox.Intersects(sigilHitbox) || target.immortal || target.dontTakeDamage || target.townNPC)
                    continue;

                // Brighten the sigil because it is dealing damage. This can only happen once per hit event.
                if (!brightenedSigil)
                {
                    blazingMouseAuraFade = MathHelper.Clamp(blazingMouseAuraFade + 0.2f, 0.25f, 1f);
                    brightenedSigil = true;
                }

                // Create a direct strike to hit this specific NPC.
                var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<Calamity>()));
                Projectile.NewProjectileDirect(source, target.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), sigilDamage, 0f, Player.whoAmI, i);

                // Incinerate the target with Vulnerability Hex.
                target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), VulnerabilityHex.CalamityDuration);

                // Make some fancy dust to indicate damage is being done.
                for (int j = 0; j < 12; j++)
                {
                    Dust fire = Dust.NewDustDirect(target.position, target.width, target.height, 267);
                    fire.velocity = Vector2.UnitY * -Main.rand.NextFloat(2f, 3.45f);
                    fire.scale = 1f + fire.velocity.Length() / 6f;
                    fire.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.85f));
                    fire.noGravity = true;
                }
            }
        }

        private void MiscEffects()
        {
            // Do a vanity/social slot check for SCal's expert drop since alternatives to get this working are a pain in the ass to create.
            int blazingCursorItem = ModContent.ItemType<Calamity>();
            for (int i = 13; i < 18 + Player.extraAccessorySlots; i++)
            {
                if (Player.armor[i].type == blazingCursorItem)
                {
                    blazingCursorVisuals = true;
                    break;
                }
            }

            // Calculate/reset DoG cart rotations based on whether the DoG cart is in use.
            if (Player.mount.Active && Player.mount.Type == ModContent.MountType<DoGCartMount>())
            {
                SmoothenedMinecartRotation = MathHelper.Lerp(SmoothenedMinecartRotation, DelegateMethods.Minecart.rotation, 0.05f);

                // Initialize segments from null if necessary.
                int direction = (Player.velocity.SafeNormalize(Vector2.UnitX * Player.direction).X > 0f).ToDirectionInt();
                if (Player.velocity.X == 0f)
                    direction = Player.direction;

                float idealRotation = DoGCartMount.CalculateIdealWormRotation(Player);
                float minecartRotation = DelegateMethods.Minecart.rotation;
                if (Math.Abs(minecartRotation) < 0.5f)
                    minecartRotation = 0f;
                Vector2 stickOffset = minecartRotation.ToRotationVector2() * Player.velocity.Length() * direction * 1.25f;
                for (int i = 0; i < DoGCartSegments.Length; i++)
                {
                    if (DoGCartSegments[i] is null)
                    {
                        DoGCartSegments[i] = new DoGCartSegment
                        {
                            Center = Player.Center - idealRotation.ToRotationVector2() * i * 20f
                        };
                    }
                }

                Vector2 startingStickPosition = Player.Center + stickOffset + new Vector2(direction * (float)Math.Cos(SmoothenedMinecartRotation * 2f) * -34f, 12f);
                DoGCartSegments[0].Update(Player, startingStickPosition, idealRotation);
                DoGCartSegments[0].Center = startingStickPosition;

                for (int i = 1; i < DoGCartSegments.Length; i++)
                {
                    Vector2 waveOffset = DoGCartMount.CalculateSegmentWaveOffset(i, Player);
                    DoGCartSegments[i].Update(Player, DoGCartSegments[i - 1].Center + waveOffset, DoGCartSegments[i - 1].Rotation);
                }
            }
            else
                DoGCartSegments = new DoGCartSegment[DoGCartSegments.Length];

            // Dust on hand when holding the phosphorescent gauntlet.
            if (Player.ActiveItem().type == ModContent.ItemType<PhosphorescentGauntlet>())
                PhosphorescentGauntletPunches.GenerateDustOnOwnerHand(Player);

            if (stealthUIAlpha > 0f && (rogueStealth <= 0f || rogueStealthMax <= 0f))
            {
                stealthUIAlpha -= 0.035f;
                stealthUIAlpha = MathHelper.Clamp(stealthUIAlpha, 0f, 1f);
            }
            else if (stealthUIAlpha < 1f)
            {
                stealthUIAlpha += 0.035f;
                stealthUIAlpha = MathHelper.Clamp(stealthUIAlpha, 0f, 1f);
            }

            if (andromedaState == AndromedaPlayerState.LargeRobot ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<RelicOfDeliveranceSpear>()] > 0)
            {
                Player.controlHook = Player.releaseHook = false;
            }

            if (andromedaCripple > 0)
            {
                Player.velocity = Vector2.Clamp(Player.velocity, new Vector2(-11f, -8f), new Vector2(11f, 8f));
                andromedaCripple--;
            }

            if (Player.ownedProjectileCounts[ModContent.ProjectileType<GiantIbanRobotOfDoom>()] <= 0 &&
                andromedaState != AndromedaPlayerState.Inactive)
            {
                andromedaState = AndromedaPlayerState.Inactive;
            }

            if (andromedaState == AndromedaPlayerState.LargeRobot)
            {
                Player.width = 80;
                Player.height = 212;
                Player.position.Y -= 170;
                resetHeightandWidth = true;
            }
            else if (andromedaState == AndromedaPlayerState.SpecialAttack)
            {
                Player.width = 24;
                Player.height = 98;
                Player.position.Y -= 56;
                resetHeightandWidth = true;
            }
            else if (!Player.mount.Active && resetHeightandWidth)
            {
                Player.width = 20;
                Player.height = 42;
                resetHeightandWidth = false;
            }

            // Summon bullseyes on nearby targets.
            if (spiritOrigin)
            {
                int bullseyeType = ModContent.ProjectileType<SpiritOriginBullseye>();
                List<int> alreadyTargetedNPCs = new List<int>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type != bullseyeType || !Main.projectile[i].active || Main.projectile[i].owner != Player.whoAmI)
                        continue;

                    alreadyTargetedNPCs.Add((int)Main.projectile[i].ai[0]);
                }

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].friendly || Main.npc[i].lifeMax < 5 || alreadyTargetedNPCs.Contains(i) || Main.npc[i].realLife >= 0 || Main.npc[i].dontTakeDamage || Main.npc[i].immortal)
                        continue;

                    var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<DaawnlightSpiritOrigin>()));
                    if (Main.myPlayer == Player.whoAmI && Main.npc[i].WithinRange(Player.Center, 2000f))
                        Projectile.NewProjectile(source, Main.npc[i].Center, Vector2.Zero, bullseyeType, 0, 0f, Player.whoAmI, i);
                    if (spiritOriginBullseyeShootCountdown <= 0)
                        spiritOriginBullseyeShootCountdown = 45;
                }
            }

            // Proficiency level ups
            if (CalamityConfig.Instance.Proficiency)
                GetExactLevelUp();

            // Max mana bonuses
            Player.statManaMax2 +=
                (permafrostsConcoction ? 50 : 0) +
                (pHeart ? 50 : 0) +
                (eCore ? 50 : 0) +
                (cShard ? 50 : 0) +
                (starBeamRye ? 50 : 0);

            // Life Steal nerf
            // Reduces Normal Mode life steal recovery rate from 0.6/s to 0.5/s
            // Reduces Expert Mode life steal recovery rate from 0.5/s to 0.35/s
            // Revengeance Mode recovery rate is 0.3/s
            // Death Mode recovery rate is 0.25/s
            // Malice Mode recovery rate is 0.2/s
            float lifeStealCooldown = (CalamityWorld.malice || BossRushEvent.BossRushActive) ? 0.3f : CalamityWorld.death ? 0.25f : CalamityWorld.revenge ? 0.2f : Main.expertMode ? 0.15f : 0.1f;
            Player.lifeSteal -= lifeStealCooldown;

            // Nebula Armor nerf
            if (Player.nebulaLevelMana > 0 && Player.statMana < Player.statManaMax2)
            {
                int num = 12;
                nebulaManaNerfCounter += Player.nebulaLevelMana;
                if (nebulaManaNerfCounter >= num)
                {
                    nebulaManaNerfCounter -= num;
                    Player.statMana--;
                    if (Player.statMana < 0)
                        Player.statMana = 0;
                }
            }
            else
                nebulaManaNerfCounter = 0;

            // Bool for drawing boss health bar small text or not
            if (Main.myPlayer == Player.whoAmI)
                BossHealthBarManager.CanDrawExtraSmallText = shouldDrawSmallText;

            // Margarita halved debuff duration
            if (margarita)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    for (int l = 0; l < Player.MaxBuffs; l++)
                    {
                        int hasBuff = Player.buffType[l];
                        if (Player.buffTime[l] > 2 && CalamityLists.debuffList.Contains(hasBuff))
                        {
                            Player.buffTime[l]--;
                        }
                    }
                }
            }

            // Update the Providence Burn effect drawer if applicable.
            float providenceBurnIntensity = 0f;
            int provID = ModContent.NPCType<ProvidenceBoss>();
            if (Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss) && Main.npc[CalamityGlobalNPC.holyBoss].active && Main.npc[CalamityGlobalNPC.holyBoss].type == provID)
                providenceBurnIntensity = (Main.npc[CalamityGlobalNPC.holyBoss].ModNPC as ProvidenceBoss).CalculateBurnIntensity();
            ProvidenceBurnEffectDrawer.ParticleSpawnRate = int.MaxValue;

            // If the burn intensity is great enough, cause the player to ignite into flames.
            if (providenceBurnIntensity > 0.45f)
                ProvidenceBurnEffectDrawer.ParticleSpawnRate = 1;

            // Otherwise, if the intensity is too weak, but still presernt, cause the player to release holy cinders.
            else if (providenceBurnIntensity > 0f)
            {
                int cinderCount = (int)MathHelper.Lerp(1f, 4f, Utils.GetLerpValue(0f, 0.45f, providenceBurnIntensity, true));
                for (int i = 0; i < cinderCount; i++)
                {
                    if (!Main.rand.NextBool(3))
                        continue;

                    Dust holyCinder = Dust.NewDustDirect(Player.position, Player.width, Player.head, (int)CalamityDusts.ProfanedFire);
                    holyCinder.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    holyCinder.velocity.Y -= Main.rand.NextFloat(1f, 3f);
                    holyCinder.scale = Main.rand.NextFloat(1.15f, 1.45f);
                    holyCinder.noGravity = true;
                }
            }

            ProvidenceBurnEffectDrawer.Update();

            // Immunity to most debuffs
            if (invincible)
            {
                foreach (int debuff in CalamityLists.debuffList)
                    Player.buffImmune[debuff] = true;
            }

            // Transformer immunity to Electrified
            if (aSparkRare)
                Player.buffImmune[BuffID.Electrified] = true;

            // Reduce breath meter while in icy water instead of chilling
            bool canBreath = (aquaticHeart && NPC.downedBoss3) || Player.gills || Player.merman;
            if (Player.arcticDivingGear || canBreath)
            {
                Player.buffImmune[ModContent.BuffType<FrozenLungs>()] = true;
            }
            if (CalamityConfig.Instance.ReworkChilledWater)
            {
                if (Main.expertMode && Player.ZoneSnow && Player.wet && !Player.lavaWet && !Player.honeyWet)
                {
                    Player.buffImmune[BuffID.Chilled] = true;
                    if (Player.IsUnderwater())
                    {
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            Player.AddBuff(ModContent.BuffType<FrozenLungs>(), 2, false);
                        }
                    }
                }
                if (iCantBreathe)
                {
                    if (Player.breath > 0)
                        Player.breath--;
                }
            }

            // Extra DoT in the lava of the crags. Negated by Abaddon.
            if (Player.lavaWet)
            {
                if (ZoneCalamity && !abaddon)
                    Player.AddBuff(ModContent.BuffType<CragsLava>(), 2, false);
            }
            else
            {
                if (Player.lavaImmune)
                {
                    if (Player.lavaTime < Player.lavaMax)
                        Player.lavaTime++;
                }
            }

            // Acid rain droplets
            if (Player.whoAmI == Main.myPlayer)
            {
                if (CalamityWorld.rainingAcid && ZoneSulphur && !areThereAnyDamnBosses && Player.Center.Y < Main.worldSurface * 16f + 800f)
                {
                    int slimeRainRate = (int)(MathHelper.Clamp(Main.invasionSize * 0.4f, 13.5f, 50) * 2.25);
                    Vector2 spawnPoint = new Vector2(Player.Center.X + Main.rand.Next(-1000, 1001), Player.Center.Y - Main.rand.Next(700, 801));

                    if (Player.miscCounter % slimeRainRate == 0f)
                    {                        
                        if (DownedBossSystem.downedAquaticScourge && !DownedBossSystem.downedPolterghast && Main.rand.NextBool(12))
                        {
                            NPC.NewNPC(new EntitySource_SpawnNPC(), (int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<IrradiatedSlime>());
                        }
                    }
                }
            }

            // Hydrothermal blue smoke effects but it doesn't work epicccccc
            if (Player.whoAmI == Main.myPlayer)
            {
                if (hydrothermalSmoke)
                {
                    if (Math.Abs(Player.velocity.X) > 0.1f || Math.Abs(Player.velocity.Y) > 0.1f)
                    {
                        Projectile.NewProjectile(new ProjectileSource_HydrothermalSmoke(Player), Player.Center, Vector2.Zero, ModContent.ProjectileType<HydrothermalSmoke>(), 0, 0f, Player.whoAmI);
                    }
                }
                // Trying to find a workaround because apparently putting the bool in ResetEffects prevents it from working
                if (!Player.armorEffectDrawOutlines)
                {
                    hydrothermalSmoke = false;
                }
            }

            // Death Mode effects
            caveDarkness = 0f;
            if (CalamityWorld.death)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    // Thorn and spike effects
                    // 10 = crimson/corruption thorns, 17 = jungle thorns, 40 = dungeon spikes, 60 = temple spikes
                    Vector2 tileType;
                    if (!Player.mount.Active || !Player.mount.Cart)
                        tileType = Collision.HurtTiles(Player.position, Player.velocity, Player.width, Player.height, Player.fireWalk);
                    else
                        tileType = Collision.HurtTiles(Player.position, Player.velocity, Player.width, Player.height - 16, Player.fireWalk);
                    switch ((int)tileType.Y)
                    {
                        case 10:
                            Player.AddBuff(BuffID.Weak, 300, false);
                            Player.AddBuff(BuffID.Bleeding, 300, false);
                            break;
                        case 17:
                            Player.AddBuff(BuffID.Poisoned, 300, false);
                            break;
                        case 40:
                            Player.AddBuff(BuffID.Bleeding, 300, false);
                            break;
                        case 60:
                            Player.AddBuff(BuffID.Venom, 300, false);
                            break;
                        default:
                            break;
                    }
                }
            }

            // Increase fall speed
            if (!Player.mount.Active)
            {
                if (Player.IsUnderwater() && ironBoots)
                    Player.maxFallSpeed = 9f;

                if (!Player.wet)
                {
                    if (cirrusDress)
                        Player.maxFallSpeed = 12f;
                    if (aeroSet)
                        Player.maxFallSpeed = 15f;
                    if (gSabatonFall > 0 || Player.PortalPhysicsEnabled)
                        Player.maxFallSpeed = 20f;
                }

                if (LungingDown)
                {
                    Player.maxFallSpeed = 80f;
                    Player.noFallDmg = true;
                }
            }

            // Omega Blue Armor bonus
            if (omegaBlueSet)
            {
                // Add tentacles
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<OmegaBlueTentacle>()] < 6 && Main.myPlayer == Player.whoAmI)
                {
                    bool[] tentaclesPresent = new bool[6];
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile projectile = Main.projectile[i];
                        if (projectile.active && projectile.type == ModContent.ProjectileType<OmegaBlueTentacle>() && projectile.owner == Main.myPlayer && projectile.ai[1] >= 0f && projectile.ai[1] < 6f)
                            tentaclesPresent[(int)projectile.ai[1]] = true;
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        if (!tentaclesPresent[i])
                        {
                            int damage = (int)(390 * Player.AverageDamage());
                            var source = new ProjectileSource_OmegaBlueTentacles(Player);
                            Vector2 vel = new Vector2(Main.rand.Next(-13, 14), Main.rand.Next(-13, 14)) * 0.25f;
                            Projectile.NewProjectile(source, Player.Center, vel, ModContent.ProjectileType<OmegaBlueTentacle>(), damage, 8f, Main.myPlayer, Main.rand.Next(120), i);
                        }
                    }
                }

                float damageUp = 0.1f;
                int critUp = 10;
                if (omegaBlueHentai)
                {
                    damageUp *= 2f;
                    critUp *= 2;
                }
                Player.GetDamage<GenericDamageClass>() += damageUp;
                AllCritBoost(critUp);
            }

            bool canProvideBuffs = profanedCrystalBuffs || (!profanedCrystal && pArtifact) || (profanedCrystal && DownedBossSystem.downedSCal && DownedBossSystem.downedExoMechs);
            bool attack = Player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianAttack>()] > 0;

            // Guardian bonuses if not burnt out
            if (canProvideBuffs && !Player.HasCooldown(Cooldowns.ProfanedSoulArtifact.ID))
            {
                bool healer = Player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianHealer>()] > 0;
                bool defend = Player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianDefense>()] > 0;
                if (healer)
                {
                    if (healCounter > 0)
                        healCounter--;

                    if (healCounter <= 0)
                    {
                        bool enrage = Player.statLife < (int)(Player.statLifeMax2 * 0.5);

                        healCounter = (!enrage && profanedCrystalBuffs) ? 360 : 300;

                        if (Player.whoAmI == Main.myPlayer)
                        {
                            int healAmount = 5 +
                                (defend ? 5 : 0) +
                                (attack ? 5 : 0);

                            Player.statLife += healAmount;
                            Player.HealEffect(healAmount);
                        }
                    }
                }

                if (defend)
                {
                    Player.moveSpeed += 0.05f +
                        (attack ? 0.05f : 0f);
                    Player.endurance += 0.025f +
                        (attack ? 0.025f : 0f);
                }

                if (attack)
                {
                    Player.GetDamage(DamageClass.Summon) += 0.1f +
                        (defend ? 0.05f : 0f);
                }
            }

            // You always get the max minions, even during the effect of the burnout debuff
            if (attack && canProvideBuffs)
                Player.maxMinions++;

            if (nucleogenesis)
            {
                Player.maxMinions += 4;
            }
            else
            {
                // First Shadowflame is +1, Statis' Blessing is +2, Statis' Curse inherits both for +3
                if (shadowMinions)
                    Player.maxMinions ++;
                if (holyMinions)
                    Player.maxMinions += 2;

                if (starTaintedGenerator)
                    Player.maxMinions += 2;
                else
                {
                    if (starbusterCore)
                        Player.maxMinions++;

                    if (voltaicJelly)
                        Player.maxMinions++;

                    if (nuclearRod)
                        Player.maxMinions++;
                }
            }

            // Tick all cooldowns.
            // Depending on the code for each individual cooldown, this isn't guaranteed to do anything.
            // It may not tick down the timer or not do anything at all.
            IList<string> expiredCooldowns = new List<string>(16);
            var cdIterator = cooldowns.GetEnumerator();
            while(cdIterator.MoveNext())
            {
                KeyValuePair<string, CooldownInstance> kv = cdIterator.Current;
                string id = kv.Key;
                CooldownInstance instance = kv.Value;
                CooldownHandler handler = instance.handler;

                // If applicable, tick down this cooldown instance's timer.
                if (handler.CanTickDown)
                    --instance.timeLeft;

                // Tick always runs, even if the timer does not decrement.
                handler.Tick();

                // Run on-completion code, play sounds and remove finished cooldowns.
                if (instance.timeLeft < 0)
                {
                    handler.OnCompleted();
                    SoundEngine.PlaySound(handler.EndSound);
                    expiredCooldowns.Add(id);
                }
            }
            cdIterator.Dispose();

            // Remove all expired cooldowns.
            foreach (string cdID in expiredCooldowns)
                cooldowns.Remove(cdID);

            // If any cooldowns were removed, send a cooldown removal packet that lists all cooldowns to remove.
            if (expiredCooldowns.Count > 0)
                SyncCooldownRemoval(Main.netMode == NetmodeID.Server, expiredCooldowns);

            if (spiritOriginBullseyeShootCountdown > 0)
                spiritOriginBullseyeShootCountdown--;
            if (phantomicHeartRegen > 0 && phantomicHeartRegen < 1000)
                phantomicHeartRegen--;
            if (phantomicBulwarkCooldown > 0)
                phantomicBulwarkCooldown--;
            if (KameiBladeUseDelay > 0)
                KameiBladeUseDelay--;
            if (galileoCooldown > 0)
                galileoCooldown--;
            if (soundCooldown > 0)
                soundCooldown--;
            if (shadowPotCooldown > 0)
                shadowPotCooldown--;
            if (raiderCooldown > 0)
                raiderCooldown--;
            if (gSabatonCooldown > 0)
                gSabatonCooldown--;
            if (gSabatonFall > 0)
                gSabatonFall--;
            if (astralStarRainCooldown > 0)
                astralStarRainCooldown--;
            if (tarraRangedCooldown > 0)
                tarraRangedCooldown--;
            if (bloodflareMageCooldown > 0)
                bloodflareMageCooldown--;
            if (silvaMageCooldown > 0)
                silvaMageCooldown--;
            if (tarraMageHealCooldown > 0)
                tarraMageHealCooldown--;
            if (rogueCrownCooldown > 0)
                rogueCrownCooldown--;
            if (spectralVeilImmunity > 0)
                spectralVeilImmunity--;
            if (jetPackDash > 0)
                jetPackDash--;
            if (theBeeCooldown > 0)
                theBeeCooldown--;
            if (jellyDmg > 0f)
                jellyDmg -= 1f;
            if (ataxiaDmg > 0f)
                ataxiaDmg -= 1.5f;
            if (ataxiaDmg < 0f)
                ataxiaDmg = 0f;
            if (xerocDmg > 0f)
                xerocDmg -= 2f;
            if (xerocDmg < 0f)
                xerocDmg = 0f;
            if (aBulwarkRareMeleeBoostTimer > 0)
                aBulwarkRareMeleeBoostTimer--;
            if (bossRushImmunityFrameCurseTimer > 0)
                bossRushImmunityFrameCurseTimer--;
            if (gaelRageAttackCooldown > 0)
                gaelRageAttackCooldown--;
            if (projRefRareLifeRegenCounter > 0)
                projRefRareLifeRegenCounter--;
            if (hurtSoundTimer > 0)
                hurtSoundTimer--;
            if (icicleCooldown > 0)
                icicleCooldown--;
            if (statisTimer > 0 && Player.dashDelay >= 0)
                statisTimer = 0;
            if (hallowedRuneCooldown > 0)
                hallowedRuneCooldown--;
            if (sulphurBubbleCooldown > 0)
                sulphurBubbleCooldown--;
            if (forbiddenCooldown > 0)
                forbiddenCooldown--;
            if (tornadoCooldown > 0)
                tornadoCooldown--;
            if (ladHearts > 0)
                ladHearts--;
            if (titanBoost > 0)
                titanBoost--;
            if (prismaticLasers > 0)
                prismaticLasers--;
            if (dogTextCooldown > 0)
                dogTextCooldown--;
            if (titanCooldown > 0)
                titanCooldown--;
            if (fungalSymbioteTimer > 0)
                fungalSymbioteTimer--;
            if (aBulwarkRareTimer > 0)
                aBulwarkRareTimer--;
            if (hellbornBoost > 0)
                hellbornBoost--;
            if (persecutedEnchantSummonTimer < 1800)
                persecutedEnchantSummonTimer++;
            else
            {
                persecutedEnchantSummonTimer = 0;
                if (Main.myPlayer == Player.whoAmI && persecutedEnchant && NPC.CountNPCS(ModContent.NPCType<DemonPortal>()) < 2)
                {
                    int tries = 0;
                    Vector2 spawnPosition;
                    do
                    {
                        spawnPosition = Player.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(270f, 420f);
                        tries++;
                    }
                    while (Collision.SolidCollision(spawnPosition - Vector2.One * 24f, 48, 24) && tries < 100);
                    CalamityNetcode.NewNPC_ClientSide(spawnPosition, ModContent.NPCType<DemonPortal>(), Player);
                }
            }
            if (Player.miscCounter % 20 == 0)
                canFireAtaxiaRangedProjectile = true;
            if (Player.miscCounter % 100 == 0)
                canFireBloodflareMageProjectile = true;
            if (Player.miscCounter % 150 == 0)
            {
                canFireGodSlayerRangedProjectile = true;
                canFireBloodflareRangedProjectile = true;
                canFireAtaxiaRogueProjectile = true;
            }
            if (reaverRegenCooldown < 60 && reaverRegen)
                reaverRegenCooldown++;
            else
                reaverRegenCooldown = 0;
            if (roverDrive)
            {
                if (roverDriveTimer < CalamityUtils.SecondsToFrames(30f))
                    roverDriveTimer++;
                if (roverDriveTimer >= CalamityUtils.SecondsToFrames(30f))
                    roverDriveTimer = 0;
            }
            else
                roverDriveTimer = 616; // Doesn't reset to zero to prevent exploits
            if (auralisAurora > 0)
                auralisAurora--;
            if (auralisAuroraCooldown > 0)
                auralisAuroraCooldown--;
            if (MythrilFlareSpawnCountdown > 0)
                MythrilFlareSpawnCountdown--;
            if (AdamantiteSetDecayDelay > 0)
                AdamantiteSetDecayDelay--;
            else if (AdamantiteSet)
            {
                adamantiteSetDefenseBoostInterpolant -= 1f / AdamantiteArmorSetChange.TimeUntilBoostCompletelyDecays;
                adamantiteSetDefenseBoostInterpolant = MathHelper.Clamp(adamantiteSetDefenseBoostInterpolant, 0f, 1f);
            }
            else
                adamantiteSetDefenseBoostInterpolant = 0f;

            // God Slayer Armor dash debuff immunity
            if (dashMod == 9 && Player.dashDelay < 0)
            {
                foreach (int debuff in CalamityLists.debuffList)
                    Player.buffImmune[debuff] = true;
            }

            // Auric dye cinders.
            int auricDyeCount = Player.dye.Count(dyeItem => dyeItem.type == ModContent.ItemType<AuricDye>());
            if (auricDyeCount > 0)
            {
                int sparkCreationChance = (int)MathHelper.Lerp(15f, 50f, Utils.GetLerpValue(4f, 1f, auricDyeCount, true));
                if (Main.rand.NextBool(sparkCreationChance))
                {
                    Dust spark = Dust.NewDustDirect(Player.position, Player.width, Player.height, 267);
                    spark.color = Color.Lerp(Color.Cyan, Color.SeaGreen, Main.rand.NextFloat(0.5f));
                    spark.velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 1.33f) * Main.rand.NextFloat(2f, 5.4f);
                    spark.noGravity = true;
                }
            }

            // Silva invincibility effects
            if (silvaCountdown > 0 && hasSilvaEffect && silvaSet)
            {
                foreach (int debuff in CalamityLists.debuffList)
                    Player.buffImmune[debuff] = true;

                silvaCountdown -= 1;
                if (silvaCountdown <= 0)
                {
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(Mod, "Sounds/Custom/AbilitySounds/SilvaDispel"), Player.Center);
                    Player.AddCooldown(SilvaRevive.ID, CalamityUtils.SecondsToFrames(5 * 60));
                }

                for (int j = 0; j < 2; j++)
                {
                    int green = Dust.NewDust(Player.position, Player.width, Player.height, 157, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 2f);
                    Main.dust[green].position.X += (float)Main.rand.Next(-20, 21);
                    Main.dust[green].position.Y += (float)Main.rand.Next(-20, 21);
                    Main.dust[green].velocity *= 0.9f;
                    Main.dust[green].noGravity = true;
                    Main.dust[green].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    Main.dust[green].shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                    if (Main.rand.NextBool(2))
                        Main.dust[green].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                }
            }
            if (!Player.HasCooldown(SilvaRevive.ID) && hasSilvaEffect && silvaCountdown <= 0 && !areThereAnyDamnBosses && !areThereAnyDamnEvents)
            {
                silvaCountdown = 480;
                hasSilvaEffect = false;
            }

            // Tarragon cloak effects
            if (tarragonCloak)
            {
                tarraDefenseTime--;
                if (tarraDefenseTime <= 0)
                {
                    tarraDefenseTime = 600;
                    if (Player.whoAmI == Main.myPlayer)
                        Player.AddCooldown(Cooldowns.TarragonCloak.ID, CalamityUtils.SecondsToFrames(30));
                }

                for (int j = 0; j < 2; j++)
                {
                    int green = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, 157, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 2f);
                    Dust dust = Main.dust[green];
                    dust.position.X += (float)Main.rand.Next(-20, 21);
                    dust.position.Y += (float)Main.rand.Next(-20, 21);
                    dust.velocity *= 0.9f;
                    dust.noGravity = true;
                    dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    dust.shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                    if (Main.rand.NextBool(2))
                        dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                }
            }

            // Tarragon immunity effects
            if (tarraThrowing)
            {
                // The iframes from the evasion are disabled by dodge disabling effects.
                if (tarragonImmunity && !disableAllDodges)
                    Player.GiveIFrames(2, true);


                if (tarraThrowingCrits >= 25)
                {
                    tarraThrowingCrits = 0;
                    if (Player.whoAmI == Main.myPlayer && !disableAllDodges)
                        Player.AddBuff(ModContent.BuffType<Buffs.StatBuffs.TarragonImmunity>(), 180, false);
                }

                for (int l = 0; l < Player.MaxBuffs; l++)
                {
                    int hasBuff = Player.buffType[l];
                    if (Player.buffTime[l] <= 2 && hasBuff == ModContent.BuffType<Buffs.StatBuffs.TarragonImmunity>())
                        if (Player.whoAmI == Main.myPlayer)
                            Player.AddCooldown(Cooldowns.TarragonImmunity.ID, CalamityUtils.SecondsToFrames(25));

                    bool shouldAffect = CalamityLists.debuffList.Contains(hasBuff);
                    if (shouldAffect)
                        throwingDamage += 0.1f;
                }
            }

            // Bloodflare pickup spawn cooldowns
            if (bloodflareSet)
            {
                if (bloodflareHeartTimer > 0)
                    bloodflareHeartTimer--;
            }

            // Bloodflare frenzy effects
            if (bloodflareMelee)
            {
                if (bloodflareMeleeHits >= 15)
                {
                    bloodflareMeleeHits = 0;
                    if (Player.whoAmI == Main.myPlayer)
                        Player.AddBuff(ModContent.BuffType<BloodflareBloodFrenzy>(), 302, false);
                }

                if (bloodflareFrenzy)
                {
                    for (int l = 0; l < Player.MaxBuffs; l++)
                    {
                        int hasBuff = Player.buffType[l];
                        if (Player.buffTime[l] <= 2 && hasBuff == ModContent.BuffType<BloodflareBloodFrenzy>() && Player.whoAmI == Main.myPlayer)
                            Player.AddCooldown(BloodflareFrenzy.ID, CalamityUtils.SecondsToFrames(30));
                    }

                    Player.GetCritChance(DamageClass.Melee) += 25;
                    Player.GetDamage(DamageClass.Melee) += 0.25f;

                    for (int j = 0; j < 2; j++)
                    {
                        int blood = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Blood, 0f, 0f, 100, default, 2f);
                        Dust dust = Main.dust[blood];
                        dust.position.X += (float)Main.rand.Next(-20, 21);
                        dust.position.Y += (float)Main.rand.Next(-20, 21);
                        dust.velocity *= 0.9f;
                        dust.noGravity = true;
                        dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                        dust.shader = GameShaders.Armor.GetSecondaryShader(Player.cWaist, Player);
                        if (Main.rand.NextBool(2))
                            dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
                    }
                }
            }

            // Raider Talisman bonus
            if (raiderTalisman)
            {
                // Nanotech use to have an exclusive nerf here, but since they are currently equal, there
                // is no check to indicate such.
                float damageMult = 0.15f;
                throwingDamage += raiderStack / 150f * damageMult;
            }

            if (kamiBoost)
                Player.GetDamage<GenericDamageClass>() += 0.15f;

            if (avertorBonus)
                Player.GetDamage<GenericDamageClass>() += 0.1f;

            if (roverDriveTimer < 616)
            {
                Player.statDefense += 15;
                if (roverDriveTimer > 606)
                    Player.statDefense -= roverDriveTimer - 606; //so it scales down when the shield dies
            }

            // Fairy Boots bonus
            if (fairyBoots)
            {
                if (Player.isNearFairy())
                {
                    Player.lifeRegen += 4;
                    Player.statDefense += 10;
                    Player.moveSpeed += 0.1f;
                }
            }

            // Absorber bonus
            if (absorber)
            {
                Player.moveSpeed += 0.05f;
                Player.jumpSpeedBoost += 0.25f;
                Player.thorns += 0.5f;
                Player.endurance += sponge ? 0.15f : 0.1f;

                if (Player.StandingStill() && Player.itemAnimation == 0)
                    Player.manaRegenBonus += 4;
            }

            // Sea Shell bonus
            if (seaShell)
            {
                if (Player.IsUnderwater())
                {
                    Player.statDefense += 3;
                    Player.endurance += 0.05f;
                    Player.moveSpeed += 0.1f;
                    Player.ignoreWater = true;
                }
            }

            // Affliction bonus
            if (affliction || afflicted)
            {
                Player.endurance += 0.07f;
                Player.statDefense += 13;
                Player.GetDamage<GenericDamageClass>() += 0.1f;
            }

            // Ambrosial Ampoule bonus and other light-granting bonuses
            float[] light = new float[3];
            if ((rOoze && !Main.dayTime) || aAmpoule)
            {
                light[0] += 1f;
                light[1] += 1f;
                light[2] += 0.6f;
            }
            if (aAmpoule)
            {
                Player.endurance += 0.07f;
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.CursedInferno] = true;
                Player.buffImmune[ModContent.BuffType<BurningBlood>()] = true;
            }
            if (cFreeze)
            {
                light[0] += 0.3f;
                light[1] += Main.DiscoG / 400f;
                light[2] += 0.5f;
            }
            if (aquaticHeartIce)
            {
                light[0] += 0.35f;
                light[1] += 1f;
                light[2] += 1.25f;
            }
            if (aquaticHeart)
            {
                light[0] += 0.1f;
                light[1] += 1f;
                light[2] += 1.5f;
            }
            if (tarraSummon)
            {
                light[0] += 0f;
                light[1] += 3f;
                light[2] += 0f;
            }
            if (forbiddenCirclet)
            {
                light[0] += 0.8f;
                light[1] += 0.7f;
                light[2] += 0.2f;
            }
            Lighting.AddLight((int)(Player.Center.X / 16f), (int)(Player.Center.Y / 16f), light[0], light[1], light[2]);

            // Blazing Core bonus
            if (blazingCore)
                Player.endurance += 0.1f;

            //Permafrost's Concoction bonuses/debuffs
            if (permafrostsConcoction)
                Player.manaCost *= 0.85f;

            if (encased)
            {
                Player.statDefense += 30;
                Player.frozen = true;
                Player.velocity.X = 0f;
                Player.velocity.Y = -0.4f; //should negate gravity

                int ice = Dust.NewDust(Player.position, Player.width, Player.height, 88);
                Main.dust[ice].noGravity = true;
                Main.dust[ice].velocity *= 2f;

                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[ModContent.BuffType<GlacialState>()] = true;
            }

            // Cosmic Discharge Cosmic Freeze buff, gives surrounding enemies the Glacial State debuff
            if (cFreeze)
            {
                int buffType = ModContent.BuffType<GlacialState>();
                float freezeDist = 200f;
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (Main.rand.NextBool(5))
                    {
                        for (int l = 0; l < Main.maxNPCs; l++)
                        {
                            NPC npc = Main.npc[l];
                            if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
                                continue;
                            if (!npc.buffImmune[buffType] && Vector2.Distance(Player.Center, npc.Center) <= freezeDist)
                            {
                                if (npc.FindBuffIndex(buffType) == -1)
                                    npc.AddBuff(buffType, 60, false);
                            }
                        }
                    }
                }
            }

            // Remove Purified Jam and Lul accessory thorn damage exploits
            if (invincible || lol)
            {
                Player.thorns = 0f;
                Player.turtleThorns = false;
            }

            // Vortex Armor nerf
            if (Player.vortexStealthActive)
            {
                Player.GetDamage(DamageClass.Ranged) -= (1f - Player.stealth) * 0.4f; // Change 80 to 40
                Player.GetCritChance(DamageClass.Ranged) -= (int)((1f - Player.stealth) * 5f); // Change 20 to 15
            }

            // Polaris fish stuff
            if (polarisBoost)
            {
                Player.endurance += 0.01f;
                Player.statDefense += 2;
            }
            if (!polarisBoost || Player.ActiveItem().type != ModContent.ItemType<PolarisParrotfish>())
            {
                polarisBoost = false;
                if (Player.FindBuffIndex(ModContent.BuffType<PolarisBuff>()) > -1)
                    Player.ClearBuff(ModContent.BuffType<PolarisBuff>());

                polarisBoostCounter = 0;
                polarisBoostTwo = false;
                polarisBoostThree = false;
            }
            if (polarisBoostCounter >= 20)
            {
                polarisBoostTwo = false;
                polarisBoostThree = true;
            }
            else if (polarisBoostCounter >= 10)
                polarisBoostTwo = true;

            // Calcium Potion buff
            if (calcium)
                Player.noFallDmg = true;

            // Ceaseless Hunger Potion buff
            if (ceaselessHunger)
            {
                for (int j = 0; j < Main.maxItems; j++)
                {
                    Item item = Main.item[j];
                    if (item.active && item.noGrabDelay == 0 && item.playerIndexTheItemIsReservedFor == Player.whoAmI)
                    {
                        item.beingGrabbed = true;
                        if (Player.Center.X > item.Center.X)
                        {
                            if (item.velocity.X < 90f + Player.velocity.X)
                            {
                                item.velocity.X += 9f;
                            }
                            if (item.velocity.X < 0f)
                            {
                                item.velocity.X += 9f * 0.75f;
                            }
                        }
                        else
                        {
                            if (item.velocity.X > -90f + Player.velocity.X)
                            {
                                item.velocity.X -= 9f;
                            }
                            if (item.velocity.X > 0f)
                            {
                                item.velocity.X -= 9f * 0.75f;
                            }
                        }

                        if (Player.Center.Y > item.Center.Y)
                        {
                            if (item.velocity.Y < 90f)
                            {
                                item.velocity.Y += 9f;
                            }
                            if (item.velocity.Y < 0f)
                            {
                                item.velocity.Y += 9f * 0.75f;
                            }
                        }
                        else
                        {
                            if (item.velocity.Y > -90f)
                            {
                                item.velocity.Y -= 9f;
                            }
                            if (item.velocity.Y > 0f)
                            {
                                item.velocity.Y -= 9f * 0.75f;
                            }
                        }
                    }
                }
            }

            // Plagued Fuel Pack and Blunder Booster effects
            if (jetPackDash > 0 && Player.whoAmI == Main.myPlayer)
            {
                int velocityAmt = blunderBooster ? 35 : 25;
                int velocityMult = jetPackDash > 1 ? velocityAmt : 5;
                Player.velocity = new Vector2(jetPackDirection, -1) * velocityMult;

                if (blunderBooster)
                {
                    int lightningCount = Main.rand.Next(2, 7);
                    var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<BlunderBooster>()));
                    for (int i = 0; i < lightningCount; i++)
                    {
                        Vector2 lightningVel = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1));
                        lightningVel.Normalize();
                        lightningVel *= Main.rand.NextFloat(1f, 2f);
                        int projectile = Projectile.NewProjectile(source, Player.Center, lightningVel, ModContent.ProjectileType<BlunderBoosterLightning>(), (int)(30 * Player.RogueDamage()), 0, Player.whoAmI, Main.rand.Next(2), 0f);
                        Main.projectile[projectile].timeLeft = Main.rand.Next(180, 240);
                        if (projectile.WithinBounds(Main.maxProjectiles))
                            Main.projectile[projectile].Calamity().forceTypeless = true;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        int dust = Dust.NewDust(Player.Center, 1, 1, 60, Player.velocity.X * -0.1f, Player.velocity.Y * -0.1f, 100, default, 3.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 1.2f;
                        Main.dust[dust].velocity.Y -= 0.15f;
                    }
                }
                else if (plaguedFuelPack)
                {
                    int numClouds = Main.rand.Next(2, 10);
                    var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<PlaguedFuelPack>()));
                    for (int i = 0; i < numClouds; i++)
                    {
                        Vector2 cloudVelocity = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1));
                        cloudVelocity.Normalize();
                        cloudVelocity *= Main.rand.NextFloat(0f, 1f);
                        int projectile = Projectile.NewProjectile(source, Player.Center, cloudVelocity, ModContent.ProjectileType<PlaguedFuelPackCloud>(), (int)(20 * Player.RogueDamage()), 0, Player.whoAmI, 0, 0);
                        Main.projectile[projectile].timeLeft = Main.rand.Next(180, 240);
                        if (projectile.WithinBounds(Main.maxProjectiles))
                            Main.projectile[projectile].Calamity().forceTypeless = true;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        int dust = Dust.NewDust(Player.Center, 1, 1, 89, Player.velocity.X * -0.1f, Player.velocity.Y * -0.1f, 100, default, 3.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 1.2f;
                        Main.dust[dust].velocity.Y -= 0.15f;
                    }
                }
            }

            // Gravistar Sabaton effects
            if (gSabaton && Player.whoAmI == Main.myPlayer)
            {
                var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<GravistarSabaton>()));
                if (gSabatonCooldown <= 0 && !Player.mount.Active)
                {
                    if (Player.controlDown && Player.releaseDown && Player.position.Y != Player.oldPosition.Y)
                    {
                        gSabatonFall = 300;
                        gSabatonCooldown = 480; //8 second cooldown
                        Player.gravity *= 2f;
                        Projectile.NewProjectile(source, Player.Center.X, Player.Center.Y + (Player.height / 5f), Player.velocity.X, Player.velocity.Y, ModContent.ProjectileType<SabatonSlam>(), 0, 0, Player.whoAmI);
                    }
                }
                if (gSabatonCooldown == 1) //dust when ready to use again
                {
                    for (int i = 0; i < 66; i++)
                    {
                        int d = Dust.NewDust(Player.position, Player.width, Player.height, Main.rand.NextBool(2) ? ModContent.DustType<AstralBlue>() : ModContent.DustType<AstralOrange>(), 0, 0, 100, default, 2.6f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].fadeIn = 1f;
                        Main.dust[d].velocity *= 6.6f;
                    }
                }
            }

            // This section of code ensures set bonuses and accessories with cooldowns go on cooldown immediately if the armor or accessory is removed.
            if (!brimflameSet && brimflameFrenzy)
            {
                brimflameFrenzy = false;
                Player.ClearBuff(ModContent.BuffType<BrimflameFrenzyBuff>());
                Player.AddCooldown(BrimflameFrenzy.ID, BrimflameScowl.CooldownLength);
            }
            if (!bloodflareMelee && bloodflareFrenzy)
            {
                bloodflareFrenzy = false;
                Player.ClearBuff(ModContent.BuffType<BloodflareBloodFrenzy>());
                Player.AddCooldown(BloodflareFrenzy.ID, CalamityUtils.SecondsToFrames(30));
            }
            if (!tarraMelee && tarragonCloak)
            {
                tarragonCloak = false;
                Player.ClearBuff(ModContent.BuffType<Buffs.StatBuffs.TarragonCloak>());
                Player.AddCooldown(Cooldowns.TarragonCloak.ID, CalamityUtils.SecondsToFrames(30));
            }
            if (!tarraThrowing && tarragonImmunity)
            {
                tarragonImmunity = false;
                Player.ClearBuff(ModContent.BuffType<Buffs.StatBuffs.TarragonImmunity>());
                Player.AddCooldown(Cooldowns.TarragonImmunity.ID, CalamityUtils.SecondsToFrames(25));
            }

            bool hasOmegaBlueCooldown = cooldowns.TryGetValue(OmegaBlue.ID, out CooldownInstance omegaBlueCD);
            if (!omegaBlueSet && hasOmegaBlueCooldown && omegaBlueCD.timeLeft > 1500)
            {
                Player.ClearBuff(ModContent.BuffType<AbyssalMadness>());
                omegaBlueCD.timeLeft = 1500;
            }

            bool hasPlagueBlackoutCD = cooldowns.TryGetValue(PlagueBlackout.ID, out CooldownInstance plagueBlackoutCD);
            if (!plagueReaper && hasPlagueBlackoutCD && plagueBlackoutCD.timeLeft > 1500)
                plagueBlackoutCD.timeLeft = 1500;

            if (!prismaticSet && prismaticLasers > 1800)
            {
                prismaticLasers = 1800;
                Player.AddCooldown(PrismaticLaser.ID, 1800);
            }
            if (!angelicAlliance && divineBless)
            {
                divineBless = false;
                Player.ClearBuff(ModContent.BuffType<Buffs.StatBuffs.DivineBless>());
                Player.AddCooldown(Cooldowns.DivineBless.ID, CalamityUtils.SecondsToFrames(60));
            }

            // Armageddon's Dodge Disable feature puts Shadow Dodge/Holy Protection on permanent cooldown
            if (disableAllDodges)
            {
                if (Player.shadowDodgeTimer < 2)
                    Player.shadowDodgeTimer = 2;
            }
        }
        #endregion

        #region Abyss Effects
        private void AbyssEffects()
        {
            int lightStrength = GetTotalLightStrength();
            abyssLightLevelStat = lightStrength;

            if (ZoneAbyss)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    // Abyss depth variables
                    Point point = Player.Center.ToTileCoordinates();
                    double abyssSurface = Main.rockLayer - Main.maxTilesY * 0.05;
                    double abyssLevel1 = Main.rockLayer + Main.maxTilesY * 0.03;
                    double totalAbyssDepth = Main.maxTilesY - 250D - abyssSurface;
                    double totalAbyssDepthFromLayer1 = Main.maxTilesY - 250D - abyssLevel1;
                    double playerAbyssDepth = point.Y - abyssSurface;
                    double playerAbyssDepthFromLayer1 = point.Y - abyssLevel1;
                    double depthRatio = playerAbyssDepth / totalAbyssDepth;
                    double depthRatioFromAbyssLayer1 = playerAbyssDepthFromLayer1 / totalAbyssDepthFromLayer1;

                    // Darkness strength scales smoothly with how deep you are.
                    float darknessStrength = (float)depthRatio;

                    // Reduce the power of abyss darkness based on your light level.
                    float multiplier = 1f;
                    switch (lightStrength)
                    {
                        case 0:
                            break;
                        case 1:
                            multiplier = 0.85f;
                            break;
                        case 2:
                            multiplier = 0.7f;
                            break;
                        case 3:
                            multiplier = 0.55f;
                            break;
                        case 4:
                            multiplier = 0.4f;
                            break;
                        case 5:
                            multiplier = 0.25f;
                            break;
                        case 6:
                            multiplier = 0.15f;
                            break;
                        case 7:
                            multiplier = 0.1f;
                            break;
                        default:
                            multiplier = 0.05f;
                            break;
                    }

                    // Increased darkness in Death Mode
                    if (CalamityWorld.death)
                        multiplier += (1f - multiplier) * 0.1f;

                    // Modify darkness variable
                    caveDarkness = darknessStrength * multiplier;

                    // Nebula Headcrab darkness effect
                    if (!Player.headcovered)
                    {
                        float screenObstructionAmt = MathHelper.Clamp(caveDarkness, 0f, 0.95f);
                        float targetValue = MathHelper.Clamp(screenObstructionAmt * 0.7f, 0.1f, 0.3f);
                        ScreenObstruction.screenObstruction = MathHelper.Lerp(ScreenObstruction.screenObstruction, screenObstructionAmt, targetValue);
                    }

                    // Breath lost while at zero breath
                    double breathLoss = point.Y > abyssLevel1 ? 50D * depthRatioFromAbyssLayer1 : 0D;

                    // Breath Loss Multiplier, depending on gear
                    double breathLossMult = 1D -
                        (Player.gills ? 0.2 : 0D) - // 0.8
                        (Player.accDivingHelm ? 0.25 : 0D) - // 0.75
                        (Player.arcticDivingGear ? 0.25 : 0D) - // 0.75
                        (aquaticEmblem ? 0.25 : 0D) - // 0.75
                        (Player.accMerman ? 0.3 : 0D) - // 0.7
                        (victideSet ? 0.2 : 0D) - // 0.85
                        ((aquaticHeart && NPC.downedBoss3) ? 0.3 : 0D) - // 0.7
                        (abyssalDivingSuit ? 0.3 : 0D); // 0.7

                    // Limit the multiplier to 5%
                    if (breathLossMult < 0.05)
                        breathLossMult = 0.05;

                    // Reduce breath lost while at zero breath, depending on gear
                    breathLoss *= breathLossMult;

                    // Stat Meter stat
                    abyssBreathLossStat = (int)breathLoss;

                    // Defense loss
                    int defenseLoss = (int)(120D * depthRatio);

                    // Anechoic Plating reduces defense loss by 66%
                    // Fathom Swarmer Breastplate reduces defense loss by 40%
                    // In tandem, reduces defense loss by 80%
                    if (anechoicPlating && fathomSwarmerBreastplate)
                        defenseLoss = (int)(defenseLoss * 0.2f);
                    else if (anechoicPlating)
                        defenseLoss /= 3;
                    else if (fathomSwarmerBreastplate)
                        defenseLoss = (int)(defenseLoss * 0.6f);

                    // Reduce defense
                    Player.statDefense -= defenseLoss;

                    // Stat Meter stat
                    abyssDefenseLossStat = defenseLoss;

                    // Bleed effect based on abyss layer
                    if (ZoneAbyssLayer4)
                    {
                        Player.bleed = true;
                    }
                    else if (ZoneAbyssLayer3)
                    {
                        if (!abyssalDivingSuit)
                            Player.bleed = true;
                    }
                    else if (ZoneAbyssLayer2)
                    {
                        if (!depthCharm)
                            Player.bleed = true;
                    }

                    // Ticks (frames) until breath is deducted from the breath meter
                    double tick = 12D * (1D - depthRatio);

                    // Prevent 0
                    if (tick < 1D)
                        tick = 1D;

                    // Tick (frame) multiplier, depending on gear
                    double tickMult = 1D +
                        (Player.gills ? 4D : 0D) + // 5
                        (Player.ignoreWater ? 5D : 0D) + // 10
                        (Player.accDivingHelm ? 10D : 0D) + // 20
                        (Player.arcticDivingGear ? 10D : 0D) + // 30
                        (aquaticEmblem ? 10D : 0D) + // 40
                        (Player.accMerman ? 15D : 0D) + // 55
                        (victideSet ? 5D : 0D) + // 60
                        ((aquaticHeart && NPC.downedBoss3) ? 15D : 0D) + // 75
                        (abyssalDivingSuit ? 15D : 0D); // 90

                    // Limit the multiplier to 50
                    if (tickMult > 50D)
                        tickMult = 50D;

                    // Increase ticks (frames) until breath is deducted, depending on gear
                    tick *= tickMult;

                    // Stat Meter stat
                    abyssBreathLossRateStat = (int)tick;

                    // Reduce breath over ticks (frames)
                    abyssBreathCD++;
                    if (abyssBreathCD >= (int)tick)
                    {
                        // Reset modded breath variable
                        abyssBreathCD = 0;

                        // Reduce breath
                        if (Player.breath > 0)
                            Player.breath -= (int)(cDepth ? breathLoss + 1D : breathLoss);
                    }

                    // If breath is greater than 0 and player has gills or is merfolk, balance out the effects by reducing breath
                    if (Player.breath > 0)
                    {
                        if (Player.gills || Player.merman)
                            Player.breath -= 3;
                    }

                    // Life loss at zero breath
                    int lifeLossAtZeroBreath = (int)(12D * depthRatio);

                    // Resistance to life loss at zero breath
                    int lifeLossAtZeroBreathResist = 0 +
                        (depthCharm ? 3 : 0) +
                        (abyssalDivingSuit ? 6 : 0);

                    // Reduce life loss, depending on gear
                    lifeLossAtZeroBreath -= lifeLossAtZeroBreathResist;

                    // Prevent negatives
                    if (lifeLossAtZeroBreath < 0)
                        lifeLossAtZeroBreath = 0;

                    // Stat Meter stat
                    abyssLifeLostAtZeroBreathStat = lifeLossAtZeroBreath;

                    // Check breath value
                    if (Player.breath <= 0)
                    {
                        // Reduce life
                        Player.statLife -= lifeLossAtZeroBreath;

                        // Special kill code if the life loss kills the player
                        if (Player.statLife <= 0)
                        {
                            abyssDeath = true;
                            KillPlayer();
                        }
                    }
                }
            }
            else
            {
                abyssBreathCD = 0;
                abyssDeath = false;
            }
        }
        #endregion

        #region Calamitas Enchantment Held Item Effects
        public static void EnchantHeldItemEffects(Player player, CalamityPlayer modPlayer, Item heldItem)
        {
            if (heldItem.IsAir)
                return;

            // Exhaustion recharge effects.
            foreach (Item item in player.inventory)
            {
                if (item.IsAir)
                    continue;

                if (item.Calamity().AppliedEnchantment.HasValue && item.Calamity().AppliedEnchantment.Value.ID == 600)
                {
                    // Initialize the exhaustion if it is currently not defined.
                    if (item.Calamity().DischargeEnchantExhaustion <= 0f)
                        item.Calamity().DischargeEnchantExhaustion = CalamityGlobalItem.DischargeEnchantExhaustionCap;

                    // Slowly recharge the weapon over time. This is depleted when the item is actaully used.
                    else if (item.Calamity().DischargeEnchantExhaustion < CalamityGlobalItem.DischargeEnchantExhaustionCap)
                        item.Calamity().DischargeEnchantExhaustion++;
                }
                else
                    item.Calamity().DischargeEnchantExhaustion = 0f;
            }

            if (!heldItem.Calamity().AppliedEnchantment.HasValue || heldItem.Calamity().AppliedEnchantment.Value.HoldEffect is null)
                return;

            heldItem.Calamity().AppliedEnchantment.Value.HoldEffect(player);

            // Weak brimstone flame hold curse effect.
            if (modPlayer.flamingItemEnchant)
                player.AddBuff(ModContent.BuffType<WeakBrimstoneFlames>(), 10);
        }
        #endregion

        #region Max Life And Mana Effects
        private void MaxLifeAndManaEffects()
        {
            // New textures
            if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
            {
                Asset<Texture2D> rain3 = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Rain3");
                Asset<Texture2D> rainOriginal = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/RainOriginal");
                Asset<Texture2D> mana2 = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Mana2");
                Asset<Texture2D> mana3 = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Mana3");
                Asset<Texture2D> mana4 = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Mana4");
                Asset<Texture2D> manaOriginal = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ManaOriginal");
                Asset<Texture2D> carpetAuric = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/AuricCarpet");
                Asset<Texture2D> carpetOriginal = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Carpet");

                int totalManaBoost =
                    (pHeart ? 1 : 0) +
                    (eCore ? 1 : 0) +
                    (cShard ? 1 : 0);
                switch (totalManaBoost)
                {
                    default:
                        TextureAssets.Mana = manaOriginal;
                        break;
                    case 3:
                        TextureAssets.Mana = mana4;
                        break;
                    case 2:
                        TextureAssets.Mana = mana3;
                        break;
                    case 1:
                        TextureAssets.Mana = mana2;
                        break;
                }

                if (Main.bloodMoon)
                    TextureAssets.Rain = rainOriginal;
                else if (Main.raining && ZoneSulphur)
                    TextureAssets.Rain = rain3;
                else
                    TextureAssets.Rain = rainOriginal;

                if (auricSet)
                    TextureAssets.FlyingCarpet = carpetAuric;
                else
                    TextureAssets.FlyingCarpet = carpetOriginal;
            }
        }
        #endregion

        #region Standing Still Effects
        private void StandingStillEffects()
        {
            // Rogue Stealth
            UpdateRogueStealth();

            // Trinket of Chi bonus
            if (trinketOfChi)
            {
                if (trinketOfChiBuff)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.5f;
                    if (Player.itemAnimation > 0)
                        chiBuffTimer = 0;
                }

                if (Player.StandingStill(0.1f) && !Player.mount.Active)
                {
                    if (chiBuffTimer < 60)
                        chiBuffTimer++;
                    else
                        Player.AddBuff(ModContent.BuffType<ChiBuff>(), 6);
                }
                else
                    chiBuffTimer--;
            }
            else
                chiBuffTimer = 0;

            // Aquatic Emblem bonus
            if (aquaticEmblem)
            {
                if (Player.IsUnderwater() && Player.wet && !Player.lavaWet && !Player.honeyWet &&
                    !Player.mount.Active)
                {
                    if (aquaticBoost > 0f)
                    {
                        aquaticBoost -= 2f;
                        if (aquaticBoost <= 0f)
                        {
                            aquaticBoost = 0f;
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    aquaticBoost += 2f;
                    if (aquaticBoost > aquaticBoostMax)
                        aquaticBoost = aquaticBoostMax;
                    if (Player.mount.Active)
                        aquaticBoost = aquaticBoostMax;
                }

                Player.statDefense += (int)((1f - aquaticBoost * 0.0001f) * 50f);
                Player.moveSpeed -= (1f - aquaticBoost * 0.0001f) * 0.1f;
            }
            else
                aquaticBoost = aquaticBoostMax;

            // Auric bonus
            if (auricBoost)
            {
                if (Player.itemAnimation > 0)
                    modStealthTimer = 5;

                if (Player.StandingStill(0.1f) && !Player.mount.Active)
                {
                    if (modStealthTimer == 0 && modStealth > 0f)
                    {
                        modStealth -= 0.015f;
                        if (modStealth <= 0f)
                        {
                            modStealth = 0f;
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    float playerVel = Math.Abs(Player.velocity.X) + Math.Abs(Player.velocity.Y);
                    modStealth += playerVel * 0.0075f;
                    if (modStealth > 1f)
                        modStealth = 1f;
                    if (Player.mount.Active)
                        modStealth = 1f;
                }

                float damageBoost = (1f - modStealth) * 0.2f;
                Player.GetDamage<GenericDamageClass>() += damageBoost;

                int critBoost = (int)((1f - modStealth) * 10f);
                AllCritBoost(critBoost);

                if (modStealthTimer > 0)
                    modStealthTimer--;
            }

            // Psychotic Amulet bonus
            else if (pAmulet)
            {
                if (Player.itemAnimation > 0)
                    modStealthTimer = 5;

                if (Player.StandingStill(0.1f) && !Player.mount.Active)
                {
                    if (modStealthTimer == 0 && modStealth > 0f)
                    {
                        modStealth -= 0.015f;
                        if (modStealth <= 0f)
                        {
                            modStealth = 0f;
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    float playerVel = Math.Abs(Player.velocity.X) + Math.Abs(Player.velocity.Y);
                    modStealth += playerVel * 0.0075f;
                    if (modStealth > 1f)
                        modStealth = 1f;
                    if (Player.mount.Active)
                        modStealth = 1f;
                }

                throwingDamage += (1f - modStealth) * 0.2f;
                throwingCrit += (int)((1f - modStealth) * 10f);
                Player.aggro -= (int)((1f - modStealth) * 750f);
                if (modStealthTimer > 0)
                    modStealthTimer--;
            }
            else
                modStealth = 1f;

            if (Player.ActiveItem().type == ModContent.ItemType<Auralis>() && Player.StandingStill(0.1f))
            {
                if (auralisStealthCounter < 300f)
                    auralisStealthCounter++;

                bool usingScope = false;
                if (!Main.gameMenu && Main.netMode != NetmodeID.Server)
                {
                    if (Player.noThrow <= 0 && !Player.lastMouseInterface || !(Main.CurrentPan == Vector2.Zero))
                    {
                        if (PlayerInput.UsingGamepad)
                        {
                            if (PlayerInput.GamepadThumbstickRight.Length() != 0f || !Main.SmartCursorIsUsed)
                            {
                                usingScope = true;
                            }
                        }
                        else if (Main.mouseRight)
                            usingScope = true;
                    }
                }

                int chargeDuration = CalamityUtils.SecondsToFrames(5f);
                int auroraDuration = CalamityUtils.SecondsToFrames(20f);

                if (usingScope && auralisAuroraCounter < chargeDuration + auroraDuration)
                    auralisAuroraCounter++;

                if (auralisAuroraCounter > chargeDuration + auroraDuration)
                {
                    auralisAuroraCounter = 0;
                    auralisAuroraCooldown = CalamityUtils.SecondsToFrames(30f);
                }

                if (auralisAuroraCounter > 0 && auralisAuroraCounter < chargeDuration && !usingScope)
                    auralisAuroraCounter--;

                if (auralisAuroraCounter > chargeDuration && auralisAuroraCounter < chargeDuration + auroraDuration && !usingScope)
                    auralisAuroraCounter = 0;
            }
            else
            {
                auralisStealthCounter = 0f;
                auralisAuroraCounter = 0;
            }
            if (auralisAuroraCooldown > 0)
            {
                if (auralisAuroraCooldown == 1)
                {
                    int dustAmt = 36;
                    for (int d = 0; d < dustAmt; d++)
                    {
                        Vector2 source = Vector2.Normalize(Player.velocity) * new Vector2((float)Player.width / 2f, (float)Player.height) * 1f; //0.75
                        source = source.RotatedBy((double)((float)(d - (dustAmt / 2 - 1)) * MathHelper.TwoPi / (float)dustAmt), default) + Player.Center;
                        Vector2 dustVel = source - Player.Center;
                        int blue = Dust.NewDust(source + dustVel, 0, 0, 229, dustVel.X, dustVel.Y, 100, default, 1.2f);
                        Main.dust[blue].noGravity = true;
                        Main.dust[blue].noLight = false;
                        Main.dust[blue].velocity = dustVel;
                    }
                    for (int d = 0; d < dustAmt; d++)
                    {
                        Vector2 source = Vector2.Normalize(Player.velocity) * new Vector2((float)Player.width / 2f, (float)Player.height) * 0.75f;
                        source = source.RotatedBy((double)((float)(d - (dustAmt / 2 - 1)) * MathHelper.TwoPi / (float)dustAmt), default) + Player.Center;
                        Vector2 dustVel = source - Player.Center;
                        int green = Dust.NewDust(source + dustVel, 0, 0, 107, dustVel.X, dustVel.Y, 100, default, 1.2f);
                        Main.dust[green].noGravity = true;
                        Main.dust[green].noLight = false;
                        Main.dust[green].velocity = dustVel;
                    }
                }
                auralisAuroraCounter = 0;
            }
        }
        #endregion

        #region Elysian Aegis Effects
        private void ElysianAegisEffects()
        {
            if (elysianAegis)
            {
                bool spawnDust = false;

                // Activate buff
                if (elysianGuard)
                {
                    if (Player.whoAmI == Main.myPlayer)
                        Player.AddBuff(ModContent.BuffType<ElysianGuard>(), 2, false);

                    float shieldBoostInitial = shieldInvinc;
                    shieldInvinc -= 0.08f;
                    if (shieldInvinc < 0f)
                        shieldInvinc = 0f;
                    else
                        spawnDust = true;

                    if (shieldInvinc == 0f && shieldBoostInitial != shieldInvinc && Main.netMode == NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);

                    float damageBoost = (5f - shieldInvinc) * 0.03f;
                    Player.GetDamage<GenericDamageClass>() += damageBoost;

                    int critBoost = (int)((5f - shieldInvinc) * 2f);
                    AllCritBoost(critBoost);

                    Player.aggro += (int)((5f - shieldInvinc) * 220f);
                    Player.statDefense += (int)((5f - shieldInvinc) * 8f);
                    Player.moveSpeed *= 0.85f;

                    if (Player.mount.Active)
                        elysianGuard = false;
                }

                // Remove buff
                else
                {
                    float shieldBoostInitial = shieldInvinc;
                    shieldInvinc += 0.08f;
                    if (shieldInvinc > 5f)
                        shieldInvinc = 5f;
                    else
                        spawnDust = true;

                    if (shieldInvinc == 5f && shieldBoostInitial != shieldInvinc && Main.netMode == NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, Player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                }

                // Emit dust
                if (spawnDust)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 vector = Vector2.UnitY.RotatedByRandom(Math.PI * 2D);
                        Dust dust = Main.dust[Dust.NewDust(Player.Center - vector * 30f, 0, 0, (int)CalamityDusts.ProfanedFire, 0f, 0f, 0, default, 1f)];
                        dust.noGravity = true;
                        dust.position = Player.Center - vector * (float)Main.rand.Next(5, 11);
                        dust.velocity = vector.RotatedBy(Math.PI / 2D, default) * 4f;
                        dust.scale = 0.5f + Main.rand.NextFloat();
                        dust.fadeIn = 0.5f;
                    }

                    if (Main.rand.NextBool(2))
                    {
                        Vector2 vector2 = Vector2.UnitY.RotatedByRandom(Math.PI * 2D);
                        Dust dust2 = Main.dust[Dust.NewDust(Player.Center - vector2 * 30f, 0, 0, 246, 0f, 0f, 0, default, 1f)];
                        dust2.noGravity = true;
                        dust2.position = Player.Center - vector2 * 12f;
                        dust2.velocity = vector2.RotatedBy(-Math.PI / 2D, default) * 2f;
                        dust2.scale = 0.5f + Main.rand.NextFloat();
                        dust2.fadeIn = 0.5f;
                    }
                }
            }
            else
                elysianGuard = false;
        }
        #endregion

        #region Other Buff Effects
        private void OtherBuffEffects()
        {
            if (gravityNormalizer)
            {
                Player.buffImmune[BuffID.VortexDebuff] = true;
                if (Player.InSpace())
                {
                    Player.gravity = Player.defaultGravity;
                    if (Player.wet)
                    {
                        if (Player.honeyWet)
                            Player.gravity = 0.1f;
                        else if (Player.merman)
                            Player.gravity = 0.3f;
                        else
                            Player.gravity = 0.2f;
                    }
                }
            }

            // Effigy of Decay effects
            if (decayEffigy)
            {
                Player.buffImmune[ModContent.BuffType<SulphuricPoisoning>()] = true;
                if (!ZoneAbyss)
                {
                    Player.gills = true;
                }
            }

            // Cobalt armor set effects.
            if (CobaltSet)
                CobaltArmorSetChange.ApplyMovementSpeedBonuses(Player);

            // Adamantite armor set effects.
            if (AdamantiteSet)
                Player.statDefense += AdamantiteSetDefenseBoost;

            if (astralInjection)
            {
                if (Player.statMana < Player.statManaMax2)
                    Player.statMana += 3;
                if (Player.statMana > Player.statManaMax2)
                    Player.statMana = Player.statManaMax2;
            }

            if (armorCrumbling)
            {
                throwingCrit += 5;
                Player.GetCritChance(DamageClass.Melee) += 5;
            }

            if (armorShattering)
            {
                if (Player.FindBuffIndex(ModContent.BuffType<ArmorCrumbling>()) > -1)
                    Player.ClearBuff(ModContent.BuffType<ArmorCrumbling>());
                throwingDamage += 0.08f;
                Player.GetDamage(DamageClass.Melee) += 0.08f;
                throwingCrit += 8;
                Player.GetCritChance(DamageClass.Melee) += 8;
            }

            if (holyWrath)
            {
                if (Player.FindBuffIndex(BuffID.Wrath) > -1)
                    Player.ClearBuff(BuffID.Wrath);
                Player.GetDamage<GenericDamageClass>() += 0.12f;
            }

            if (profanedRage)
            {
                if (Player.FindBuffIndex(BuffID.Rage) > -1)
                    Player.ClearBuff(BuffID.Rage);
                AllCritBoost(12);
            }

            if (shadow)
            {
                if (Player.FindBuffIndex(BuffID.Invisibility) > -1)
                    Player.ClearBuff(BuffID.Invisibility);
            }

            if (irradiated)
            {
                Player.statDefense -= 10;
                Player.moveSpeed -= 0.1f;
                Player.GetDamage<GenericDamageClass>() += 0.05f;
                Player.GetKnockback<SummonDamageClass>().Base += 0.5f;
            }

            if (rRage)
            {
                Player.GetDamage<GenericDamageClass>() += 0.3f;
                Player.statDefense += 5;
            }

            if (xRage)
                throwingDamage += 0.1f;

            if (xWrath)
                throwingCrit += 5;

            if (graxDefense)
            {
                Player.statDefense += 30;
                Player.endurance += 0.1f;
                Player.GetDamage(DamageClass.Melee) += 0.2f;
            }

            if (tFury)
            {
                Player.GetDamage(DamageClass.Melee) += 0.3f;
                Player.GetCritChance(DamageClass.Melee) += 10;
            }

            if (yPower)
            {
                Player.endurance += 0.06f;
                Player.statDefense += 8;
                Player.pickSpeed -= 0.05f;
                Player.GetDamage<GenericDamageClass>() += 0.06f;
                AllCritBoost(2);
                Player.GetKnockback<SummonDamageClass>().Base += 1f;
                Player.moveSpeed += 0.06f;
            }

            if (tScale)
            {
                Player.endurance += 0.05f;
                Player.statDefense += 5;
                Player.kbBuff = true;
                if (titanBoost > 0)
                {
                    Player.statDefense += 25;
                    Player.endurance += 0.1f;
                }
            }
            else
                titanBoost = 0;

            if (darkSunRing)
            {
                Player.maxMinions += 2;
                Player.GetDamage<GenericDamageClass>() += 0.12f;
                Player.GetKnockback<SummonDamageClass>().Base += 1.2f;
                Player.pickSpeed -= 0.15f;
                if (Main.eclipse || !Main.dayTime)
                    Player.statDefense += 15;
            }

            if (eGauntlet)
            {
                Player.kbGlove = true;
                Player.magmaStone = true;
                Player.GetDamage(DamageClass.Melee) += 0.15f;
                Player.GetCritChance(DamageClass.Melee) += 5;
                Player.lavaMax += 240;
            }

            if (bloodPactBoost)
            {
                Player.GetDamage<GenericDamageClass>() += 0.05f;
                Player.statDefense += 20;
                Player.endurance += 0.1f;
                Player.longInvince = true;
                Player.crimsonRegen = true;
            }

            // 50% movement speed bonus so that you don't feel like a snail in the early game.
            Player.moveSpeed += 0.5f;

            if (cirrusDress)
                Player.moveSpeed -= 0.2f;

            if (fabsolVodka)
                Player.GetDamage<GenericDamageClass>() += 0.08f;

            if (vodka)
            {
                Player.GetDamage<GenericDamageClass>() += 0.06f;
                AllCritBoost(2);
            }

            if (grapeBeer)
                Player.moveSpeed -= 0.05f;

            if (moonshine)
            {
                Player.statDefense += 10;
                Player.endurance += 0.05f;
            }

            if (rum)
                Player.moveSpeed += 0.1f;

            if (whiskey)
            {
                Player.GetDamage<GenericDamageClass>() += 0.04f;
                AllCritBoost(2);
            }

            if (everclear)
                Player.GetDamage<GenericDamageClass>() += 0.25f;

            if (bloodyMary)
            {
                if (Main.bloodMoon)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.15f;
                    AllCritBoost(7);
                    Player.moveSpeed += 0.1f;
                }
            }

            if (tequila)
            {
                if (Main.dayTime)
                {
                    Player.statDefense += 5;
                    Player.GetDamage<GenericDamageClass>() += 0.03f;
                    AllCritBoost(2);
                    Player.endurance += 0.03f;
                }
            }

            if (tequilaSunrise)
            {
                if (Main.dayTime)
                {
                    Player.statDefense += 10;
                    Player.GetDamage<GenericDamageClass>() += 0.07f;
                    AllCritBoost(3);
                    Player.endurance += 0.07f;
                }
            }

            if (caribbeanRum)
                Player.moveSpeed += 0.1f;

            if (cinnamonRoll)
            {
                Player.manaRegenDelay--;
                Player.manaRegenBonus += 10;
            }

            if (starBeamRye)
            {
                Player.GetDamage(DamageClass.Magic) += 0.08f;
                Player.manaCost *= 0.9f;
            }

            if (moscowMule)
            {
                Player.GetDamage<GenericDamageClass>() += 0.09f;
                AllCritBoost(3);
            }

            if (whiteWine)
                Player.GetDamage(DamageClass.Magic) += 0.1f;

            if (evergreenGin)
                Player.endurance += 0.05f;

            if (giantPearl)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && !areThereAnyDamnBosses)
                {
                    for (int m = 0; m < Main.maxNPCs; m++)
                    {
                        NPC npc = Main.npc[m];
                        if (!npc.active || npc.friendly || npc.dontTakeDamage)
                            continue;
                        float distance = (npc.Center - Player.Center).Length();
                        if (distance < 120f)
                            npc.AddBuff(ModContent.BuffType<PearlAura>(), 20, false);
                    }
                }
            }

            if (CalamityLists.scopedWeaponList.Contains(Player.ActiveItem().type))
                Player.scope = true;

            if (CalamityLists.highTestFishList.Contains(Player.ActiveItem().type))
                Player.accFishingLine = true;

            if (CalamityLists.boomerangList.Contains(Player.ActiveItem().type) && Player.invis)
                throwingDamage += 0.1f;

            if (CalamityLists.javelinList.Contains(Player.ActiveItem().type) && Player.invis)
                Player.GetArmorPenetration(DamageClass.Generic) += 5;

            if (CalamityLists.flaskBombList.Contains(Player.ActiveItem().type) && Player.invis)
                throwingVelocity += 0.1f;

            if (CalamityLists.spikyBallList.Contains(Player.ActiveItem().type) && Player.invis)
                throwingCrit += 10;

            if (planarSpeedBoost != 0)
            {
                if (Player.ActiveItem().type != ModContent.ItemType<PrideHuntersPlanarRipper>())
                    planarSpeedBoost = 0;
            }
            if (brimlashBusterBoost)
            {
                if (Player.ActiveItem().type != ModContent.ItemType<BrimlashBuster>())
                    brimlashBusterBoost = false;
            }
            if (evilSmasherBoost > 0)
            {
                if (Player.ActiveItem().type != ModContent.ItemType<EvilSmasher>())
                    evilSmasherBoost = 0;
            }
            if (searedPanCounter > 0)
            {
                if (Player.ActiveItem().type != ModContent.ItemType<SearedPan>())
                {
                    searedPanCounter = 0;
                    searedPanTimer = 0;
                }
                else if (searedPanTimer < SearedPan.ConsecutiveHitOpening)
                    searedPanTimer++;
                else
                    searedPanCounter = 0;
            }
            if (animusBoost > 1f)
            {
                if (Player.ActiveItem().type != ModContent.ItemType<Animus>())
                    animusBoost = 1f;
            }

            // Flight time boosts
            double flightTimeMult = 1D +
                (ZoneAstral ? 0.05 : 0D) +
                (harpyRing ? 0.2 : 0D) +
                (reaverSpeed ? 0.1 : 0D) +
                (aeroStone ? 0.1 : 0D) +
                (angelTreads ? 0.1 : 0D) +
                (blueCandle ? 0.1 : 0D) +
                (soaring ? 0.1 : 0D) +
                (prismaticGreaves ? 0.1 : 0D) +
                (plagueReaper ? 0.05 : 0D) +
                (draconicSurge ? 0.2 : 0D) +
                (Player.empressBrooch ? 0.5 : 0D);

            if (harpyRing)
                Player.moveSpeed += 0.1f;

            if (blueCandle)
                Player.moveSpeed += 0.1f;

            // If the player has the Draconic Elixir cooldown, prevent Draconic Surge from being set as true by any means.
            // This can be caused by other mod interference, e.g. by Luiafk.
            if (Player.HasCooldown(Cooldowns.DraconicElixir.ID))
            {
                draconicSurge = false;
                if (Player.FindBuffIndex(ModContent.BuffType<DraconicSurgeBuff>()) > -1)
                    Player.ClearBuff(ModContent.BuffType<DraconicSurgeBuff>());
            }

            if (community)
            {
                float floatTypeBoost = 0.05f +
                    (NPC.downedSlimeKing ? 0.01f : 0f) +
                    (NPC.downedBoss1 ? 0.01f : 0f) +
                    (NPC.downedBoss2 ? 0.01f : 0f) +
                    (NPC.downedQueenBee ? 0.01f : 0f) +
                    (NPC.downedBoss3 ? 0.01f : 0f) + // 0.1
                    (Main.hardMode ? 0.01f : 0f) +
                    (NPC.downedMechBossAny ? 0.01f : 0f) +
                    (NPC.downedPlantBoss ? 0.01f : 0f) +
                    (NPC.downedGolemBoss ? 0.01f : 0f) +
                    (NPC.downedFishron ? 0.01f : 0f) + // 0.15
                    (NPC.downedAncientCultist ? 0.01f : 0f) +
                    (NPC.downedMoonlord ? 0.01f : 0f) +
                    (DownedBossSystem.downedProvidence ? 0.01f : 0f) +
                    (DownedBossSystem.downedDoG ? 0.01f : 0f) +
                    (DownedBossSystem.downedYharon ? 0.01f : 0f); // 0.2
                int integerTypeBoost = (int)(floatTypeBoost * 50f);
                int critBoost = integerTypeBoost / 2;
                float damageBoost = floatTypeBoost * 0.5f;
                Player.endurance += floatTypeBoost * 0.25f;
                Player.statDefense += integerTypeBoost;
                Player.GetDamage<GenericDamageClass>() += damageBoost;
                AllCritBoost(critBoost);
                Player.GetKnockback<SummonDamageClass>().Base += floatTypeBoost;
                Player.moveSpeed += floatTypeBoost * 0.5f;
                flightTimeMult += floatTypeBoost;
            }
            // Shattered Community gives the same wing time boost as normal Community
            if (shatteredCommunity)
                flightTimeMult += 0.2f;

            if (profanedCrystalBuffs && gOffense && gDefense)
            {
                bool offenseBuffs = (Main.dayTime && !Player.wet) || Player.lavaWet;
                if (offenseBuffs)
                    flightTimeMult += 0.1;
            }

            // Increase wing time
            if (Player.wingTimeMax > 0)
                Player.wingTimeMax = (int)(Player.wingTimeMax * flightTimeMult);

            if (vHex)
            {
                Player.blind = true;
                Player.statDefense -= 10;
                Player.moveSpeed -= 0.1f;

                if (Player.wingTimeMax < 0)
                    Player.wingTimeMax = 0;

                Player.wingTimeMax = (int)(Player.wingTimeMax * 0.75);
            }

            if (eGravity)
            {
                if (Player.wingTimeMax < 0)
                    Player.wingTimeMax = 0;

                if (Player.wingTimeMax > 400)
                    Player.wingTimeMax = 400;

                Player.wingTimeMax = (int)(Player.wingTimeMax * 0.66);
            }

            if (eGrav)
            {
                if (Player.wingTimeMax < 0)
                    Player.wingTimeMax = 0;

                if (Player.wingTimeMax > 400)
                    Player.wingTimeMax = 400;

                Player.wingTimeMax = (int)(Player.wingTimeMax * 0.75);
            }

            if (bounding)
            {
                Player.jumpSpeedBoost += 0.25f;
                Player.jumpHeight += 10;
                Player.extraFall += 25;
            }

            if (mushy)
                Player.statDefense += 5;

            if (omniscience)
            {
                Player.detectCreature = true;
                Player.dangerSense = true;
                Player.findTreasure = true;
            }

            if (aWeapon)
                Player.moveSpeed += 0.05f;

            if (molten)
                Player.resistCold = true;

            if (shellBoost)
                Player.moveSpeed += 0.4f;

            if (tarraSet)
            {
                if (!tarraMelee)
                    Player.calmed = true;
                Player.lifeMagnet = true;
            }

            if (cadence)
            {
                if (Player.FindBuffIndex(BuffID.Regeneration) > -1)
                    Player.ClearBuff(BuffID.Regeneration);
                if (Player.FindBuffIndex(BuffID.Lifeforce) > -1)
                    Player.ClearBuff(BuffID.Lifeforce);
                Player.lifeMagnet = true;
            }

            if (Player.wellFed)
                Player.moveSpeed -= 0.1f;

            if (Player.poisoned)
                Player.moveSpeed -= 0.1f;

            if (Player.venom)
                Player.moveSpeed -= 0.15f;

            if (wDeath)
            {
                Player.GetDamage<GenericDamageClass>() -= 0.2f;
                Player.moveSpeed -= 0.1f;
            }

            if (dragonFire)
                Player.moveSpeed -= 0.15f;

            if (hInferno)
                Player.moveSpeed -= 0.25f;

            if (gsInferno)
                Player.moveSpeed -= 0.15f;

            if (astralInfection)
            {
                Player.GetDamage<GenericDamageClass>() -= 0.1f;
                Player.moveSpeed -= 0.15f;
            }

            if (pFlames)
            {
                Player.blind = !reducedPlagueDmg;
                Player.GetDamage<GenericDamageClass>() -= 0.1f;
                Player.moveSpeed -= 0.15f;
            }

            if (bBlood)
            {
                Player.blind = true;
                Player.statDefense -= 3;
                Player.moveSpeed += 0.1f;
                Player.GetDamage(DamageClass.Melee) += 0.05f;
                Player.GetDamage(DamageClass.Ranged) -= 0.1f;
                Player.GetDamage(DamageClass.Magic) -= 0.1f;
            }

            if (aCrunch && !laudanum)
            {
                Player.statDefense -= ArmorCrunch.DefenseReduction;
                Player.endurance *= 0.33f;
            }

            if (wCleave && !laudanum)
            {
                Player.statDefense -= WarCleave.DefenseReduction;
                Player.endurance *= 0.75f;
            }

            if (wither)
            {
                Player.statDefense -= WitherDebuff.DefenseReduction;
            }

            if (gState)
            {
                Player.velocity.X *= 0.5f;
                Player.velocity.Y += 0.05f;
                if (Player.velocity.Y > 15f)
                    Player.velocity.Y = 15f;
            }

            if (eFreeze)
            {
                Player.velocity.X *= 0.5f;
                Player.velocity.Y += 0.1f;
                if (Player.velocity.Y > 15f)
                    Player.velocity.Y = 15f;
            }

            if (eFreeze || eutrophication)
                Player.velocity = Vector2.Zero;

            if (vaporfied || teslaFreeze)
                Player.velocity *= 0.98f;

            if (molluskSet)
                Player.velocity.X *= 0.985f;

            if ((warped || caribbeanRum) && !Player.slowFall && !Player.mount.Active)
            {
                Player.velocity.Y *= 1.01f;
                Player.moveSpeed -= 0.1f;
            }

            if (corrEffigy)
            {
                Player.moveSpeed += 0.1f;
                AllCritBoost(10);
            }

            if (crimEffigy)
            {
                Player.GetDamage<GenericDamageClass>() += 0.15f;
                Player.statDefense += 10;
            }

            if (warbannerOfTheSun)
                Player.GetDamage(DamageClass.Melee) += warBannerBonus;

            // The player's true max life value with Calamity adjustments
            actualMaxLife = Player.statLifeMax2;

            if (thirdSageH && !Player.dead && healToFull)
            {
                thirdSageH = false;
                Player.statLife = actualMaxLife;
            }

            if (manaOverloader)
                Player.GetDamage(DamageClass.Magic) += 0.06f;

            if (rBrain)
            {
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.75))
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                    Player.moveSpeed -= 0.05f;
            }

            if (bloodyWormTooth)
            {
                if (Player.statLife < (int)(Player.statLifeMax2 * 0.5))
                {
                    Player.GetDamage(DamageClass.Melee) += 0.1f;
                    Player.endurance += 0.1f;
                }
                else
                {
                    Player.GetDamage(DamageClass.Melee) += 0.05f;
                    Player.endurance += 0.05f;
                }
            }

            if (dAmulet)
                Player.pStone = true;

            if (fBulwark)
            {
                Player.noKnockback = true;
                if (Player.statLife > (int)(Player.statLifeMax2 * 0.25))
                {
                    Player.hasPaladinShield = true;
                    if (Player.whoAmI != Main.myPlayer && Player.miscCounter % 10 == 0)
                    {
                        if (Main.LocalPlayer.team == Player.team && Player.team != 0)
                        {
                            Vector2 otherPlayerPos = Player.position - Main.LocalPlayer.position;

                            if (otherPlayerPos.Length() < 800f)
                                Main.LocalPlayer.AddBuff(BuffID.PaladinsShield, 20, true);
                        }
                    }
                }

                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                    Player.AddBuff(BuffID.IceBarrier, 5, true);
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.15))
                    Player.endurance += 0.05f;
            }

            if (frostFlare)
            {
                Player.resistCold = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frozen] = true;

                if (Player.statLife > (int)(Player.statLifeMax2 * 0.75))
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
                if (Player.statLife < (int)(Player.statLifeMax2 * 0.25))
                    Player.statDefense += 20;
            }

            if (vexation)
            {
                if (Player.statLife < (int)(Player.statLifeMax2 * 0.5))
                    Player.GetDamage<GenericDamageClass>() += 0.2f;
            }

            if (ataxiaBlaze)
            {
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                    Player.AddBuff(BuffID.Inferno, 2);
            }

            if (bloodflareThrowing)
            {
                if (Player.statLife > (int)(Player.statLifeMax2 * 0.8))
                {
                    throwingCrit += 5;
                    Player.statDefense += 30;
                }
                else
                    throwingDamage += 0.1f;
            }

            if (bloodflareSummon)
            {
                if (Player.statLife >= (int)(Player.statLifeMax2 * 0.9))
                    Player.GetDamage(DamageClass.Summon) += 0.1f;
                else if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                    Player.statDefense += 20;

                if (bloodflareSummonTimer > 0)
                    bloodflareSummonTimer--;

                if (Player.whoAmI == Main.myPlayer && bloodflareSummonTimer <= 0)
                {
                    bloodflareSummonTimer = 900;
                    var source = new ProjectileSource_BloodflareSummonSet(Player);
                    for (int I = 0; I < 3; I++)
                    {
                        float ai1 = I * 120;
                        int projectile = Projectile.NewProjectile(source, Player.Center.X + (float)(Math.Sin(I * 120) * 550), Player.Center.Y + (float)(Math.Cos(I * 120) * 550), 0f, 0f,
                            ModContent.ProjectileType<GhostlyMine>(), (int)(3750 * Player.MinionDamage()), 1f, Player.whoAmI, ai1, 0f);
                        if (projectile.WithinBounds(Main.maxProjectiles))
                        {
                            Main.projectile[projectile].originalDamage = 3750;
                            Main.projectile[projectile].Calamity().forceTypeless = true;
                        }
                    }
                }
            }

            if (yInsignia)
            {
                Player.GetDamage(DamageClass.Melee) += 0.1f;
                Player.lavaMax += 240;
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
            }

            if (deepDiver && Player.IsUnderwater())
            {
                Player.GetDamage<GenericDamageClass>() += 0.15f;
                Player.statDefense += 15;
                Player.moveSpeed += 0.15f;
            }

            if (abyssalDivingSuit && !Player.IsUnderwater())
            {
                float moveSpeedLoss = (3 - abyssalDivingSuitPlateHits) * 0.2f;
                Player.moveSpeed -= moveSpeedLoss;
            }

            if (ursaSergeant)
                Player.moveSpeed -= 0.15f;

            if (elysianGuard)
                Player.moveSpeed -= 0.5f;

            if (coreOfTheBloodGod)
            {
                Player.endurance += 0.08f;
                Player.GetDamage<GenericDamageClass>() += 0.08f;
            }

            if (godSlayerThrowing)
            {
                if (Player.statLife >= Player.statLifeMax2)
                {
                    throwingCrit += 10;
                    throwingDamage += 0.1f;
                    throwingVelocity += 0.1f;
                }
            }

            #region Damage Auras
            // Tarragon Summon set bonus life aura
            if (tarraSummon)
            {
                const int FramesPerHit = 80;

                // Constantly increment the timer every frame.
                tarraLifeAuraTimer = (tarraLifeAuraTimer + 1) % FramesPerHit;

                // If the timer rolls over, it's time to deal damage. Only run this code for the client which is wearing the armor.
                if (tarraLifeAuraTimer == 0 && Player.whoAmI == Main.myPlayer)
                {
                    const int BaseDamage = 120;
                    int damage = (int)(BaseDamage * Player.MinionDamage());
                    var source = new ProjectileSource_TarragonSummonAura(Player);
                    float range = 300f;

                    for (int i = 0; i < Main.maxNPCs; ++i)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
                            continue;

                        if (Vector2.Distance(Player.Center, npc.Center) <= range)
                            Projectile.NewProjectileDirect(source, npc.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), damage, 0f, Player.whoAmI, i);
                    }
                }
            }

            // Navy Fishing Rod's electric aura when in-use
            if (Player.ActiveItem().type == ModContent.ItemType<NavyFishingRod>() && Player.ownedProjectileCounts[ModContent.ProjectileType<NavyBobber>()] != 0)
            {
                const int FramesPerHit = 120;

                // Constantly increment the timer every frame.
                navyRodAuraTimer = (navyRodAuraTimer + 1) % FramesPerHit;

                // If the timer rolls over, it's time to deal damage. Only run this code for the client which is holding the fishing rod,
                if (navyRodAuraTimer == 0 && Player.whoAmI == Main.myPlayer)
                {
                    const int BaseDamage = 10;
                    int damage = (int)(BaseDamage * Player.AverageDamage());
                    var source = Player.GetSource_ItemUse(Player.ActiveItem());
                    float range = 200f;

                    for (int i = 0; i < Main.maxNPCs; ++i)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
                            continue;

                        if (Vector2.Distance(Player.Center, npc.Center) <= range)
                            Projectile.NewProjectileDirect(source, npc.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), damage, 0f, Player.whoAmI, i);

                        // Occasionally spawn cute sparks so it looks like an electrical aura
                        if (Main.rand.NextBool(10))
                        {
                            Vector2 velocity = CalamityUtils.RandomVelocity(50f, 30f, 60f);
                            int spark = Projectile.NewProjectile(source, npc.Center, velocity, ModContent.ProjectileType<EutrophicSpark>(), damage / 2, 0f, Player.whoAmI);
                            if (spark.WithinBounds(Main.maxProjectiles))
                            {
                                Main.projectile[spark].Calamity().forceTypeless = true;
                                Main.projectile[spark].localNPCHitCooldown = -2;
                                Main.projectile[spark].penetrate = 5;
                            }
                        }
                    }
                }
            }

            // Inferno potion boost
            if (ataxiaBlaze && Player.inferno)
            {
                const int FramesPerHit = 30;

                // Constantly increment the timer every frame.
                brimLoreInfernoTimer = (brimLoreInfernoTimer + 1) % FramesPerHit;

                // Only run this code for the client which is wearing the armor.
                // Brimstone flames is applied every single frame, but direct damage is only dealt twice per second.
                if (Player.whoAmI == Main.myPlayer)
                {
                    const int BaseDamage = 50;
                    int damage = (int)(BaseDamage * Player.AverageDamage());
                    var source = new ProjectileSource_InfernoPotionBoost(Player);
                    float range = 300f;

                    for (int i = 0; i < Main.maxNPCs; ++i)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
                            continue;

                        if (Vector2.Distance(Player.Center, npc.Center) <= range)
                        {
                            npc.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
                            if (brimLoreInfernoTimer == 0)
                                Projectile.NewProjectileDirect(source, npc.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), damage, 0f, Player.whoAmI, i);
                        }
                    }
                }
            }
            #endregion

            if (royalGel)
            {
                Player.npcTypeNoAggro[ModContent.NPCType<AeroSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<BloomSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<CharredSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<CrimulanBlightSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<CryoSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<EbonianBlightSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<IrradiatedSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<PerennialSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<PlaguedJungleSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<AstralSlime>()] = true;
                Player.npcTypeNoAggro[ModContent.NPCType<GammaSlime>()] = true;
            }

            if (oldDukeScales)
            {
                Player.buffImmune[ModContent.BuffType<SulphuricPoisoning>()] = true;
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.Venom] = true;
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.75))
                {
                    Player.GetDamage<GenericDamageClass>() += 0.06f;
                    AllCritBoost(3);
                }
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.5))
                {
                    Player.GetDamage<GenericDamageClass>() += 0.06f;
                    AllCritBoost(3);
                }
                if (Player.statLife <= (int)(Player.statLifeMax2 * 0.25))
                {
                    Player.GetDamage<GenericDamageClass>() += 0.06f;
                    AllCritBoost(3);
                }
                if (Player.lifeRegen < 0)
                {
                    Player.GetDamage<GenericDamageClass>() += 0.1f;
                    AllCritBoost(5);
                }
            }

            if (dArtifact)
                Player.GetDamage<GenericDamageClass>() += 0.25f;

            if (trippy)
                Player.GetDamage<GenericDamageClass>() += 0.5f;

            if (eArtifact)
            {
                Player.manaCost *= 0.85f;
                throwingDamage += 0.15f;
                Player.maxMinions += 2;
            }

            if (gArtifact && Player.FindBuffIndex(ModContent.BuffType<YharonKindleBuff>()) != -1)
                Player.maxMinions += Player.ownedProjectileCounts[ModContent.ProjectileType<SonOfYharon>()];

            if (pArtifact)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<Items.Accessories.ProfanedSoulArtifact>()));
                    if (Player.FindBuffIndex(ModContent.BuffType<ProfanedBabs>()) == -1 && !profanedCrystalBuffs)
                        Player.AddBuff(ModContent.BuffType<ProfanedBabs>(), 3600, true);

                    bool crystal = profanedCrystal && !profanedCrystalForce;
                    bool summonSet = tarraSummon || bloodflareSummon || silvaSummon || dsSetBonus || omegaBlueSet || fearmongerSet;
                    int guardianAmt = 1;

                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianHealer>()] < guardianAmt)
                        Projectile.NewProjectile(source, Player.Center.X, Player.Center.Y, 0f, -6f, ModContent.ProjectileType<MiniGuardianHealer>(), 0, 0f, Main.myPlayer, 0f, 0f);

                    if (crystal || minionSlotStat >= 10)
                    {
                        gDefense = true;

                        if (Player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianDefense>()] < guardianAmt)
                            Projectile.NewProjectile(source, Player.Center.X, Player.Center.Y, 0f, -3f, ModContent.ProjectileType<MiniGuardianDefense>(), 1, 1f, Main.myPlayer, 0f, 0f);
                    }

                    if (crystal || summonSet)
                    {
                        gOffense = true;

                        if (Player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianAttack>()] < guardianAmt)
                            Projectile.NewProjectile(source, Player.Center.X, Player.Center.Y, 0f, -1f, ModContent.ProjectileType<MiniGuardianAttack>(), 1, 1f, Main.myPlayer, 0f, 0f);
                    }
                }
            }

            if (profanedCrystalBuffs && gOffense && gDefense)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    Player.scope = false; //this is so it doesn't mess with the balance of ranged transform attacks over the others
                    Player.lavaImmune = true;
                    Player.lavaMax += 420;
                    Player.lavaRose = true;
                    Player.fireWalk = true;
                    Player.buffImmune[ModContent.BuffType<HolyFlames>()] = Main.dayTime;
                    Player.buffImmune[ModContent.BuffType<Nightwither>()] = !Main.dayTime;
                    Player.buffImmune[BuffID.OnFire] = true;
                    Player.buffImmune[BuffID.Burning] = true;
                    Player.buffImmune[BuffID.Daybreak] = true;
                    bool offenseBuffs = (Main.dayTime && !Player.wet) || Player.lavaWet;
                    if (offenseBuffs)
                    {
                        Player.GetDamage(DamageClass.Summon) += 0.15f;
                        Player.GetKnockback<SummonDamageClass>().Base += 0.15f;
                        Player.moveSpeed += 0.1f;
                        Player.statDefense -= 15;
                        Player.ignoreWater = true;
                    }
                    else
                    {
                        Player.moveSpeed -= 0.1f;
                        Player.endurance += 0.05f;
                        Player.statDefense += 15;
                        Player.lifeRegen += 5;
                    }
                    bool enrage = Player.statLife <= (int)(Player.statLifeMax2 * 0.5);
                    if (!ZoneAbyss) //No abyss memes.
                        Lighting.AddLight(Player.Center, enrage ? 1.2f : offenseBuffs ? 1f : 0.2f, enrage ? 0.21f : offenseBuffs ? 0.2f : 0.01f, 0);
                    if (enrage)
                    {
                        bool special = Player.name == "Amber" || Player.name == "Nincity" || Player.name == "IbanPlay" || Player.name == "Chen"; //People who either helped create the item or test it.
                        for (int i = 0; i < 3; i++)
                        {
                            int fire = Dust.NewDust(Player.position, Player.width, Player.height, special ? 231 : (int)CalamityDusts.ProfanedFire, 0f, 0f, 100, special ? Color.DarkRed : default, 1f);
                            Main.dust[fire].scale = special ? 1.169f : 2f;
                            Main.dust[fire].noGravity = true;
                            Main.dust[fire].velocity *= special ? 10f : 6.9f;
                        }
                    }
                }
            }

            if (plaguebringerPistons)
            {
                //Spawn bees while sprinting or dashing
                pistonsCounter++;
                if (pistonsCounter % 12 == 0)
                {
                    if (Player.velocity.Length() >= 5f && Player.whoAmI == Main.myPlayer)
                    {
                        int beeCount = 1;
                        if (Main.rand.NextBool(3))
                            ++beeCount;
                        if (Main.rand.NextBool(3))
                            ++beeCount;
                        if (Player.strongBees && Main.rand.NextBool(3))
                            ++beeCount;
                        int damage = (int)(30 * Player.MinionDamage());
                        var source = new ProjectileSource_PlaguebringerSetBoost(Player);
                        for (int index = 0; index < beeCount; ++index)
                        {
                            int bee = Projectile.NewProjectile(source, Player.Center.X, Player.Center.Y, Main.rand.NextFloat(-35f, 35f) * 0.02f, Main.rand.NextFloat(-35f, 35f) * 0.02f, (Main.rand.NextBool(4) ? ModContent.ProjectileType<PlagueBeeSmall>() : Player.beeType()), damage, Player.beeKB(0f), Player.whoAmI, 0f, 0f);
                            Main.projectile[bee].usesLocalNPCImmunity = true;
                            Main.projectile[bee].localNPCHitCooldown = 10;
                            Main.projectile[bee].penetrate = 2;
                            if (bee.WithinBounds(Main.maxProjectiles))
                                Main.projectile[bee].Calamity().forceTypeless = true;
                        }
                    }
                }
            }

            List<int> summonDeleteList = new List<int>()
            {
                ModContent.ProjectileType<BrimstoneElementalMinion>(),
                ModContent.ProjectileType<WaterElementalMinion>(),
                ModContent.ProjectileType<SandElementalHealer>(),
                ModContent.ProjectileType<SandElementalMinion>(),
                ModContent.ProjectileType<CloudElementalMinion>(),
                ModContent.ProjectileType<FungalClumpMinion>(),
                ModContent.ProjectileType<HowlsHeartHowl>(),
                ModContent.ProjectileType<HowlsHeartCalcifer>(),
                ModContent.ProjectileType<HowlsHeartTurnipHead>(),
                ModContent.ProjectileType<MiniGuardianAttack>(),
                ModContent.ProjectileType<MiniGuardianDefense>(),
                ModContent.ProjectileType<MiniGuardianHealer>()
            };
            int projAmt = 1;
            for (int i = 0; i < summonDeleteList.Count; i++)
            {
                if (Player.ownedProjectileCounts[summonDeleteList[i]] > projAmt)
                {
                    for (int projIndex = 0; projIndex < Main.maxProjectiles; projIndex++)
                    {
                        Projectile proj = Main.projectile[projIndex];
                        if (proj.active && proj.owner == Player.whoAmI)
                        {
                            if (summonDeleteList.Contains(proj.type))
                            {
                                proj.Kill();
                            }
                        }
                    }
                }
            }

            if (blunderBooster)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<BlunderBooster>()));
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<BlunderBoosterAura>()] < 1)
                        Projectile.NewProjectile(source, Player.Center, Vector2.Zero, ModContent.ProjectileType<BlunderBoosterAura>(), (int)(30 * Player.RogueDamage()), 0f, Player.whoAmI, 0f, 0f);
                }
            }
            else if (Player.ownedProjectileCounts[ModContent.ProjectileType<BlunderBoosterAura>()] != 0)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<BlunderBoosterAura>() && Main.projectile[i].owner == Player.whoAmI)
                        {
                            Main.projectile[i].Kill();
                            break;
                        }
                    }
                }
            }

            if (tesla)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    //Reduce the buffTime of Electrified
                    for (int l = 0; l < Player.MaxBuffs; l++)
                    {
                        bool electrified = Player.buffType[l] == BuffID.Electrified;
                        if (Player.buffTime[l] > 2 && electrified)
                        {
                            Player.buffTime[l]--;
                        }
                    }

                    //Summon the aura
                    var source = new ProjectileSource_TeslaPotion(Player);
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<TeslaAura>()] < 1)
                        Projectile.NewProjectile(source, Player.Center, Vector2.Zero, ModContent.ProjectileType<TeslaAura>(), (int)(10 * Player.AverageDamage()), 0f, Player.whoAmI, 0f, 0f);
                }
            }
            else if (Player.ownedProjectileCounts[ModContent.ProjectileType<TeslaAura>()] != 0)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    int auraType = ModContent.ProjectileType<TeslaAura>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].type != auraType || !Main.projectile[i].active || Main.projectile[i].owner != Player.whoAmI)
                            continue;

                        Main.projectile[i].Kill();
                        break;
                    }
                }
            }

            if (CryoStone)
            {
                var source = Player.GetSource_Accessory(FindAccessory(ModContent.ItemType<CryoStone>()));
                if (Player.whoAmI == Main.myPlayer && Player.ownedProjectileCounts[ModContent.ProjectileType<CryonicShield>()] == 0)
                    Projectile.NewProjectile(source, Player.Center, Vector2.Zero, ModContent.ProjectileType<CryonicShield>(), (int)(Player.AverageDamage() * 70), 0f, Player.whoAmI);
            }
            else if (Player.whoAmI == Main.myPlayer)
            {
                int shieldType = ModContent.ProjectileType<CryonicShield>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type != shieldType || !Main.projectile[i].active || Main.projectile[i].owner != Player.whoAmI)
                        continue;

                    Main.projectile[i].Kill();
                    break;
                }
            }

            if (prismaticLasers > 1800 && Player.whoAmI == Main.myPlayer)
            {
                float shootSpeed = 18f;
                int dmg = (int)(30 * Player.MagicDamage());
                Vector2 startPos = Player.RotatedRelativePoint(Player.MountedCenter, true);
                Vector2 velocity = Main.MouseWorld - startPos;
                if (Player.gravDir == -1f)
                {
                    velocity.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - startPos.Y;
                }
                float travelDist = velocity.Length();
                if ((float.IsNaN(velocity.X) && float.IsNaN(velocity.Y)) || (velocity.X == 0f && velocity.Y == 0f))
                {
                    velocity.X = Player.direction;
                    velocity.Y = 0f;
                    travelDist = shootSpeed;
                }
                else
                {
                    travelDist = shootSpeed / travelDist;
                }

                var source = new ProjectileSource_PrismaticArmorLasers(Player);
                int laserAmt = Main.rand.Next(2);
                for (int index = 0; index < laserAmt; index++)
                {
                    startPos = new Vector2(Player.Center.X + (Main.rand.Next(201) * -(float)Player.direction) + (Main.mouseX + Main.screenPosition.X - Player.position.X), Player.MountedCenter.Y - 600f);
                    startPos.X = (startPos.X + Player.Center.X) / 2f + Main.rand.Next(-200, 201);
                    startPos.Y -= 100 * index;
                    velocity.X = Main.mouseX + Main.screenPosition.X - startPos.X;
                    velocity.Y = Main.mouseY + Main.screenPosition.Y - startPos.Y;
                    if (velocity.Y < 0f)
                    {
                        velocity.Y *= -1f;
                    }
                    if (velocity.Y < 20f)
                    {
                        velocity.Y = 20f;
                    }
                    travelDist = velocity.Length();
                    travelDist = shootSpeed / travelDist;
                    velocity.X *= travelDist;
                    velocity.Y *= travelDist;
                    velocity.X += Main.rand.Next(-50, 51) * 0.02f;
                    velocity.Y += Main.rand.Next(-50, 51) * 0.02f;
                    int laser = Projectile.NewProjectile(source, startPos, velocity, ModContent.ProjectileType<DeathhailBeam>(), dmg, 4f, Player.whoAmI, 0f, 0f);
                    Main.projectile[laser].localNPCHitCooldown = 5;
                    if (laser.WithinBounds(Main.maxProjectiles))
                        Main.projectile[laser].Calamity().forceTypeless = true;
                }
                SoundEngine.PlaySound(SoundID.Item12, Player.Center);
            }
            if (prismaticLasers == 1800)
            {
                // At the exact moment the lasers stop, set the cooldown to appear
                Player.AddCooldown(PrismaticLaser.ID, 1800);
            }
            if (prismaticLasers == 1)
            {
                //Spawn some dust since you can use it again
                int dustAmt = 36;
                for (int dustIndex = 0; dustIndex < dustAmt; dustIndex++)
                {
                    Color color = Utils.SelectRandom(Main.rand, new Color[]
                    {
                        new Color(255, 0, 0, 50), //Red
                        new Color(255, 128, 0, 50), //Orange
                        new Color(255, 255, 0, 50), //Yellow
                        new Color(128, 255, 0, 50), //Lime
                        new Color(0, 255, 0, 50), //Green
                        new Color(0, 255, 128, 50), //Turquoise
                        new Color(0, 255, 255, 50), //Cyan
                        new Color(0, 128, 255, 50), //Light Blue
                        new Color(0, 0, 255, 50), //Blue
                        new Color(128, 0, 255, 50), //Purple
                        new Color(255, 0, 255, 50), //Fuschia
                        new Color(255, 0, 128, 50) //Hot Pink
                    });
                    Vector2 source = Vector2.Normalize(Player.velocity) * new Vector2(Player.width / 2f, Player.height) * 0.75f;
                    source = source.RotatedBy((dustIndex - (dustAmt / 2 - 1)) * MathHelper.TwoPi / dustAmt, default) + Player.Center;
                    Vector2 dustVel = source - Player.Center;
                    int dusty = Dust.NewDust(source + dustVel, 0, 0, 267, dustVel.X * 1f, dustVel.Y * 1f, 100, color, 1f);
                    Main.dust[dusty].noGravity = true;
                    Main.dust[dusty].noLight = true;
                    Main.dust[dusty].velocity = dustVel;
                }
            }

            if (angelicAlliance && Main.myPlayer == Player.whoAmI)
            {
                for (int l = 0; l < Player.MaxBuffs; l++)
                {
                    int hasBuff = Player.buffType[l];
                    if (hasBuff == ModContent.BuffType<Buffs.StatBuffs.DivineBless>())
                    {
                        angelicActivate = Player.buffTime[l];
                    }
                }

                if (Player.FindBuffIndex(ModContent.BuffType<Buffs.StatBuffs.DivineBless>()) == -1)
                    angelicActivate = -1;

                if (angelicActivate == 1)
                    Player.AddCooldown(Cooldowns.DivineBless.ID, CalamityUtils.SecondsToFrames(60));
            }

            if (theBee)
            {
                if (Player.statLife >= Player.statLifeMax2)
                {
                    float beeBoost = Player.endurance / 2f;
                    Player.GetDamage<GenericDamageClass>() += beeBoost;
                }
            }

            if (badgeOfBravery)
            {
                Player.GetDamage(DamageClass.Melee) += 0.05f;
                Player.GetCritChance(DamageClass.Melee) += 5;
            }

            if (CalamityConfig.Instance.Proficiency)
                GetStatBonuses();

            // True melee damage bonuses
            double damageAdd = (dodgeScarf ? 0.1 : 0) +
                    (evasionScarf ? 0.05 : 0) +
                    ((aBulwarkRare && aBulwarkRareMeleeBoostTimer > 0) ? 0.5 : 0) +
                    (fungalSymbiote ? 0.15 : 0) +
                    ((Player.head == ArmorIDs.Head.MoltenHelmet && Player.body == ArmorIDs.Body.MoltenBreastplate && Player.legs == ArmorIDs.Legs.MoltenGreaves) ? 0.2 : 0) +
                    (Player.kbGlove ? 0.1 : 0) +
                    (eGauntlet ? 0.1 : 0) +
                    (yInsignia ? 0.1 : 0) +
                    (warbannerOfTheSun ? warBannerBonus : 0);
            trueMeleeDamage += damageAdd;

            // Amalgam boosts
            if (Main.myPlayer == Player.whoAmI)
            {
                for (int l = 0; l < Player.MaxBuffs; l++)
                {
                    int hasBuff = Player.buffType[l];
                    if ((hasBuff >= BuffID.ObsidianSkin && hasBuff <= BuffID.Gravitation) || hasBuff == BuffID.Tipsy || hasBuff == BuffID.WellFed ||
                        hasBuff == BuffID.Honey || hasBuff == BuffID.WeaponImbueVenom || (hasBuff >= BuffID.WeaponImbueCursedFlames && hasBuff <= BuffID.WeaponImbuePoison) ||
                        (hasBuff >= BuffID.Mining && hasBuff <= BuffID.Wrath) || (hasBuff >= BuffID.Lovestruck && hasBuff <= BuffID.Warmth) || hasBuff == BuffID.SugarRush ||
                        hasBuff == ModContent.BuffType<AbyssalWeapon>() || hasBuff == ModContent.BuffType<AnechoicCoatingBuff>() || hasBuff == ModContent.BuffType<ArmorCrumbling>() ||
                        hasBuff == ModContent.BuffType<ArmorShattering>() || hasBuff == ModContent.BuffType<AstralInjectionBuff>() || hasBuff == ModContent.BuffType<BaguetteBuff>() ||
                        hasBuff == ModContent.BuffType<BloodfinBoost>() || hasBuff == ModContent.BuffType<BoundingBuff>() || hasBuff == ModContent.BuffType<Cadence>() ||
                        hasBuff == ModContent.BuffType<CalciumBuff>() || hasBuff == ModContent.BuffType<CeaselessHunger>() || hasBuff == ModContent.BuffType<DraconicSurgeBuff>() ||
                        hasBuff == ModContent.BuffType<GravityNormalizerBuff>() || hasBuff == ModContent.BuffType<HolyWrathBuff>() || hasBuff == ModContent.BuffType<Omniscience>() ||
                        hasBuff == ModContent.BuffType<PenumbraBuff>() || hasBuff == ModContent.BuffType<PhotosynthesisBuff>() || hasBuff == ModContent.BuffType<ProfanedRageBuff>() ||
                        hasBuff == ModContent.BuffType<Revivify>() || hasBuff == ModContent.BuffType<ShadowBuff>() || hasBuff == ModContent.BuffType<Soaring>() ||
                        hasBuff == ModContent.BuffType<SulphurskinBuff>() || hasBuff == ModContent.BuffType<TeslaBuff>() || hasBuff == ModContent.BuffType<TitanScale>() ||
                        hasBuff == ModContent.BuffType<TriumphBuff>() || hasBuff == ModContent.BuffType<YharimPower>() || hasBuff == ModContent.BuffType<Zen>() ||
                        hasBuff == ModContent.BuffType<Zerg>() || hasBuff == ModContent.BuffType<BloodyMaryBuff>() || hasBuff == ModContent.BuffType<CaribbeanRumBuff>() ||
                        hasBuff == ModContent.BuffType<CinnamonRollBuff>() || hasBuff == ModContent.BuffType<EverclearBuff>() || hasBuff == ModContent.BuffType<EvergreenGinBuff>() ||
                        hasBuff == ModContent.BuffType<FabsolVodkaBuff>() || hasBuff == ModContent.BuffType<FireballBuff>() || hasBuff == ModContent.BuffType<GrapeBeerBuff>() ||
                        hasBuff == ModContent.BuffType<MargaritaBuff>() || hasBuff == ModContent.BuffType<MoonshineBuff>() || hasBuff == ModContent.BuffType<MoscowMuleBuff>() ||
                        hasBuff == ModContent.BuffType<RedWineBuff>() || hasBuff == ModContent.BuffType<RumBuff>() || hasBuff == ModContent.BuffType<ScrewdriverBuff>() ||
                        hasBuff == ModContent.BuffType<StarBeamRyeBuff>() || hasBuff == ModContent.BuffType<TequilaBuff>() || hasBuff == ModContent.BuffType<TequilaSunriseBuff>() ||
                        hasBuff == ModContent.BuffType<Trippy>() || hasBuff == ModContent.BuffType<VodkaBuff>() || hasBuff == ModContent.BuffType<WhiskeyBuff>() ||
                        hasBuff == ModContent.BuffType<WhiteWineBuff>())
                    {
                        if (amalgam)
                        {
                            // Every other frame, increase the buff timer by one frame. Thus, the buff lasts twice as long.
                            if (Player.miscCounter % 2 == 0)
                                Player.buffTime[l] += 1;

                            // Buffs will not go away when you die, to prevent wasting potions.
                            if (!Main.persistentBuff[hasBuff])
                                Main.persistentBuff[hasBuff] = true;
                        }
                        else
                        {
                            // Reset buff persistence if Amalgam is removed.
                            if (Main.persistentBuff[hasBuff])
                                Main.persistentBuff[hasBuff] = false;
                        }
                    }
                }
            }

            // Laudanum boosts
            if (laudanum)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    for (int l = 0; l < Player.MaxBuffs; l++)
                    {
                        int hasBuff = Player.buffType[l];
                        if (hasBuff == ModContent.BuffType<ArmorCrunch>() || hasBuff == ModContent.BuffType<WarCleave>() || hasBuff == BuffID.Obstructed ||
                            hasBuff == BuffID.Ichor || hasBuff == BuffID.Chilled || hasBuff == BuffID.BrokenArmor || hasBuff == BuffID.Weak ||
                            hasBuff == BuffID.Slow || hasBuff == BuffID.Confused || hasBuff == BuffID.Blackout || hasBuff == BuffID.Darkness)
                        {
                            // Every other frame, increase the buff timer by one frame. Thus, the buff lasts twice as long.
                            if (Player.miscCounter % 2 == 0)
                                Player.buffTime[l] += 1;
                        }

                        // See later as Laud cancels out the normal effects
                        if (hasBuff == ModContent.BuffType<ArmorCrunch>())
                        {
                            // +15 defense
                            Player.statDefense += ArmorCrunch.DefenseReduction;
                        }
                        if (hasBuff == ModContent.BuffType<WarCleave>())
                        {
                            // +10% damage reduction
                            Player.endurance += 0.1f;
                        }

                        switch (hasBuff)
                        {
                            case BuffID.Obstructed:
                                Player.headcovered = false;
                                Player.statDefense += 50;
                                Player.GetDamage<GenericDamageClass>() += 0.5f;
                                AllCritBoost(25);
                                break;
                            case BuffID.Ichor:
                                Player.statDefense += 40;
                                break;
                            case BuffID.Chilled:
                                Player.chilled = false;
                                Player.moveSpeed *= 1.3f;
                                break;
                            case BuffID.BrokenArmor:
                                Player.brokenArmor = false;
                                Player.statDefense += (int)(Player.statDefense * 0.25);
                                break;
                            case BuffID.Weak:
                                Player.GetDamage(DamageClass.Melee) += 0.151f;
                                Player.statDefense += 14;
                                Player.moveSpeed += 0.3f;
                                break;
                            case BuffID.Slow:
                                Player.slow = false;
                                Player.moveSpeed *= 1.5f;
                                break;
                            case BuffID.Confused:
                                Player.confused = false;
                                Player.statDefense += 30;
                                Player.GetDamage<GenericDamageClass>() += 0.25f;
                                AllCritBoost(10);
                                break;
                            case BuffID.Blackout:
                                Player.blackout = false;
                                Player.statDefense += 30;
                                Player.GetDamage<GenericDamageClass>() += 0.25f;
                                AllCritBoost(10);
                                break;
                            case BuffID.Darkness:
                                Player.blind = false;
                                Player.statDefense += 15;
                                Player.GetDamage<GenericDamageClass>() += 0.1f;
                                AllCritBoost(5);
                                break;
                        }
                    }
                }
            }

            // Draedon's Heart bonus
            if (draedonsHeart)
            {
                if (Player.StandingStill() && Player.itemAnimation == 0)
                    Player.statDefense += (int)(Player.statDefense * 0.75);
            }

            // Endurance reductions
            EnduranceReductions();

            if (spectralVeilImmunity > 0)
            {
                int numDust = 2;
                for (int i = 0; i < numDust; i++)
                {
                    int dustIndex = Dust.NewDust(Player.position, Player.width, Player.height, 21, 0f, 0f);
                    Dust dust = Main.dust[dustIndex];
                    dust.position.X += Main.rand.Next(-5, 6);
                    dust.position.Y += Main.rand.Next(-5, 6);
                    dust.velocity *= 0.2f;
                    dust.noGravity = true;
                    dust.noLight = true;
                }
            }

            // Gem Tech stats based on gems.
            GemTechState.ProvideGemBoosts();
        }
        #endregion

        #region Defense Effects
        private void DefenseEffects()
        {
            //
            // Defense Damage
            //
            // Current defense damage can be calculated at any time using the accessor property CurrentDefenseDamage.
            // However, it CANNOT be written to. You can only set the total defense damage.
            // CalamityPlayer has a function called DealDefenseDamage to handle everything for you, when dealing defense damage.
            //
            // The player's current recovery through defense damage is tracked through two frame counts:
            // defenseDamageRecoveryFrames = How many more frames the player will still be recovering from defense damage
            // totalDefenseDamageRecoveryFrames = The total timer for defense damage recovery that the player is undergoing
            //
            // Defense damage heals over a fixed time (CalamityPlayer.DefenseDamageRecoveryTime).
            // This is independent of how much defense the player started with, or how much they lost.
            // If hit again while recovering from defense damage, that fixed time is ADDED to the current recovery timer
            // (in addition to the player taking more defense damage, of course).
            if (totalDefenseDamage > 0)
            {
                // Defense damage is capped at your maximum defense, no matter what.
                if (totalDefenseDamage > Player.statDefense)
                    totalDefenseDamage = Player.statDefense;

                // You cannot begin recovering from defense damage until your iframes wear off.
                bool hasIFrames = false;
                for (int i = 0; i < Player.hurtCooldowns.Length; i++)
                    if (Player.hurtCooldowns[i] > 0)
                        hasIFrames = true;

                // Delay before defense damage recovery can start. While this delay is ticking down, defense damage doesn't recover at all.
                if (!hasIFrames && defenseDamageDelayFrames > 0)
                    --defenseDamageDelayFrames;

                // Once the delay is up, defense damage recovery occurs.
                else if (defenseDamageDelayFrames <= 0)
                {
                    // Make one frame's worth of progress towards recovery.
                    --defenseDamageRecoveryFrames;

                    // If completely recovered, reset defense damage to nothing.
                    if (defenseDamageRecoveryFrames <= 0)
                    {
                        totalDefenseDamage = 0;
                        defenseDamageRecoveryFrames = 0;
                        totalDefenseDamageRecoveryFrames = DefenseDamageBaseRecoveryTime;
                        defenseDamageDelayFrames = 0;
                    }
                }

                // Get current amount of defense damage to apply this frame.
                int currentDefenseDamage = CurrentDefenseDamage;

                // Apply DR Damage.
                //
                // DR Damage is applied at exactly the same ratio as defense damage;
                // if you lose half your defense to defense damage, you also lose half your DR.
                // This is applied first because the math would be wrong if the player's defense was already reduced by defense damage.
                if (Player.statDefense > 0 && Player.endurance > 0f)
                {
                    float drDamageRatio = currentDefenseDamage / (float)Player.statDefense;
                    Player.endurance *= 1f - drDamageRatio;
                }

                // Apply defense damage
                Player.statDefense -= currentDefenseDamage;
            }

            // Bloodflare Core's defense reduction
            // This is intentionally after defense damage.
            // This defense still comes back over time if you take off Bloodflare Core while you're missing defense.
            // However, removing the item means you won't get healed as the defense comes back.
            ref int lostDef = ref bloodflareCoreLostDefense;
            if (lostDef > 0)
            {
                // Defense regeneration occurs every four frames while defense is missing
                if (Player.miscCounter % 4 == 0)
                {
                    --lostDef;
                    if (bloodflareCore)
                    {
                        Player.statLife += 1;
                        Player.HealEffect(1, false);

                        // Produce an implosion of blood themed dust so it's obvious an effect is occurring
                        for (int i = 0; i < 3; ++i)
                        {
                            Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(23f, 33f);
                            Vector2 dustPos = Player.Center + offset;
                            Vector2 dustVel = offset * -0.08f;
                            Dust d = Dust.NewDustDirect(dustPos, 0, 0, 90, 0.08f, 0.08f);
                            d.velocity = dustVel;
                            d.noGravity = true;
                        }
                    }
                }

                // Actually apply Bloodflare Core defense reduction
                Player.statDefense -= lostDef;
            }

            // Defense can never be reduced below zero, no matter what
            if (Player.statDefense < 0)
                Player.statDefense = 0;

            // Multiplicative defense reductions.
            // These are done last because they need to be after the defense lower cap at 0.
            if (fabsolVodka)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.1);
            }

            if (vodka)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.05);
            }

            if (grapeBeer)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.03);
            }

            if (rum)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.05);
            }

            if (whiskey)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.05);
            }

            if (everclear)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.3);
            }

            if (bloodyMary)
            {
                if (Main.bloodMoon)
                {
                    if (Player.statDefense > 0)
                        Player.statDefense -= (int)(Player.statDefense * 0.04);
                }
            }

            if (caribbeanRum)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.1);
            }

            if (cinnamonRoll)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.1);
            }

            if (margarita)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.06);
            }

            if (starBeamRye)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.06);
            }

            if (whiteWine)
            {
                if (Player.statDefense > 0)
                    Player.statDefense -= (int)(Player.statDefense * 0.06);
            }
        }
        #endregion

        #region Limits
        private void Limits()
        {
            //not sure where else this should go
            if (forbiddenCirclet)
            {
                float rogueDmg = Player.GetDamage(DamageClass.Throwing).Base + throwingDamage - 1f;
                float minionDmg = Player.GetDamage(DamageClass.Summon).Base;
                if (minionDmg < rogueDmg)
                {
                    Player.GetDamage(DamageClass.Summon) += rogueDmg - Player.GetDamage(DamageClass.Summon).Base;
                }
                if (rogueDmg < minionDmg)
                {
                    throwingDamage = minionDmg - Player.GetDamage(DamageClass.Throwing).Base + 1f;
                }
            }

            // 10% is converted to 9%, 25% is converted to 20%, 50% is converted to 33%, 75% is converted to 43%, 100% is converted to 50%
            if (Player.endurance > 0f)
                Player.endurance = 1f - (1f / (1f + Player.endurance));

            // Do not apply reduced aggro if there are any bosses alive and it's singleplayer
            if (areThereAnyDamnBosses && Main.netMode == NetmodeID.SinglePlayer)
            {
                if (Player.aggro < 0)
                    Player.aggro = 0;
            }
        }
        #endregion

        #region Endurance Reductions
        private void EnduranceReductions()
        {
            if (vHex)
                Player.endurance -= 0.1f;

            if (irradiated)
                    Player.endurance -= 0.1f;

            if (corrEffigy)
                Player.endurance -= 0.05f;
        }
        #endregion

        #region Stat Meter
        private void UpdateStatMeter()
        {
            float allDamageStat = Player.GetDamage<GenericDamageClass>().Base - 1f;
            damageStats[0] = (int)((Player.GetDamage(DamageClass.Melee).Base + allDamageStat - 1f) * 100f);
            damageStats[1] = (int)((Player.GetDamage(DamageClass.Ranged).Base + allDamageStat - 1f) * 100f);
            damageStats[2] = (int)((Player.GetDamage(DamageClass.Magic).Base + allDamageStat - 1f) * 100f);
            damageStats[3] = (int)((Player.GetDamage(DamageClass.Summon).Base + allDamageStat - 1f) * 100f);
            damageStats[4] = (int)((throwingDamage + allDamageStat - 1f) * 100f);
            damageStats[5] = (int)(trueMeleeDamage * 100D);
            critStats[0] = (int)Player.GetCritChance(DamageClass.Melee);
            critStats[1] = (int)Player.GetCritChance(DamageClass.Ranged);
            critStats[2] = (int)Player.GetCritChance(DamageClass.Magic);
            critStats[3] = (int)Player.GetCritChance(DamageClass.Throwing) + throwingCrit;
            ammoReductionRanged = (int)(100f *
                (Player.ammoBox ? 0.8f : 1f) *
                (Player.ammoPotion ? 0.8f : 1f) *
                (Player.ammoCost80 ? 0.8f : 1f) *
                (Player.ammoCost75 ? 0.75f : 1f) *
                rangedAmmoCost);
            ammoReductionRogue = (int)(throwingAmmoCost * 100);
            // Cancel out defense damage for the purposes of the stat meter.
            defenseStat = Player.statDefense + CurrentDefenseDamage;
            DRStat = (int)(Player.endurance * 100f);
            meleeSpeedStat = (int)((1f - Player.GetAttackSpeed(DamageClass.Melee)) * (100f / Player.GetAttackSpeed(DamageClass.Melee)));
            manaCostStat = (int)(Player.manaCost * 100f);
            rogueVelocityStat = (int)((throwingVelocity - 1f) * 100f);

            // Max stealth 1f is actually "100 stealth", so multiply by 100 to get visual stealth number.
            stealthStat = (int)(rogueStealthMax * 100f);
            // Then divide by 3, because it takes 3 seconds to regen full stealth.
            // Divide by 3 again for moving, because it recharges at 1/3 speed (so divide by 9 overall).
            // Then multiply by stealthGen variables, which start at 1f and increase proportionally to your boosts.
            standingRegenStat = (rogueStealthMax * 100f / 3f) * stealthGenStandstill;
            movingRegenStat = (rogueStealthMax * 100f / 9f) * stealthGenMoving * stealthAcceleration;

            minionSlotStat = Player.maxMinions;
            manaRegenStat = Player.manaRegen;
            armorPenetrationStat = (int)Player.GetArmorPenetration(DamageClass.Generic);
            moveSpeedStat = (int)((Player.moveSpeed - 1f) * 100f);
            wingFlightTimeStat = Player.wingTimeMax / 60f;
            float trueJumpSpeedBoost = Player.jumpSpeedBoost +
                (Player.wereWolf ? 0.2f : 0f) +
                (Player.jumpBoost ? 0.75f : 0f);
            jumpSpeedStat = trueJumpSpeedBoost * 20f;
            rageDamageStat = (int)(100D * RageDamageBoost);
            adrenalineDamageStat = (int)(100D * Player.Calamity().GetAdrenalineDamage());
            int extraAdrenalineDR = 0 +
                (adrenalineBoostOne ? 5 : 0) +
                (adrenalineBoostTwo ? 5 : 0) +
                (adrenalineBoostThree ? 5 : 0);
            adrenalineDRStat = 50 + extraAdrenalineDR;
        }
        #endregion

        #region Double Jumps
        private void DoubleJumps()
        {
            if (CalamityUtils.CountHookProj() > 0 || Player.sliding || Player.autoJump && Player.justJumped)
            {
                jumpAgainSulfur = true;
                jumpAgainStatigel = true;
                return;
            }

            bool mountCheck = true;
            if (Player.mount != null && Player.mount.Active)
                mountCheck = Player.mount.BlockExtraJumps;
            bool carpetCheck = true;
            if (Player.carpet)
                carpetCheck = Player.carpetTime <= 0 && Player.canCarpet;
            bool wingCheck = Player.wingTime == Player.wingTimeMax || Player.autoJump;
            Tile tileBelow = CalamityUtils.ParanoidTileRetrieval((int)(Player.Bottom.X / 16f), (int)(Player.Bottom.Y / 16f));

            if (Player.position.Y == Player.oldPosition.Y && wingCheck && mountCheck && carpetCheck && tileBelow.IsTileSolidGround())
            {
                jumpAgainSulfur = true;
                jumpAgainStatigel = true;
            }
        }
        #endregion

        #region Mouse Item Checks
        public void CheckIfMouseItemIsSchematic()
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            bool shouldSync = false;

            // ActiveItem doesn't need to be checked as the other possibility involves
            // the item in question already being in the inventory.
            if (Main.mouseItem != null && !Main.mouseItem.IsAir)
            {
                if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicSunkenSea>() && !RecipeUnlockHandler.HasFoundSunkenSeaSchematic)
                {
                    RecipeUnlockHandler.HasFoundSunkenSeaSchematic = true;
                    shouldSync = true;
                }

                if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicPlanetoid>() && !RecipeUnlockHandler.HasFoundPlanetoidSchematic)
                {
                    RecipeUnlockHandler.HasFoundPlanetoidSchematic = true;
                    shouldSync = true;
                }

                if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicJungle>() && !RecipeUnlockHandler.HasFoundJungleSchematic)
                {
                    RecipeUnlockHandler.HasFoundJungleSchematic = true;
                    shouldSync = true;
                }

                if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicHell>() && !RecipeUnlockHandler.HasFoundHellSchematic)
                {
                    RecipeUnlockHandler.HasFoundHellSchematic = true;
                    shouldSync = true;
                }

                if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicIce>() && !RecipeUnlockHandler.HasFoundIceSchematic)
                {
                    RecipeUnlockHandler.HasFoundIceSchematic = true;
                    shouldSync = true;
                }
            }

            if (shouldSync)
                CalamityNetcode.SyncWorld();
        }
        #endregion

        #region Potion Handling
        private void HandlePotions()
        {
            if (potionTimer > 0)
                potionTimer--;
            if (potionTimer > 0 && Player.potionDelay == 0)
                Player.potionDelay = potionTimer;
            if (potionTimer == 1)
            {
                //Reduced duration than normal
                int duration = 3000;
                if (Player.pStone)
                    duration = (int)(duration * 0.75);
                Player.ClearBuff(BuffID.PotionSickness);
                Player.AddBuff(BuffID.PotionSickness, duration);
            }

            if (PlayerInput.Triggers.JustPressed.QuickBuff)
            {
                for (int i = 0; i < Main.InventorySlotsTotal; ++i)
                {
                    Item item = Player.inventory[i];

                    if (Player.potionDelay > 0 || potionTimer > 0)
                        break;
                    if (item is null || item.stack <= 0)
                        continue;

                    if (item.type == ModContent.ItemType<SunkenStew>())
                        CalamityUtils.ConsumeItemViaQuickBuff(Player, item, SunkenStew.BuffType, SunkenStew.BuffDuration, true);
                    if (item.type == ModContent.ItemType<Margarita>())
                        CalamityUtils.ConsumeItemViaQuickBuff(Player, item, Margarita.BuffType, Margarita.BuffDuration, false);
                    if (item.type == ModContent.ItemType<Bloodfin>())
                        CalamityUtils.ConsumeItemViaQuickBuff(Player, item, Bloodfin.BuffType, Bloodfin.BuffDuration, false);
                }
            }
        }
        #endregion
    }
}
