using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using smartDodgeAI.Content.Players;

namespace smartDodgeAI.Content.Items.Accessories
{
    public class PhantomLocus : ModItem
    {
        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Lime;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<DodgePlayer>();
            modPlayer.ForceInkSplashTo1 = true;
            player.statDefense += 1;
            player.statLifeMax2 += 20;
            player.GetCritChance(DamageClass.Generic) += 2;
            player.moveSpeed += 0.05f;
            player.statManaMax2 += 20;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<BlurredTrinket>())
                .AddIngredient(ModContent.ItemType<GelEcho>())
                .AddIngredient(ModContent.ItemType<RetinalRipple>())
                .AddIngredient(ModContent.ItemType<ShadowRemnant>())
                .AddIngredient(ModContent.ItemType<HiveMirage>())
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Set the color of the item name and tooltips to a rainbow gradient
            foreach (var line in tooltips)
            {
                if (line.Mod == "Terraria" && (line.Name.StartsWith("Tooltip") || line.Name == "ItemName"))
                {
                    line.OverrideColor = Main.DiscoColor;
                }
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = TextureAssets.Item[Item.type].Value;
            
            float rotation = Main.GameUpdateCount * 0.05f;

            // The new origin is the center of the texture
            Vector2 newOrigin = texture.Size() / 2f;
            
            // To rotate around the center while keeping the top-left corner at the same spot,
            // we need to adjust the drawing position using a transformation.
            // newPosition = oldPosition - oldOrigin * scale + newOrigin * scale
            Vector2 newPosition = position - origin * scale + newOrigin * scale;

            spriteBatch.Draw(texture, newPosition, null, drawColor, rotation, newOrigin, scale, SpriteEffects.None, 0f);

            // Return false to prevent the default drawing
            return false;
        }
    }
} 