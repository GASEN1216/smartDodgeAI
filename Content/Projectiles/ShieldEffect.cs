using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace smartDodgeAI.Content.Projectiles
{
    public class ShieldEffect : ModProjectile
    {
        // 显式指定纹理路径，以避免游戏因找不到默认纹理而报错
        // 我们使用一个1x1的像素作为占位符，因为它不会被实际绘制出来
        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 30; // 持续时间，半秒
            Projectile.aiStyle = -1; // 使用自定义AI
        }

        public override void AI()
        {
            // 尝试获取我们跟随的NPC
            NPC owner = Main.npc[(int)Projectile.ai[1]];

            // 如果NPC不存在或不再活动，则销毁投射物
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            // 将我们的位置锁定在NPC的中心
            Projectile.Center = owner.Center;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 获取画布和纹理
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D texture = TextureAssets.MagicPixel.Value;

            // 护盾颜色
            Color shieldColor = Color.DeepSkyBlue;

            // 基础半径
            float baseRadius = 50f;
            float radius = baseRadius * Projectile.scale;

            // 粗细
            int thickness = 3;

            // 计算消散动画的进度
            float dissipationProgress = 1f - (float)Projectile.timeLeft / 30f; // 0 to 1

            // 在半圆弧的一半处（45度角）定义会合点
            const float meetingPoint = MathHelper.PiOver4;

            // 计算动态的内边界和外边界
            float innerAngle = dissipationProgress * meetingPoint;
            float outerAngle = MathHelper.PiOver2 - dissipationProgress * meetingPoint;


            // 绘制一个从两端向中心收缩的半圆弧
            for (float angle = -MathHelper.PiOver2; angle <= MathHelper.PiOver2; angle += 0.02f)
            {
                float absAngle = Math.Abs(angle);

                // 如果当前角度不在两个动态边界之间，则跳过绘制
                if (absAngle < innerAngle || absAngle > outerAngle)
                {
                    continue;
                }

                // 计算圆弧上每个点的位置
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                // 将位置旋转到正确的方向
                Vector2 drawPos = Projectile.Center + offset.RotatedBy(Projectile.ai[0]);

                // 定义绘制区域和颜色
                Rectangle rect = new Rectangle((int)(drawPos.X - Main.screenPosition.X), (int)(drawPos.Y - Main.screenPosition.Y), thickness, thickness);
                
                // 绘制像素点
                spriteBatch.Draw(texture, rect, shieldColor);
            }

            // 返回false因为我们已经手动绘制了所有东西
            return false;
        }
    }
} 