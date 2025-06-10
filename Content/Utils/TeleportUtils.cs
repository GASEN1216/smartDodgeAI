using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using smartDodgeAI.Content.Config;

namespace smartDodgeAI.Content.Utils
{
    public static class TeleportUtils
    {
        private const int MAX_TELEPORT_ATTEMPTS = 30;

        public static bool AttemptTeleport(NPC npc, Player targetPlayer)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return false;

            // 记录原始速度大小
            float originalSpeed = npc.velocity.Length();

            // 计算瞬移半径
            float teleportRadius = Vector2.Distance(npc.Center, targetPlayer.Center);
            // 如果距离太近，则不瞬移
            if (teleportRadius < 100)
            {
                return false;
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
                    // 执行瞬移
                    // npc.Teleport(targetPosition, 0); // 原方法会产生声音，替换为手动设置位置
                    npc.position = targetPosition;
                    npc.oldPosition = targetPosition; // 防止联机抖动
                    
                    // 计算指向玩家的新方向
                    Vector2 newDirection = Vector2.Normalize(targetPlayer.Center - npc.Center);
                    
                    // 应用新方向和旧速度，并增加一个随机的速度加成
                    npc.velocity = newDirection * (originalSpeed + Main.rand.NextFloat(1f, 3f));
                    
                    // 手动同步
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    return true; // 瞬移成功
                }
            }
            return false; // 未找到落点
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
    }
} 