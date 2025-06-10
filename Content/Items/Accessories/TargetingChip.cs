using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using smartDodgeAI.Content.Players;
using smartDodgeAI.Content.Config;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace smartDodgeAI.Content.Items.Accessories
{
    public class TargetingChip : ModItem
    {
        // NOTE: This item needs a 32x32 texture file named TargetingChip.png in the same folder.

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config != null)
            {
                // The bonus is the configured miss chance / 5, converted to a percentage float.
                // e.g., if MissChance is 25, bonus is (25 / 5) / 100 = 0.05f (5%)
                player.GetModPlayer<DodgePlayer>().HitRateBonus = (config.MissChance / 5f) / 100f;
            }
        }

        public override void AddRecipes()
        {
            // Remove old recipes by creating a recipe that crafts nothing. This is a standard tModLoader practice.
            // This is not strictly necessary if we control the source, but it's good practice.
            // However, a simpler way is to just replace the recipe logic.

            // Recipe from Warrior Emblem
            Recipe recipeWarrior = CreateRecipe();
            recipeWarrior.AddIngredient(ItemID.WarriorEmblem, 1);
            recipeWarrior.AddTile(TileID.TinkerersWorkbench);
            recipeWarrior.Register();

            // Recipe from Ranger Emblem
            Recipe recipeRanger = CreateRecipe();
            recipeRanger.AddIngredient(ItemID.RangerEmblem, 1);
            recipeRanger.AddTile(TileID.TinkerersWorkbench);
            recipeRanger.Register();

            // Recipe from Sorcerer Emblem
            Recipe recipeSorcerer = CreateRecipe();
            recipeSorcerer.AddIngredient(ItemID.SorcererEmblem, 1);
            recipeSorcerer.AddTile(TileID.TinkerersWorkbench);
            recipeSorcerer.Register();
            
            // Recipe from Summoner Emblem
            Recipe recipeSummoner = CreateRecipe();
            recipeSummoner.AddIngredient(ItemID.SummonerEmblem, 1);
            recipeSummoner.AddTile(TileID.TinkerersWorkbench);
            recipeSummoner.Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config != null)
            {
                float bonusPercent = config.MissChance / 5f;
                
                string bonusText = Language.GetTextValue("Mods.smartDodgeAI.ItemTooltips.TargetingChipBonus", $"{bonusPercent:F2}");

                var line = new TooltipLine(Mod, "DynamicBonus", bonusText)
                {
                    OverrideColor = Color.LightGreen
                };
                tooltips.Add(line);
            }
        }
    }
} 