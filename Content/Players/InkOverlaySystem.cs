using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Renderers;
using Terraria.UI;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.Utilities;
using Terraria.GameContent.UI.Elements;
using ReLogic.Content;

namespace smartDodgeAI.Content.Players
{
    // 墨水覆盖效果系统
    public class InkOverlaySystem : ModSystem
    {
        private static Asset<Texture2D> _inkTexture;

        public override void Load()
        {
            Main.OnResolutionChanged += OnResolutionChanged;
            if (!Main.dedServ)
            {
                _inkTexture = ModContent.Request<Texture2D>("smartDodgeAI/Content/Assets/Textures/UI/InkSplash");
            }
        }

        public override void Unload()
        {
            Main.OnResolutionChanged -= OnResolutionChanged;
            _inkTexture = null;
        }

        private void OnResolutionChanged(Vector2 newSize)
        {
            // 此方法现在为空，但保留以避免在加载/卸载时出现问题
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryLayerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryLayerIndex != -1)
            {
                layers.Insert(inventoryLayerIndex, new LegacyGameInterfaceLayer(
                    "smartDodgeAI: Ink Overlay",
                    DrawInkOverlay,
                    InterfaceScaleType.UI)
                );
            }
        }

        private bool DrawInkOverlay()
        {
            if (Main.LocalPlayer == null || Main.gameMenu)
            {
                return true;
            }

            var modPlayer = Main.LocalPlayer.GetModPlayer<DodgePlayer>();
            if (modPlayer.InkEffectTime > 0)
            {
                // 计算透明度，实现淡出效果
                float inkAlpha = 1f;
                const int fadeOutTime = 60; // 在最后1秒开始淡出
                if (modPlayer.InkEffectTime < fadeOutTime)
                {
                    inkAlpha = modPlayer.InkEffectTime / (float)fadeOutTime;
                }
                
                // 将最终的纹理绘制到屏幕中央
                Main.spriteBatch.Draw(_inkTexture.Value, Main.ScreenSize.ToVector2() / 2f, null, Color.White * inkAlpha, 0f, _inkTexture.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }

            return true;
        }
    }
} 