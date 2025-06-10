using Terraria.ModLoader;

namespace smartDodgeAI.Content.Players
{
    public class DodgePlayer : ModPlayer
    {
        // This field will be set by the accessory
        public float HitRateBonus;

        public override void ResetEffects()
        {
            // Reset the bonus each frame
            HitRateBonus = 0f;
        }
    }
} 