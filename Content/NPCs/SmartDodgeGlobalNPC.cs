using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.Localization;
using System.Reflection;
using smartDodgeAI.Content.Config;
using smartDodgeAI.Content.Utils;
using smartDodgeAI.Content.Players;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace smartDodgeAI.Content.NPCs
{
    // 用于跟踪哪些NPC已经闪避了弹幕
    public class MissTracker : ModSystem
    {
        // 记录已闪避弹幕的NPC
        public static bool[] NPCsMissed = new bool[Main.maxNPCs];
        
        public override void Load()
        {
            // 重置所有NPC的闪避状态
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPCsMissed[i] = false;
            }
        }
        
        // 每帧重置闪避状态
        public override void PostUpdateEverything()
        {
            // 重置所有NPC的闪避状态
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPCsMissed[i] = false;
            }
        }
    }

    public class SmartDodgeGlobalNPC : GlobalNPC
    {
        // --- 配置相关字段 ---
        private bool enableBossDodge = true;
        private bool enableNormalEnemyDodge = true;
        private int missChance = 25;
        private bool showMissText = true;
        private bool enableMissSound = true;
        private bool enableMissParticles = true;

        // --- 瞬移冷却计时器 ---
        private double _lastTeleportTime = -1; // 使用-1表示从未瞬移过

        // --- 瞬移延迟相关字段 ---
        private int _teleportDelayTimer;
        private Vector2? _teleportTargetPosition;
        private int _teleportTargetPlayerId = -1;

        // --- 本地AI索引 ---
        private const int DODGE_TIMER = 0;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return false;
            if (entity.boss)
                return lateInstantiation && config.EnableBossDodge;
            else
                return lateInstantiation && config.EnableNormalEnemyDodge && !entity.townNPC;
        }

        public override bool PreAI(NPC npc)
        {
            // 读取当前配置
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return true;

            // 更新配置值
            enableBossDodge = config.EnableBossDodge;
            enableNormalEnemyDodge = config.EnableNormalEnemyDodge;
            missChance = config.MissChance;
            showMissText = config.ShowMissText;
            enableMissSound = config.EnableMissSound;
            enableMissParticles = config.EnableMissParticles;

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (_teleportDelayTimer > 0)
            {
                _teleportDelayTimer--;
                if (_teleportDelayTimer == 0 && _teleportTargetPosition.HasValue && _teleportTargetPlayerId != -1)
                {
                    // 确保目标玩家仍然有效
                    if (_teleportTargetPlayerId >= 0 && _teleportTargetPlayerId < Main.maxPlayers)
                    {
                        Player targetPlayer = Main.player[_teleportTargetPlayerId];
                        if (targetPlayer.active && !targetPlayer.dead)
                        {
                            float originalSpeed = npc.velocity.Length();
                            TeleportUtils.PerformTeleport(npc, targetPlayer, _teleportTargetPosition.Value, originalSpeed);
                        }
                    }
                    
                    // 无论瞬移是否成功，都重置状态
                    _teleportTargetPosition = null;
                    _teleportTargetPlayerId = -1;
                }
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_teleportDelayTimer > 0 && _teleportTargetPosition.HasValue)
            {
                // 获取NPC的纹理和帧信息
                Texture2D texture = TextureAssets.Npc[npc.type].Value;
                Rectangle frame = npc.frame;
                Vector2 origin = frame.Size() / 2f;
                float scale = npc.scale;

                // 使用NPC当前的朝向
                float rotation = npc.rotation;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // 计算绘制位置，将左上角坐标转换为中心坐标
                Vector2 drawPos = _teleportTargetPosition.Value + origin - screenPos;
                
                // --- 绘制效果 ---
                Color outlineColor = Color.White;
                Color mainColor = Color.Gray * 0.7f;
                
                // 绘制轮廓
                float offset = 2f;
                spriteBatch.Draw(texture, drawPos + new Vector2(offset, 0), frame, outlineColor, rotation, origin, scale, effects, 0f);
                spriteBatch.Draw(texture, drawPos - new Vector2(offset, 0), frame, outlineColor, rotation, origin, scale, effects, 0f);
                spriteBatch.Draw(texture, drawPos + new Vector2(0, offset), frame, outlineColor, rotation, origin, scale, effects, 0f);
                spriteBatch.Draw(texture, drawPos - new Vector2(0, offset), frame, outlineColor, rotation, origin, scale, effects, 0f);

                // 绘制主体
                spriteBatch.Draw(texture, drawPos, frame, mainColor, rotation, origin, scale, effects, 0f);
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // 检查是否应用闪避逻辑
            bool canApply = (npc.boss && enableBossDodge) || (!npc.boss && !npc.townNPC && enableNormalEnemyDodge);
            if (!canApply) return;

            // 获取配置实例
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return;

            // --- 闪避率计算 ---
            int currentMissChance = -1;

            // 1. 检查特定弹幕的覆写设置（最高优先级）
            var overrideConfig = config.ProjectileOverrides.FirstOrDefault(o => o.Projectile != null && o.Projectile.Type == projectile.type);
            if (overrideConfig != null)
            {
                currentMissChance = overrideConfig.OverrideChance;
            }
            else
            {
                // 2. 检查伤害类型的覆写设置
                if (projectile.DamageType == DamageClass.Ranged && config.RangedDodgeChance != -1)
                {
                    currentMissChance = config.RangedDodgeChance;
                }
                else if (projectile.DamageType == DamageClass.Magic && config.MagicDodgeChance != -1)
                {
                    currentMissChance = config.MagicDodgeChance;
                }
                else if (projectile.DamageType == DamageClass.Summon && config.SummonDodgeChance != -1)
                {
                    currentMissChance = config.SummonDodgeChance;
                }
                else if (projectile.DamageType == DamageClass.Melee && config.MeleeDodgeChance != -1)
                {
                    currentMissChance = config.MeleeDodgeChance;
                }
            }

            // 3. 如果没有任何覆写，则使用全局闪避率
            if (currentMissChance == -1)
            {
                currentMissChance = missChance;
            }
            // --- 闪避率计算结束 ---

            // --- 命中率饰品加成 ---
            // 确保弹幕所有者是有效的玩家
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player owner = Main.player[projectile.owner];
                if (owner.active && !owner.dead)
                {
                    var dodgePlayer = owner.GetModPlayer<DodgePlayer>();
                    if (dodgePlayer.HitRateBonus > 0)
                    {
                        // 降低闪避率
                        currentMissChance = (int)(currentMissChance * (1f - dodgePlayer.HitRateBonus));
                    }
                }
            }
            
            // 检查随机概率是否触发miss
            if (currentMissChance > 0 && Main.rand.Next(100) < currentMissChance)
            {
                // 标记此NPC为闪避状态
                MissTracker.NPCsMissed[npc.whoAmI] = true;
                
                // 完全禁用所有伤害效果
                modifiers.FinalDamage.Base = 0;
                modifiers.FinalDamage.Flat = -9999; // 确保伤害不会有最低值的限制
                modifiers.SetMaxDamage(0);
                modifiers.SourceDamage *= 0;
                modifiers.Knockback *= 0f; // 禁用击退
                modifiers.DisableCrit(); // 禁用暴击
                
                // 不能设置DamageType，它是只读的
                // modifiers.DamageType = DamageClass.Default; // 重置伤害类型
                
                // 终止弹幕（将弹幕销毁），但保留仆从
                if (!projectile.minion)
                {
                    projectile.active = false;
                    projectile.netUpdate = true; // 确保在多人游戏中同步状态
                }
                
                // 如果配置了显示文本，则在弹幕命中位置显示"MISS"
                if (showMissText && Main.netMode != NetmodeID.Server)
                {
                    // 创建MISS文本（客户端）
                    if (Main.netMode != NetmodeID.Server)
                    {
                        // 生成文本在命中点
                        Vector2 textPosition = projectile.Center;
                        
                        // 在服务器上，这会被发送到所有客户端
                        Rectangle textArea = new Rectangle(
                            (int)textPosition.X - 20, 
                            (int)textPosition.Y - 20,
                            40, 40);
                        
                        // 创建MISS文本
                        CombatText.NewText(textArea, Color.LightGray, "MISS", true, true);
                    }
                }

                // 根据配置播放闪避音效
                if (enableMissSound)
                {
                    SoundEngine.PlaySound(SoundID.Item30, npc.position);
                }
                
                // 根据配置生成闪避粒子效果
                if (enableMissParticles)
                {
                    for (int d = 0; d < 10; d++)
                    {
                        Vector2 dustVel = new Vector2(
                            Main.rand.NextFloat(-3f, 3f),
                            Main.rand.NextFloat(-3f, 3f)
                        );
                        Dust.NewDust(
                            npc.position + new Vector2(Main.rand.Next(npc.width), Main.rand.Next(npc.height)), 
                            0, 0, 
                            DustID.MagicMirror, 
                            dustVel.X, dustVel.Y, 
                            150, default(Color), 1.2f
                        );
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // 确保弹幕所有者是有效的玩家
                    if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                    {
                        Player targetPlayer = Main.player[projectile.owner];
                        if (targetPlayer.active && !targetPlayer.dead)
                        {
                            if (config.EnableTeleport)
                            {
                                // --- 冲锋行为检查 ---
                                // 只有当NPC正在朝玩家移动时，才考虑瞬移
                                bool isCharging = npc.realLife == -1 &&
                                                  npc.velocity.LengthSquared() > 0.1f &&
                                                  Vector2.Dot(npc.velocity, targetPlayer.Center - npc.Center) > 0;

                                if (isCharging)
                                {
                                    // --- 瞬移冷却检查 ---
                                    double cooldownInTicks = config.TeleportCooldown * 60; // 将秒转换为ticks
                                    if (_lastTeleportTime != -1 && Main.GameUpdateCount - _lastTeleportTime < cooldownInTicks)
                                    {
                                        // 冷却中，不瞬移
                                    }
                                    else
                                    {
                                        Vector2 oldPosition = npc.Center;
                                        // 修改为不直接瞬移，而是查找位置并设置延迟
                                        Vector2? targetPos = TeleportUtils.FindTeleportPosition(npc, targetPlayer);

                                        if (targetPos.HasValue)
                                        {
                                            _teleportTargetPosition = targetPos;
                                            _teleportDelayTimer = 30; // 0.5秒延迟 (60 FPS)
                                            _teleportTargetPlayerId = targetPlayer.whoAmI;
                                            _lastTeleportTime = Main.GameUpdateCount; // 更新瞬移冷却计时器
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 添加对物品（近战）伤害的处理方法
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            // 注意：我们只在非近战伤害或复合伤害（比如投掷+近战）类武器时才检查闪避
            // 对于纯近战攻击，我们不应用闪避逻辑
            
            // 检查是否是纯近战伤害
            bool isPurelyMelee = item.damage > 0 && item.DamageType == DamageClass.Melee && item.shoot <= 0;
            
            // 对纯近战不应用闪避
            if (isPurelyMelee) return;
            
            // 检查是否应用闪避逻辑
            bool canApply = (npc.boss && enableBossDodge) || (!npc.boss && !npc.townNPC && enableNormalEnemyDodge);
            if (!canApply) return;

            // 获取配置实例
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return;

            // 检查随机概率是否触发miss
            if (Main.rand.Next(100) < missChance)
            {
                // 标记此NPC为闪避状态
                MissTracker.NPCsMissed[npc.whoAmI] = true;
                
                // 完全禁用所有伤害效果
                modifiers.FinalDamage.Base = 0;
                modifiers.FinalDamage.Flat = -9999; // 确保伤害不会有最低值的限制
                modifiers.SetMaxDamage(0);
                modifiers.SourceDamage *= 0;
                modifiers.Knockback *= 0f; // 禁用击退
                modifiers.DisableCrit(); // 禁用暴击
                
                // 不能设置DamageType，它是只读的
                // modifiers.DamageType = DamageClass.Default; // 重置伤害类型
                
                // 如果配置了显示文本，则在近战命中位置显示"MISS"
                if (showMissText && Main.netMode != NetmodeID.Server)
                {
                    // 计算命中点 - 在玩家和NPC之间的位置
                    Vector2 hitPosition = (npc.Center + player.Center) / 2f;
                    
                    // 绘制MISS文本
                    Rectangle textArea = new Rectangle(
                        (int)hitPosition.X - 20, 
                        (int)hitPosition.Y - 20,
                        40, 40);
                    
                    CombatText.NewText(textArea, Color.LightGray, "MISS", true, true);
                }

                // 根据配置播放闪避音效
                if (enableMissSound)
                {
                    SoundEngine.PlaySound(SoundID.Item30, npc.position);
                }
                
                // 根据配置生成闪避粒子效果
                if (enableMissParticles)
                {
                    for (int d = 0; d < 10; d++)
                    {
                        Vector2 dustVel = new Vector2(
                            Main.rand.NextFloat(-3f, 3f),
                            Main.rand.NextFloat(-3f, 3f)
                        );
                        Dust.NewDust(
                            npc.position + new Vector2(Main.rand.Next(npc.width), Main.rand.Next(npc.height)), 
                            0, 0, 
                            DustID.MagicMirror, 
                            dustVel.X, dustVel.Y, 
                            150, default(Color), 1.2f
                        );
                    }
                }
            }
        }
        
        // 如果仍有伤害漏过，这可以作为最后的防线
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            // 检查是否是已被闪避的弹幕
            if (MissTracker.NPCsMissed[npc.whoAmI] || (!projectile.active && damageDone <= 1))
            {
                // 恢复NPC生命值（抵消最小伤害）
                if (damageDone > 0)
                {
                    npc.life += damageDone;
                    if (npc.life > npc.lifeMax)
                        npc.life = npc.lifeMax;
                    
                    // 防止伤害数字显示
                    for(int i = 0; i < Main.maxCombatText; i++)
                    {
                        if (Main.combatText[i].active && 
                            Vector2.Distance(Main.combatText[i].position, npc.Center) < 100f &&
                            Main.combatText[i].color != Color.LightGray) // 不清除我们的MISS文本
                        {
                            Main.combatText[i].active = false;
                        }
                    }
                }
            }
        }
        
        // 同理处理物品伤害
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            // 检查是否是闪避的物品伤害
            if (MissTracker.NPCsMissed[npc.whoAmI] || (damageDone <= 1 && item.DamageType != DamageClass.Melee))
            {
                // 恢复NPC生命值（抵消最小伤害）
                if (damageDone > 0)
                {
                    npc.life += damageDone;
                    if (npc.life > npc.lifeMax)
                        npc.life = npc.lifeMax;
                    
                    // 防止伤害数字显示
                    for(int i = 0; i < Main.maxCombatText; i++)
                    {
                        if (Main.combatText[i].active && 
                            Vector2.Distance(Main.combatText[i].position, npc.Center) < 100f &&
                            Main.combatText[i].color != Color.LightGray) // 不清除我们的MISS文本
                        {
                            Main.combatText[i].active = false;
                        }
                    }
                }
            }
        }
    }
} 