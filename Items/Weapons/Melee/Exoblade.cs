﻿using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System.Linq;
using CalamityMod.Projectiles.Melee;
using static Terraria.ModLoader.ModContent;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace CalamityMod.Items.Weapons.Melee
{
    [LegacyName("DraedonsExoblade")]
    public class Exoblade : ModItem
    {
        public static readonly SoundStyle SwingSound = new("CalamityMod/Sounds/Item/ExobladeSwing") { MaxInstances = 3, PitchVariance = 0.6f, Volume = 0.8f };
        public static readonly SoundStyle BigSwingSound = new("CalamityMod/Sounds/Item/ExobladeBigSwing") { MaxInstances = 3, PitchVariance = 0.2f };


        public static int BeamNoHomeTime = 24;

        public static float NotTrueMeleeDamagePenalty = 0.46f;

        public static float ExplosionDamageFactor = 1.8f;

        public static float LungeDamageFactor = 1.75f;

        public static int LungeCooldown = 60;

        public static float LungeMaxCorrection = MathHelper.PiOver4 * 0.05f;

        public static float LungeSpeed = 60f;

        public static float ReboundSpeed = 6f;

        public static float PercentageOfAnimationSpentLunging = 0.6f;

        public static int OpportunityForBigSlash = 37;

        public static float BigSlashUpscaleFactor = 1.5f;

        public static int DashTime = 49;

        public static int BaseUseTime = 49;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exoblade");
            Tooltip.SetDefault("Ancient blade of Yharim's weapons and armors expert, Draedon\n" +
                               "Left clicks release multiple energy beams that home in on enemies and slice them on hit\n" +
                               "Right clicks makes you dash in the direction of the cursor with the blade\n" +
                               "Enemy hits from the blade during the dash result in massive damage and a rebound\n" +
                               "Left clicks briefly after a rebound are far stronger and create explosions on enemy hits");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 114;
            Item.damage = 2625;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.useTurn = true;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 9f;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.value = CalamityGlobalItem.Rarity15BuyPrice;
            Item.shoot = ProjectileType<ExobladeProj>();
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 9f;
            Item.Calamity().customRarity = CalamityRarity.Violet;
        }

        public override bool CanShoot(Player player)
        {
            //Lunge can't be used if ANY exoblade is there (even the ones in stasis)
            if (player.altFunctionUse == 2)
                return !Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == ProjectileType<ExobladeProj>());


            return !Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == ProjectileType<ExobladeProj>() &&         
            !(n.ai[0] == 1 && n.ai[1] == 1)); //Ignores exoblades in post bonk stasis.
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().rightClickListener = true;
            player.Calamity().mouseWorldListener = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? CanHitNPC(Player player, NPC target) => false;

        public override bool CanHitPvp(Player player, Player target) => false;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float state = 0;

            //If there are any exoblades in "stasis" after a bonk, the attack should be an empowered slash instead
            if (Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == ProjectileType<ExobladeProj>() && n.ai[0] == 1 && n.ai[1] == 1 && n.timeLeft > LungeCooldown))
            {
                state = 2;

                //Put all the "post bonk" stasised exoblades into regular cooldown for the right click ljunge
                for (int i = 0; i < Main.maxProjectiles; ++i)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.owner != player.whoAmI || p.type != Item.shoot || p.ai[0] != 1 || p.ai[1] != 1)
                        continue;

                    p.timeLeft = LungeCooldown;
                    p.netUpdate = true;
                    p.netSpam = 0;
                }
            }

            if (player.altFunctionUse == 2)
            {
                state = 1;
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, state, 0);

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<Terratomere>().
                AddIngredient<AnarchyBlade>().
                AddIngredient<FlarefrostBlade>().
                AddIngredient<StellarStriker>().
                AddIngredient<MiracleMatter>().
                AddTile(ModContent.TileType<DraedonsForge>()).
                Register();
        }
    }
}
