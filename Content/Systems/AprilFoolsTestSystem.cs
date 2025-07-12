using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using smartDodgeAI.Content.Config;
using smartDodgeAI.Content.Utils;
using System;

namespace smartDodgeAI.Content.Systems
{
    /// <summary>
    /// 愚人节彩蛋测试系统
    /// 用于验证骷髅专用模式和Megalovania播放功能
    /// </summary>
    public class AprilFoolsTestSystem : ModSystem
    {
        private static bool _testMode = false;
        private static int _testTimer = 0;
        private const int TEST_DURATION = 600; // 10秒测试时间

        public override void Load()
        {
            _testMode = false;
            _testTimer = 0;
        }

        public override void Unload()
        {
            _testMode = false;
            _testTimer = 0;
        }

        public override void PostUpdateEverything()
        {
            // 测试模式：按F12键激活测试
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F12) && !_testMode)
            {
                ActivateTestMode();
            }

            // 更新测试计时器
            if (_testMode)
            {
                _testTimer++;
                if (_testTimer >= TEST_DURATION)
                {
                    DeactivateTestMode();
                }
            }
        }

        /// <summary>
        /// 激活测试模式
        /// </summary>
        private void ActivateTestMode()
        {
            _testMode = true;
            _testTimer = 0;

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("🎃 愚人节彩蛋测试模式已激活！", Color.Orange);
                Main.NewText("🎵 Megalovania音乐将在骷髅出现时播放", Color.Yellow);
                Main.NewText("⏰ 测试将在10秒后自动结束", Color.Gray);
            }

            // 强制启用骷髅专用模式进行测试
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config != null)
            {
                config.EnableSkeletonOnlyDodge = true;
            }
        }

        /// <summary>
        /// 停用测试模式
        /// </summary>
        private void DeactivateTestMode()
        {
            _testMode = false;
            _testTimer = 0;

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("🎃 愚人节彩蛋测试模式已结束", Color.Gray);
            }

            // 恢复配置
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config != null)
            {
                config.EnableSkeletonOnlyDodge = false;
            }

            // 停止Megalovania音乐
            AprilFoolsAudioSystem.StopMegalovania();
        }

        /// <summary>
        /// 检查是否为测试模式
        /// </summary>
        public static bool IsTestMode => _testMode;

        /// <summary>
        /// 检查是否应该显示愚人节选项
        /// </summary>
        public static bool ShouldShowAprilFoolsOption()
        {
            return SmartDodgeConfig.IsAprilFoolsDay() || IsTestMode;
        }

        /// <summary>
        /// 获取测试信息
        /// </summary>
        public static string GetTestInfo()
        {
            if (!_testMode) return "测试模式未激活";

            var config = ModContent.GetInstance<SmartDodgeConfig>();
            bool skeletonMode = config?.EnableSkeletonOnlyDodge ?? false;
            bool megalovaniaPlaying = AprilFoolsAudioSystem.IsMegalovaniaPlaying;

            return $"测试模式: 激活中\n" +
                   $"骷髅专用模式: {(skeletonMode ? "启用" : "禁用")}\n" +
                   $"Megalovania播放: {(megalovaniaPlaying ? "是" : "否")}\n" +
                   $"剩余时间: {Math.Max(0, (TEST_DURATION - _testTimer) / 60f):F1}秒";
        }
    }
} 