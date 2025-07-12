using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using smartDodgeAI.Content.Players;

namespace smartDodgeAI.Content.Items.Accessories
{
    public class HiveMirage : ModItem
    {
        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<DodgePlayer>().InkSplashReduction += 2;
            player.statManaMax2 += 20;
        }
    }
} 