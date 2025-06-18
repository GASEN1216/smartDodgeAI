using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace smartDodgeAI.Content.NPCs
{
    public class ShadowCloneManager : ModSystem
    {
        // 存储所有活跃分身的字典，键是分身NPC的whoAmI，值是对应的真身NPC的whoAmI
        public static Dictionary<int, int> ActiveClones = new Dictionary<int, int>();
        
        // 存储所有克隆源的字典，键是原始NPC的whoAmI，值是该NPC创建的所有分身的whoAmI列表
        public static Dictionary<int, List<int>> CloneSources = new Dictionary<int, List<int>>();

        public override void Load()
        {
            // 初始化字典
            ActiveClones = new Dictionary<int, int>();
            CloneSources = new Dictionary<int, List<int>>();
        }
        
        public override void Unload()
        {
            // 清理字典
            ActiveClones = null;
            CloneSources = null;
        }
        
        // 注册一个新的分身
        public static void RegisterClone(int cloneWhoAmI, int sourceWhoAmI)
        {
            ActiveClones[cloneWhoAmI] = sourceWhoAmI;
            
            if (!CloneSources.ContainsKey(sourceWhoAmI))
            {
                CloneSources[sourceWhoAmI] = new List<int>();
            }
            
            CloneSources[sourceWhoAmI].Add(cloneWhoAmI);
        }
        
        // 注销一个分身
        public static void UnregisterClone(int cloneWhoAmI)
        {
            if (ActiveClones.ContainsKey(cloneWhoAmI))
            {
                int sourceWhoAmI = ActiveClones[cloneWhoAmI];
                
                ActiveClones.Remove(cloneWhoAmI);
                
                if (CloneSources.ContainsKey(sourceWhoAmI))
                {
                    CloneSources[sourceWhoAmI].Remove(cloneWhoAmI);
                    
                    if (CloneSources[sourceWhoAmI].Count == 0)
                    {
                        CloneSources.Remove(sourceWhoAmI);
                    }
                }
            }
        }
        
        // 检查一个NPC是否是分身
        public static bool IsClone(int npcWhoAmI)
        {
            return ActiveClones.ContainsKey(npcWhoAmI);
        }
        
        // 获取分身的源NPC
        public static int GetCloneSource(int cloneWhoAmI)
        {
            if (ActiveClones.ContainsKey(cloneWhoAmI))
            {
                return ActiveClones[cloneWhoAmI];
            }
            return -1;
        }
    }
} 