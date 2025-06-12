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
            // 6 ticks = 0.1 seconds at 60 FPS
            player.GetModPlayer<DodgePlayer>().TeleportDelayBonus += 6;
        }
    }
} 