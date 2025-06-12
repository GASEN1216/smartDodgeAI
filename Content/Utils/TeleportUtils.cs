using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using smartDodgeAI.Content.Config;
using smartDodgeAI.Content.Players;

namespace smartDodgeAI.Content.Utils
{
    public static class TeleportUtils
    {
        private const int MAX_TELEPORT_ATTEMPTS = 30;

        public static Vector2? FindTeleportPosition(NPC npc, Player targetPlayer)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return null;

            // 计算瞬移半径
            float teleportRadius = Vector2.Distance(npc.Center, targetPlayer.Center);
            // 如果距离太近，则不瞬移
            if (teleportRadius < 100)
            {
                return null;
            }

            // --- 优化瞬移落点 ---
            // 1. 计算从玩家到NPC的向量
            Vector2 vectorToNpc = npc.Center - targetPlayer.Center;

            // 2. 瞬移到NPC当前位置的对面半圆
            //    - 首先获取从玩家到NPC的角度
            //    - 然后将此角度旋转90度（Pi/2），得到半圆的起始切线点
            //    - 在此基础上再增加一个0到180度（Pi）的随机角度
            float angleToNpc = vectorToNpc.ToRotation();
            float targetAngle = angleToNpc + MathHelper.PiOver2;

            for (int i = 0; i < MAX_TELEPORT_ATTEMPTS; i++)
            {
                // 在目标半圆上随机取一个角度
                float randomAngle = targetAngle + Main.rand.NextFloat(MathHelper.Pi);
                Vector2 targetPosition = targetPlayer.Center + new Vector2((float)System.Math.Cos(randomAngle), (float)System.Math.Sin(randomAngle)) * teleportRadius;

                // 转换到图格坐标
                Point targetTile = targetPosition.ToTileCoordinates();
                
                // 根据NPC类型选择验证方法
                bool validSpot = false;
                if (npc.wet) // 水生
                {
                    validSpot = IsValidWaterSpot(targetPosition, npc.width, npc.height);
                }
                else if (npc.noGravity) // 飞行
                {
                    validSpot = IsValidAirSpot(targetPosition, npc.width, npc.height);
                }
                else // 地面
                {
                    validSpot = IsValidGroundSpot(targetPosition, npc.width, npc.height);
                }

                if (validSpot)
                {
                    return targetPosition; // 找到有效位置，返回它
                }
            }
            return null; // 未找到落点
        }

        public static void PerformTeleport(NPC npc, Player targetPlayer, Vector2 targetPosition, float originalSpeed)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            Vector2 oldPosition = npc.Center;

            // 执行瞬移
            npc.position = targetPosition;
            npc.oldPosition = targetPosition; // 防止联机抖动

            // 计算指向玩家的新方向
            Vector2 newDirection = Vector2.Normalize(targetPlayer.Center - npc.Center);
            
            // 应用新方向和旧速度，并增加一个随机的速度加成
            float speedBonus = Main.rand.NextFloat(1f, 3f);
            var dodgePlayer = targetPlayer.GetModPlayer<DodgePlayer>();
            if (dodgePlayer.HitRateBonus > 0)
            {
                speedBonus *= (1f + dodgePlayer.HitRateBonus);
            }
            npc.velocity = newDirection * (originalSpeed + speedBonus);
            
            // 手动同步
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

