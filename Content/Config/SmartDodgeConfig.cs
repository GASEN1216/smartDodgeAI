using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using smartDodgeAI.Content.NPCs;
using smartDodgeAI.Content.Systems;
using System.ComponentModel;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace smartDodgeAI.Content.Config
{
    public class ProjectileDodgeOverride
    {
        [Header("ProjectileType")]
        public ProjectileDefinition Projectile;

        [Header("DodgeChance")]
        [Range(0, 100)]
        [Slider]
        public int OverrideChance;

        public override string ToString()
        {
            if (Projectile == null || Projectile.IsUnloaded)
            {
                return "<Select a Projectile>";
            }
            return $"{Terraria.Lang.GetProjectileName(Projectile.Type).Value}: {OverrideChance}%";
        }
    }

    public class SmartDodgeConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("GlobalSettings")]
        [DefaultValue(25)]
        [Range(0, 100)]
        [Slider]
        public int MissChance = 25;

        [DefaultValue(true)]
        public bool ShowMissText = true;

        [DefaultValue(true)]
        public bool EnableMissSound = true;

        [DefaultValue(true)]
        public bool EnableMissParticles = true;

        [DefaultValue(true)]
        public bool EnableBossDodge = true;

        [DefaultValue(true)]
        public bool EnableNormalEnemyDodge = true;

        [Header("DodgeBehavior")]
        [DefaultValue(true)]
        public bool EnableTeleport = true;

        [DefaultValue(5)]
        [Range(0, 60)]
        [Slider]
        public int TeleportCooldown = 5;

        [Header("DamageTypeDodgeChance")]
        [DefaultValue(-1)]
        [Range(-1, 100)]
        [Slider]
        public int RangedDodgeChance = -1;

        [DefaultValue(-1)]
        [Range(-1, 100)]
        [Slider]
        public int MagicDodgeChance = -1;

        [DefaultValue(-1)]
        [Range(-1, 100)]
        [Slider]
        public int SummonDodgeChance = -1;

        [DefaultValue(-1)]
        [Range(-1, 100)]
        [Slider]
        public int MeleeDodgeChance = -1;

        [Header("ProjectileSpecificDodgeChance")]
        [SeparatePage]
        public List<ProjectileDodgeOverride> ProjectileOverrides { get; set; } = new List<ProjectileDodgeOverride>();

        // 愚人节彩蛋：骷髅专用闪避模式
        [Header("AprilFoolsSpecial")]
        [DefaultValue(false)]
        public bool EnableSkeletonOnlyDodge
        {
            get => _enableSkeletonOnlyDodge;
            set
            {
                // 如果尝试设置为true，检查是否为4月1日或测试模式
                if (value && !ShouldShowAprilFoolsOption())
                {
                    // 在客户端显示提示信息
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText("🎃 愚人节彩蛋仅在4月1日可用！", Color.Orange);
                    }
                    return; // 拒绝设置
                }
                _enableSkeletonOnlyDodge = value;
            }
        }
        
        private bool _enableSkeletonOnlyDodge = false;

        // 检查是否为4月1日
        public static bool IsAprilFoolsDay()
        {
            DateTime now = DateTime.Now;
            return now.Month == 4 && now.Day == 1;
        }

        // 检查是否应该显示愚人节选项
        public static bool ShouldShowAprilFoolsOption()
        {
            return IsAprilFoolsDay() || AprilFoolsTestSystem.IsTestMode;
        }

        // 重写配置显示逻辑，实现真正的条件显示
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message)
        {
            // 如果是骷髅专用选项且不是4月1日且不是测试模式，拒绝更改
            if (pendingConfig is SmartDodgeConfig config)
            {
                if (config.EnableSkeletonOnlyDodge != EnableSkeletonOnlyDodge && !ShouldShowAprilFoolsOption())
                {
                    message = "此选项仅在4月1日可用！";
                    return false;
                }
            }
            return base.AcceptClientChanges(pendingConfig, whoAmI, ref message);
        }

        // 重写OnChanged方法，在配置保存时进行日期检测和自动修正
        public override void OnChanged()
        {
            // 在配置保存时检查日期，如果不是4月1日且不是测试模式，自动将骷髅专用选项设置为false
            if (!ShouldShowAprilFoolsOption() && _enableSkeletonOnlyDodge)
            {
                _enableSkeletonOnlyDodge = false;
                
                // 在客户端显示提示信息
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("🎃 愚人节彩蛋已自动关闭（仅在4月1日可用）", Color.Orange);
                }
            }
            base.OnChanged();
        }
    }
} 