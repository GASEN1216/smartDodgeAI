using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using smartDodgeAI.Content.Buffs;
using System.Collections.Generic;

namespace smartDodgeAI.Content.NPCs
{
    public class ShadowCloneGlobalNPC : GlobalNPC
    {
        public bool IsShadowClone { get; set; } = false;
        public int LifeTime { get; set; } = 0;
        public int MaxLifeTime { get; set; } = 0;
        public float AlphaValue { get; set; } = 1f;
        
        public override bool InstancePerEntity => true;
        
        public override void PostAI(NPC npc)
        {
            if (IsShadowClone)
            {
                // 确保分身不会掉落物品（设置价值为0）
                npc.value = 0;
                
                // 增加生命时间
                LifeTime++;
                
                // 临近消失时开始淡出
                if (LifeTime > MaxLifeTime - 30)
                {
                    AlphaValue = 1f - (LifeTime - (MaxLifeTime - 30)) / 30f;
                    if (AlphaValue < 0.1f) AlphaValue = 0.1f;
                }
                
                // 如果已达到最大生命时间，移除分身
                if (LifeTime >= MaxLifeTime)
                {
                    npc.active = false;
                    npc.life = 0;
                    ShadowCloneManager.UnregisterClone(npc.whoAmI);
                }
            }
        }
        
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (IsShadowClone)
            {
                // 修改绘制颜色使分身更暗且半透明
                drawColor.R = (byte)(drawColor.R * 0.7 * AlphaValue);
                drawColor.G = (byte)(drawColor.G * 0.5 * AlphaValue);
                drawColor.B = (byte)(drawColor.B * 0.9 * AlphaValue);
                drawColor.A = (byte)(drawColor.A * AlphaValue);
            }
        }
        
        public override bool CheckDead(NPC npc)
        {
            // 如果是分身被杀死，给击杀的玩家施加debuff
            if (IsShadowClone && npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers)
            {
                // 确保不会掉落物品（设置价值为0）
                npc.value = 0;
                
                Player player = Main.player[npc.lastInteraction];
                if (player.active && !player.dead)
                {
                    // 施加减攻击力debuff，持续2秒（120帧）
                    player.AddBuff(ModContent.BuffType<ShadowConfusion>(), 120);
                }
                
                // 注销这个分身
                ShadowCloneManager.UnregisterClone(npc.whoAmI);
            }
            
            return base.CheckDead(npc);
        }
        
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 如果是分身，阻止掉落物品
            if (IsShadowClone)
            {
                // 清空所有掉落规则
                npcLoot.RemoveWhere(rule => true);
            }
        }
    }
} 