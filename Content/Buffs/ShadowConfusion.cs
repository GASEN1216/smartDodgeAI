using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace smartDodgeAI.Content.Buffs
{
    public class ShadowConfusion : ModBuff
    {
        public const float DamageReductionPercent = 25f;
        public static float DamageMultiplier = 1f - DamageReductionPercent / 100f;

        public override void SetStaticDefaults()
        {
            // 设置为debuff
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            // 显示剩余时间
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true; // 护士无法移除
            
            // 本地化会通过类名自动处理
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 降低玩家各种伤害
            player.GetDamage(DamageClass.Generic) *= DamageMultiplier;
            player.GetDamage(DamageClass.Melee) *= DamageMultiplier;
            player.GetDamage(DamageClass.Ranged) *= DamageMultiplier;
            player.GetDamage(DamageClass.Magic) *= DamageMultiplier;
            player.GetDamage(DamageClass.Summon) *= DamageMultiplier;
        }
    }
} 