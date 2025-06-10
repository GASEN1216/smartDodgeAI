using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using smartDodgeAI.Content.NPCs;
using System.ComponentModel;
using System.Collections.Generic;

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

        [Header("TeleportSettings")]
        [DefaultValue(true)]
        public bool EnableTeleport = true;

        [DefaultValue(5)]
        [Range(0, 60)]
        [Slider]
        public int TeleportCooldown = 5;

        // OnChanged 似乎不需要了，因为 GlobalNPC 会在需要时读取配置实例
        /*
        public override void OnChanged()
        {
            var globalNPCInstance = ModContent.GetInstance<SmartDodgeGlobalNPC>();
            if (globalNPCInstance != null)
            {
                globalNPCInstance.UpdateConfig(this);
            }
        }
        */
    }
} 