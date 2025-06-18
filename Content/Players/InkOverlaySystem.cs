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

namespace smartDodgeAI.Content.Players
{
    // 墨水覆盖效果系统
    public class InkOverlaySystem : ModSystem
    {
        private static RenderTarget2D _inkRenderTarget;
        private static UnifiedRandom _inkRandom;
        private static int _cachedInkSeed = -1;
        private static int _cachedScreenWidth = -1;
        private static int _cachedScreenHeight = -1;

        public override void Load()
        {
            Main.OnResolutionChanged += OnResolutionChanged;
        }

        public override void Unload()
        {
            Main.OnResolutionChanged -= OnResolutionChanged;
            _inkRenderTarget?.Dispose();
        }

        private void OnResolutionChanged(Vector2 newSize)
        {
            // 当分辨率变化时，标记缓存的屏幕尺寸为无效，以便下次绘制时重建RenderTarget
            _cachedScreenWidth = -1;
            _cachedScreenHeight = -1;
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
                // 如果需要，（重新）生成墨迹纹理
                GenerateInkSplatTexture(modPlayer);

                // 计算透明度，实现淡出效果
                float inkAlpha = 1f;
                const int fadeOutTime = 60; // 在最后1秒开始淡出
                if (modPlayer.InkEffectTime < fadeOutTime)
                {
                    inkAlpha = modPlayer.InkEffectTime / (float)fadeOutTime;
                }
                
                // 将最终的纹理绘制到屏幕中央
                Main.spriteBatch.Draw(_inkRenderTarget, Main.ScreenSize.ToVector2() / 2f, null, Color.White * inkAlpha, 0f, _inkRenderTarget.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }

            return true;
        }

        private void GenerateInkSplatTexture(DodgePlayer modPlayer)
        {
            // 仅在需要时（如效果首次触发、屏幕尺寸变化）才重新生成纹理
            if (_cachedInkSeed == modPlayer.InkSeed && _cachedScreenWidth == Main.screenWidth && _cachedScreenHeight == Main.screenHeight && _inkRenderTarget != null && !_inkRenderTarget.IsDisposed)
            {
                return;
            }

            _cachedInkSeed = modPlayer.InkSeed;
            _cachedScreenWidth = Main.screenWidth;
            _cachedScreenHeight = Main.screenHeight;
            
            // 使用种子确保随机性是可复现的，这样墨迹在效果持续时间内不会闪烁
            _inkRandom = new UnifiedRandom(_cachedInkSeed);

            var graphicsDevice = Main.graphics.GraphicsDevice;
            
            // 基于屏幕高度计算半径
            float targetRadius = Main.screenHeight * 0.42f; // 约占屏幕75%的面积 (sqrt(0.75) / 2) * 2
            int textureSize = (int)(targetRadius * 2);

            // 创建或重建 RenderTarget
            _inkRenderTarget?.Dispose();
            _inkRenderTarget = new RenderTarget2D(graphicsDevice, textureSize, textureSize, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);

            // 切换到 RenderTarget 进行绘制
            graphicsDevice.SetRenderTarget(_inkRenderTarget);
            graphicsDevice.Clear(Color.Transparent);

            var spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            Texture2D inkTexture = TextureAssets.MagicPixel.Value;
            Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);

            // 绘制一个大的、不规则的中心墨迹团
            int coreSplats = 150;
            for (int i = 0; i < coreSplats; i++)
            {
                float angle = _inkRandom.NextFloat() * MathHelper.TwoPi;
                float distance = (float)Math.Sqrt(_inkRandom.NextFloat()) * targetRadius;
                Vector2 position = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
                float size = targetRadius * _inkRandom.NextFloat(0.1f, 0.45f);
                float rotation = _inkRandom.NextFloat() * MathHelper.TwoPi;
                spriteBatch.Draw(inkTexture, position, null, Color.Black, rotation, inkTexture.Size() / 2, size, SpriteEffects.None, 0f);
            }

            // 绘制一些向外辐射的小墨滴
            int outerSplats = 60;
            for (int i = 0; i < outerSplats; i++)
            {
                float angle = _inkRandom.NextFloat() * MathHelper.TwoPi;
                float distance = targetRadius * (0.9f + _inkRandom.NextFloat() * 0.2f); // 在边缘附近
                Vector2 position = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
                float size = targetRadius * _inkRandom.NextFloat(0.05f, 0.15f);
                spriteBatch.Draw(inkTexture, position, null, Color.Black, 0f, inkTexture.Size() / 2, size, SpriteEffects.None, 0f);
            }

            spriteBatch.End();

            // 恢复默认的 RenderTarget
            graphicsDevice.SetRenderTarget(null);
        }
    }
} 