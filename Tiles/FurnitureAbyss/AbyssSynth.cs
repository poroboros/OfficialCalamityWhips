﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityMod.Tiles.FurnitureAbyss
{
    [LegacyName("AbyssPiano")]
    public class AbyssSynth : ModTile
    {
        public override void SetStaticDefaults()
        {
            this.SetUpPiano(true);
            LocalizedText name = CreateMapEntryName();
            // name.SetDefault("Abyss Synth");
            AddMapEntry(new Color(191, 142, 111), name);
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, 1, 0f, 0f, 1, new Color(100, 130, 150), 1f);
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }
}