            // --- 瞬移特效 ---
            if (config != null)
            {
                if (config.EnableMissSound)
                {
                    SoundEngine.PlaySound(SoundID.Item8, oldPosition);
                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                }
                if (config.EnableMissParticles)
                {
                    for (int d = 0; d < 20; d++)
                    {
                        Dust.NewDust(oldPosition, 0, 0, DustID.MagicMirror, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 150, default, 1.5f);
                    }
                    for (int d = 0; d < 20; d++)
                    {
                        Dust.NewDust(npc.Center, 0, 0, DustID.MagicMirror, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 150, default, 1.5f);
                    }
                }
            }
        }

        public static Vector2? FindDodgeRollPosition(NPC npc, Projectile projectile)
        {
            // 翻滚距离 = 弹幕尺寸的两倍 + NPC碰撞盒直径（取最大边），至少 48 像素
            float dodgeDistance = Math.Max(projectile.width, projectile.height) * 2f + Math.Max(npc.width, npc.height);
            if (dodgeDistance < 48f)
                dodgeDistance = 48f;

            // 计算远离弹幕中心的初始方向
            Vector2 awayDir = (npc.Center - projectile.Center).SafeNormalize(Vector2.UnitX);

            // 如果 awayDir 为零（重叠），则使用随机方向
            if (awayDir == Vector2.Zero)
                awayDir = Main.rand.NextVector2CircularEdge(1f, 1f);

            // 计算朝向玩家的方向
            Vector2 playerDir = Vector2.Zero;
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player p = Main.player[projectile.owner];
                if (p.active && !p.dead)
                {
                    playerDir = (p.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                }
            }
            // 若获取失败则退回 awayDir 相反方向
            if (playerDir == Vector2.Zero)
                playerDir = -awayDir;

            // 在面向玩家的 ±90° 扇形内取 16 个等距方向
            const int DIR_COUNT = 16;
            float step = MathHelper.Pi / (DIR_COUNT); // ≈11.25°

            // 生成偏移数组
            float[] offsets = new float[DIR_COUNT];
            for (int i = 0; i < DIR_COUNT; i++)
            {
                offsets[i] = -MathHelper.PiOver2 + step/2f + step * i;
            }
            // 随机打乱顺序
            for (int i = 0; i < DIR_COUNT; i++)
            {
                int swap = Main.rand.Next(DIR_COUNT);
                (offsets[i], offsets[swap]) = (offsets[swap], offsets[i]);
            }

            foreach (float offset in offsets)
            {
                Vector2 testDir = playerDir.RotatedBy(offset);

                Vector2 targetCenter = npc.Center + testDir * dodgeDistance;
                Vector2 targetTopLeft = targetCenter - npc.Size / 2f;

                bool validSpot = (npc.noGravity || npc.wet)
                    ? IsValidAirSpot(targetTopLeft, npc.width, npc.height)
                    : IsValidGroundSpot(targetTopLeft, npc.width, npc.height);

                if (validSpot)
                    return targetTopLeft; // 成功找到
            }

            return null; // 全部尝试失败
        }

        private static bool IsValidGroundSpot(Vector2 position, int width, int height)
        {
            // 检查目标区域是否会卡墙
            if (Collision.SolidCollision(position, width, height))
            {
                return false;
            }

            // 检查脚下是否有坚实的地面
            int tileX = (int)(position.X + width / 2f) / 16;
            int tileY = (int)(position.Y + height) / 16;

            for (int i = 0; i < 3; i++) // 检查脚下3格，增加稳定性
            {
                Tile tile = Framing.GetTileSafely(tileX, tileY + i);
                if (WorldGen.SolidTile(tile) && !tile.IsActuated)
                {
                    return true; // 找到坚实地面
                }
            }
            return false;
        }

        private static bool IsValidAirSpot(Vector2 position, int width, int height)
        {
            // 只需要检查目标区域是否会卡墙
            return !Collision.SolidCollision(position, width, height);
        }

        private static bool IsValidWaterSpot(Vector2 position, int width, int height)
        {
            // 检查目标区域是否在水里且不会卡墙
            return Collision.WetCollision(position, width, height) && !Collision.SolidCollision(position, width, height);
        }

        public static Vector2? FindLeapApexPosition(NPC npc, Projectile projectile)
        {
            // 跳跃高度为敌怪自身碰撞体积+弹幕大小的一到三倍
            float jumpHeight = (npc.height + Math.Max(projectile.width, projectile.height)) * Main.rand.NextFloat(1.5f, 3f);

            // 目标悬停点在NPC正上方
            Vector2 apexPosition = npc.Center;
            apexPosition.Y -= jumpHeight;

            // 检查悬停点是否会卡墙
            if (Collision.SolidCollision(apexPosition - npc.Size / 2f, npc.width, npc.height))
            {
                return null; // 悬停点无效
            }
            return apexPosition;
        }
    }
} 