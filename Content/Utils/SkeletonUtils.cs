using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace smartDodgeAI.Content.Utils
{
    /// <summary>
    /// 骷髅NPC识别工具类
    /// </summary>
    public static class SkeletonUtils
    {
        /// <summary>
        /// 检查NPC是否为骷髅类型
        /// </summary>
        /// <param name="npc">要检查的NPC</param>
        /// <returns>如果是骷髅类型返回true，否则返回false</returns>
        public static bool IsSkeleton(NPC npc)
        {
            if (npc == null || !npc.active) return false;

            // 检查原版骷髅类型
            if (IsVanillaSkeleton(npc.type)) return true;

            // 检查Mod骷髅类型（通过名称匹配）
            if (IsModSkeleton(npc)) return true;

            return false;
        }

        /// <summary>
        /// 检查是否为原版骷髅类型
        /// </summary>
        private static bool IsVanillaSkeleton(int npcType)
        {
            // 原版骷髅类型列表
            return npcType switch
            {
                NPCID.Skeleton => true,           // 普通骷髅
                NPCID.SkeletonArcher => true,     // 骷髅弓箭手
                NPCID.SkeletonCommando => true,   // 骷髅突击队员
                NPCID.SkeletonSniper => true,     // 骷髅狙击手
                NPCID.SkeletonTopHat => true,     // 骷髅礼帽
                NPCID.SkeletonAstonaut => true,   // 骷髅宇航员
                NPCID.SkeletonAlien => true,      // 骷髅外星人
                _ => false
            };
        }

        /// <summary>
        /// 检查是否为Mod骷髅类型
        /// </summary>
        private static bool IsModSkeleton(NPC npc)
        {
            if (npc.ModNPC == null) return false;

            string npcName = npc.ModNPC.Name?.ToLower() ?? "";
            string displayName = npc.FullName?.ToLower() ?? "";

            // 通过名称关键词识别骷髅
            string[] skeletonKeywords = {
                "skeleton", "skel", "bone", "undead", "skele", "骷髅", "骨头"
            };

            foreach (string keyword in skeletonKeywords)
            {
                if (npcName.Contains(keyword) || displayName.Contains(keyword))
                {
                    return true;
                }
            }

            // 检查特定的Mod骷髅类型
            return IsSpecificModSkeleton(npc);
        }

        /// <summary>
        /// 检查特定的Mod骷髅类型
        /// </summary>
        private static bool IsSpecificModSkeleton(NPC npc)
        {
            // 这里可以添加对特定Mod骷髅的支持
            // 例如：Calamity Mod, Thorium Mod等
            
            string modName = npc.ModNPC.Mod?.Name?.ToLower() ?? "";
            string npcName = npc.ModNPC.Name?.ToLower() ?? "";

            // Calamity Mod骷髅
            if (modName.Contains("calamity"))
            {
                if (npcName.Contains("skeleton") || npcName.Contains("bone"))
                {
                    return true;
                }
            }

            // Thorium Mod骷髅
            if (modName.Contains("thorium"))
            {
                if (npcName.Contains("skeleton") || npcName.Contains("bone"))
                {
                    return true;
                }
            }

            // 可以继续添加其他Mod的支持
            return false;
        }

        /// <summary>
        /// 获取骷髅类型的描述
        /// </summary>
        public static string GetSkeletonDescription(NPC npc)
        {
            if (!IsSkeleton(npc)) return "非骷髅";

            if (IsVanillaSkeleton(npc.type))
            {
                return npc.FullName;
            }
            else if (npc.ModNPC != null)
            {
                return $"{npc.ModNPC.Mod?.DisplayName ?? "未知Mod"} - {npc.FullName}";
            }

            return "未知骷髅类型";
        }

        /// <summary>
        /// 检查是否为Boss骷髅
        /// </summary>
        public static bool IsSkeletonBoss(NPC npc)
        {
            if (!IsSkeleton(npc)) return false;

            // 检查是否为Boss
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
            {
                return true;
            }

            // 检查Mod Boss骷髅
            if (npc.ModNPC != null)
            {
                string npcName = npc.ModNPC.Name?.ToLower() ?? "";
                string[] bossKeywords = { "boss", "king", "queen", "lord", "master", "boss骷髅", "骷髅王" };
                
                foreach (string keyword in bossKeywords)
                {
                    if (npcName.Contains(keyword))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
} 