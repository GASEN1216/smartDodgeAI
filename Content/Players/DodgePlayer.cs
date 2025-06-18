using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ID;

namespace smartDodgeAI.Content.Players
{
    public class DodgePlayer : ModPlayer
    {
        // This field will be set by the accessory
        public float HitRateBonus;
        public float TeleportDelayBonus;
        
        // 墨水效果相关字段
        public int InkEffectTime; // 墨水效果持续时间（帧数）
        public bool HasInkEffect => InkEffectTime > 0;
        public int InkSeed; // 用于在效果期间保持墨迹形状不变的随机种子
        
        // 用于调试的状态标记
        private bool _wasInkEffectActive = false;

        public override void ResetEffects()
        {
            // Reset the bonus each frame
            HitRateBonus = 0f;
            TeleportDelayBonus = 0f;
        }
        
        public override void PostUpdate()
        {
            // 更新墨水效果计时器
            if (InkEffectTime > 0)
            {
                if (!_wasInkEffectActive)
                {
                    _wasInkEffectActive = true;
                    // 当效果开始时，生成一个新的随机种子
                    InkSeed = Main.rand.Next();
                }
                
                InkEffectTime--;
                
                if (InkEffectTime > 0)
                {
                    Player.moveSpeed *= 0.8f;
                    
                    // 每隔30帧产生一些粒子效果
                    if (Main.GameUpdateCount % 30 == 0 && Player.whoAmI == Main.myPlayer)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Dust.NewDust(
                                Player.position,
                                Player.width,
                                Player.height,
                                DustID.Shadowflame,
                                0f,
                                0f,
                                100,
                                default,
                                0.8f
                            );
                        }
                    }
                }
                
                if (InkEffectTime == 0)
                {
                    _wasInkEffectActive = false;
                    InkSeed = 0; // 重置种子
                }
            }
            else if (_wasInkEffectActive)
            {
                _wasInkEffectActive = false;
                InkSeed = 0; // 确保在效果意外中断时也重置
            }
        }
    }
} 