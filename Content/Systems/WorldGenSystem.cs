using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using smartDodgeAI.Content.Items.Accessories;
using Terraria.IO;
using System.Linq;

namespace smartDodgeAI.Content.Systems
{
    public class WorldGenSystem : ModSystem
    {
        public override void PostWorldGen()
        {
            // 寻找离出生点最近的木制宝箱
            int chestIndex = -1;
            double shortestDistance = -1;

            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest chest = Main.chest[i];
                if (chest != null && TileID.Sets.BasicChest[Main.tile[chest.x, chest.y].TileType])
                {
                    // 检查这是否是一个天然的木箱（而不是玩家放置的）
                    Tile chestTile = Main.tile[chest.x, chest.y];
                    if (chestTile.TileType == TileID.Containers && chestTile.TileFrameX == 0 * 36)
                    {
                        double distance = new Vector2(Main.spawnTileX, Main.spawnTileY).Distance(new Vector2(chest.x, chest.y));
                        if (chestIndex == -1 || distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            chestIndex = i;
                        }
                    }
                }
            }

            // 如果找到了一个合适的宝箱
            if (chestIndex != -1)
            {
                Chest targetChest = Main.chest[chestIndex];
                // 在宝箱的第一个空格子中放入物品
                for (int i = 0; i < Chest.maxItems; i++)
                {
                    if (targetChest.item[i] == null || targetChest.item[i].type == ItemID.None)
                    {
                        targetChest.item[i] = new Item();
                        targetChest.item[i].SetDefaults(ModContent.ItemType<BlurredTrinket>());
                        break; // 只放一个
                    }
                }
            }
        }
    }
} 