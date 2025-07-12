using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ID;
using smartDodgeAI.Content.Config;
using smartDodgeAI.Content.Utils;
using System;

namespace smartDodgeAI.Content.Systems
{
    public class AprilFoolsAudioSystem : ModSystem
    {
        private static bool _megalovaniaPlaying = false;
        private static int _megalovaniaTimer = 0;
        private const int MEGALOVANIA_DURATION = 1800; // 30秒（60fps * 30秒）

        public override void Load()
        {
            // 系统加载时的初始化
            _megalovaniaPlaying = false;
            _megalovaniaTimer = 0;
        }

        public override void Unload()
        {
            // 系统卸载时停止音乐
            StopMegalovania();
        }

        public override void PostUpdateEverything()
        {
            // 检查是否需要播放Megalovania
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config != null && config.EnableSkeletonOnlyDodge && ShouldPlayMegalovania())
            {
                // 检查是否有骷髅NPC在附近
                if (HasSkeletonNearby() && !_megalovaniaPlaying)
                {
                    StartMegalovania();
                }
            }

            // 更新Megalovania播放计时器
            if (_megalovaniaPlaying)
            {
                _megalovaniaTimer++;
                if (_megalovaniaTimer >= MEGALOVANIA_DURATION)
                {
                    StopMegalovania();
                }
            }
        }

        /// <summary>
        /// 开始播放Megalovania音乐
        /// </summary>
        public static void StartMegalovania()
        {
            if (!_megalovaniaPlaying && ShouldPlayMegalovania())
            {
                try
                {
                    // 尝试播放压缩版Megalovania
                    SoundEngine.PlaySound(new SoundStyle("smartDodgeAI/Content/Assets/Audio/Megalovania_Compressed"));
                    _megalovaniaPlaying = true;
                    _megalovaniaTimer = 0;

                    // 显示愚人节消息
                    if (Main.netMode != NetmodeID.Server)
                    {
                        string message = AprilFoolsTestSystem.IsTestMode ? 
                            "🎵 *Megalovania starts playing... (TEST MODE)* 🎵" : 
                            "🎵 *Megalovania starts playing...* 🎵";
                        Main.NewText(message, Color.Orange);
                    }
                }
                catch
                {
                    // 如果音频文件不存在，使用替代音效
                    if (Main.netMode != NetmodeID.Server)
                    {
                        string message = AprilFoolsTestSystem.IsTestMode ? 
                            "🎵 *Megalovania starts playing... (compressed version - TEST MODE)* 🎵" : 
                            "🎵 *Megalovania starts playing... (compressed version)* 🎵";
                        Main.NewText(message, Color.Orange);
                    }
                    
                    // 播放一个替代音效
                    SoundEngine.PlaySound(SoundID.Item4, Main.LocalPlayer.Center);
                    _megalovaniaPlaying = true;
                    _megalovaniaTimer = 0;
                }
            }
        }

        /// <summary>
        /// 停止播放Megalovania音乐
        /// </summary>
        public static void StopMegalovania()
        {
            if (_megalovaniaPlaying)
            {
                _megalovaniaPlaying = false;
                _megalovaniaTimer = 0;

                // 显示结束消息
                if (Main.netMode != NetmodeID.Server)
                {
                    string message = AprilFoolsTestSystem.IsTestMode ? 
                        "🎵 *Megalovania fades away... (TEST MODE)* 🎵" : 
                        "🎵 *Megalovania fades away...* 🎵";
                    Main.NewText(message, Color.Gray);
                }
            }
        }

        /// <summary>
        /// 检查附近是否有骷髅NPC
        /// </summary>
        private bool HasSkeletonNearby()
        {
            if (Main.LocalPlayer == null) return false;

            float detectionRange = 800f; // 检测范围
            Vector2 playerCenter = Main.LocalPlayer.Center;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && !npc.townNPC)
                {
                    // 检查距离
                    float distance = Vector2.Distance(playerCenter, npc.Center);
                    if (distance <= detectionRange)
                    {
                        // 检查是否为骷髅类型
                        if (IsSkeletonNPC(npc))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查NPC是否为骷髅类型
        /// </summary>
        private bool IsSkeletonNPC(NPC npc)
        {
            // 使用SkeletonUtils中的方法检查
            return SkeletonUtils.IsSkeleton(npc);
        }

        /// <summary>
        /// 获取当前Megalovania播放状态
        /// </summary>
        public static bool IsMegalovaniaPlaying => _megalovaniaPlaying;

        /// <summary>
        /// 检查是否应该播放Megalovania（静态方法）
        /// </summary>
        public static bool ShouldPlayMegalovania()
        {
            return SmartDodgeConfig.IsAprilFoolsDay() || AprilFoolsTestSystem.IsTestMode;
        }
    }
} 