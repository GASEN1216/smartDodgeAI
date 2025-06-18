using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using smartDodgeAI.Content.Players;

namespace smartDodgeAI.Content.Projectiles
{
    public class InkProjectile : ModProjectile
    {
        // 使用内置的像素纹理，在游戏中绘制时会被自定义绘制覆盖
        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetStaticDefaults()
        {
            // 设置显示名称 - 使用自动本地化名称
            // 新版tModLoader不再使用SetDefault方法
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;  // 对玩家友好
            Projectile.hostile = true;    // 对玩家有害
            Projectile.penetrate = 1;     // 穿透一个目标
            Projectile.tileCollide = true; // 与地形碰撞
            Projectile.timeLeft = 180;    // 存在3秒
            Projectile.alpha = 100;       // 半透明
            Projectile.ignoreWater = false; // 不忽略水
            Projectile.aiStyle = -1;      // 使用自定义AI
            Projectile.knockBack = 0f;    // 设置击退力为0
        }

        public override void AI()
        {
            // 墨水粒子留下痕迹
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Shadowflame, // 使用暗影火焰粒子表示墨水
                    0f,
                    0f,
                    100,
                    new Color(20, 20, 40),
                    0.8f
                );
                dust.noGravity = true;
                dust.velocity *= 0.3f;
            }

            // 让墨水下落
            Projectile.velocity.Y += 0.1f;

            // 墨水旋转
            Projectile.rotation += 0.1f;

            // 如果在水中，加速消失
            if (Projectile.wet)
            {
                Projectile.timeLeft -= 2;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 墨水撞击地面时减速并产生飞溅效果
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            {
                Projectile.velocity.X = -oldVelocity.X * 0.2f;
            }
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            {
                Projectile.velocity.Y = -oldVelocity.Y * 0.2f;
            }

            // 播放墨水飞溅音效
            SoundEngine.PlaySound(SoundID.Drip, Projectile.position);

            // 创建飞溅粒子效果
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Shadowflame,
                    0f,
                    0f,
                    100,
                    new Color(20, 20, 40),
                    0.8f
                );
                dust.velocity *= 1.4f;
            }

            // 减少存活时间
            Projectile.timeLeft -= 60;
            if (Projectile.timeLeft <= 0)
                return true;

            return false;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            // 阻止弹幕造成任何实际伤害
            modifiers.FinalDamage.Base = 0;
            modifiers.SetMaxDamage(0);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // 当墨水击中玩家时，给玩家添加墨水效果
            var dodgePlayer = target.GetModPlayer<DodgePlayer>();
            dodgePlayer.InkEffectTime = 120; // 2秒墨水效果

            // 治疗玩家1点生命
            target.Heal(1);
            
            // 创建飞溅粒子效果
            for (int i = 0; i < 15; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    target.position,
                    target.width,
                    target.height,
                    DustID.Shadowflame,
                    0f,
                    0f,
                    100,
                    new Color(20, 20, 40),
                    1.2f
                );
                dust.velocity *= 2f;
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 获取纹理和画布
            Texture2D texture = TextureAssets.MagicPixel.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // 绘制墨水滴
            float scale = Projectile.scale * 1.5f;
            Color inkColor = new Color(10, 10, 30, 200);
            
            // 绘制多个不同大小的墨水粒子，形成墨水滴效果
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-5f, 5f),
                    Main.rand.NextFloat(-5f, 5f)
                );
                float particleScale = scale * Main.rand.NextFloat(0.5f, 1.5f);
                
                Rectangle rect = new Rectangle(
                    (int)(drawPosition.X + offset.X - 4 * particleScale),
                    (int)(drawPosition.Y + offset.Y - 4 * particleScale),
                    (int)(8 * particleScale),
                    (int)(8 * particleScale)
                );
                
                Main.spriteBatch.Draw(
                    texture,
                    rect,
                    null,
                    inkColor,
                    Projectile.rotation,
                    new Vector2(0.5f, 0.5f),
                    SpriteEffects.None,
                    0f
                );
            }
            
            return false; // 返回false因为我们已经手动绘制了
        }
    }
} 