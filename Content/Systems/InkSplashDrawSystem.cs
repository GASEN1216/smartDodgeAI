using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using smartDodgeAI.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace smartDodgeAI.Content.Systems
{
    public class InkSplashDrawSystem : ModSystem
    {
        private static Asset<Texture2D> _inkSplashTexture;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _inkSplashTexture = ModContent.Request<Texture2D>("smartDodgeAI/Content/Assets/Textures/UI/InkSplash");
            }
        }

        public override void Unload()
        {
            _inkSplashTexture = null;
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (Main.LocalPlayer.TryGetModPlayer<DodgePlayer>(out var dodgePlayer) && dodgePlayer.HasInkEffect && _inkSplashTexture != null && _inkSplashTexture.IsLoaded)
            {
                // 注意：移除了 spriteBatch.Begin() 调用，因为在 PostDrawInterface 中传入的 spriteBatch 已处于绘制状态
                // 再次调用 Begin() 而不先调用 End() 会导致 InvalidOperationException

                Texture2D texture = _inkSplashTexture.Value;
                float alpha = 0f;
                const int totalDuration = 120;
                const int fadeInDuration = 10;
                const int fadeOutStartTime = 60;

                // 计算alpha值
                if (dodgePlayer.InkEffectTime > totalDuration - fadeInDuration)
                {
                    // 淡入
                    alpha = 1f - (dodgePlayer.InkEffectTime - (totalDuration - fadeInDuration)) / (float)fadeInDuration;
                }
                else if (dodgePlayer.InkEffectTime > fadeOutStartTime)
                {
                    // 完全显示
                    alpha = 1f;
                }
                else
                {
                    // 淡出
                    alpha = dodgePlayer.InkEffectTime / (float)fadeOutStartTime;
                }
                
                // 确保alpha在0和1之间
                alpha = MathHelper.Clamp(alpha, 0f, 1f);

                // 计算绘制位置和大小
                Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
                Vector2 textureSize = texture.Size();
                Vector2 position = (screenSize - textureSize) / 2f;
                
                // 绘制纹理
                spriteBatch.Draw(
                    texture,
                    position,
                    null,
                    Color.White * alpha,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0f
                );

                // 注意：移除了 spriteBatch.End() 调用，因为不应结束我们没有开始的绘制过程
            }
        }
    }
} 