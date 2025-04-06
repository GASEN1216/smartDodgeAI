using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using smartDodgeAI.Content.NPCs;
using System.ComponentModel;

namespace smartDodgeAI.Content.Config
{
    public class SmartDodgeConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

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