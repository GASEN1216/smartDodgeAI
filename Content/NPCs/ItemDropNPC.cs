using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using smartDodgeAI.Content.Items.Accessories;

namespace smartDodgeAI.Content.NPCs
{
    public class ItemDropNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // King Slime -> Gel Echo (33% chance)
            if (npc.type == NPCID.KingSlime)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<GelEcho>(), 3, 1, 1)); // 1 in 3 chance
            }

            // Eye of Cthulhu -> Retinal Ripple (25% chance)
            if (npc.type == NPCID.EyeofCthulhu)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RetinalRipple>(), 4, 1, 1)); // 1 in 4 chance
            }
            
            // Queen Bee -> Hive Mirage (20% chance)
            if (npc.type == NPCID.QueenBee)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HiveMirage>(), 5, 1, 1)); // 1 in 5 chance
            }
        }

        public override void OnKill(NPC npc)
        {
            // Brain of Cthulhu is not segmented, so a simple OnKill check is sufficient.
            if (npc.type == NPCID.BrainofCthulhu)
            {
                 Item.NewItem(npc.GetSource_Death(), npc.getRect(), ModContent.ItemType<ShadowRemnant>());
            }
            
            // For Eater of Worlds, we must ensure the drop only happens once the entire boss is defeated.
            // We check if the killed NPC is a head and if it's the very last head alive.
            if (npc.type == NPCID.EaterofWorldsHead && !NPC.AnyNPCs(NPCID.EaterofWorldsHead))
            {
                 Item.NewItem(npc.GetSource_Death(), npc.getRect(), ModContent.ItemType<ShadowRemnant>());
            }
        }
    }
} 