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
using smartDodgeAI.Content.Systems;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.Chat;

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
        private enum DodgeType { Teleport, Roll, Leap, Flash, TimeDilation, ShadowClone, BurrowOrInvisibility, Invisibility, MagneticWave, InkSplash, TimeRewind, Shrink }
        private enum DodgeLeapPhase { Inactive, Rising, Hovering, Lunging }
        private enum DodgeBurrowPhase { Inactive, Diving, Hidden, Emerging }

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

        // --- 翻滚闪避相关字段 ---
        private int _dodgeRollTimer = 0;
        private Vector2 _dodgeRollStartPosition = Vector2.Zero;
        private Vector2 _dodgeRollTargetPosition = Vector2.Zero;
        private float _initialRotation = 0f;
        private Vector2 _savedVelocity = Vector2.Zero;

        // --- 跳跃空翻相关字段 ---
        private DodgeLeapPhase _leapPhase = DodgeLeapPhase.Inactive;
        private int _leapTimer = 0;
        private Vector2 _leapStartPosition;
        private Vector2 _leapApexPosition;

        // --- 闪现步相关字段 ---
        private int _flashStepTimer = 0;
        private Vector2 _flashStepStartPosition = Vector2.Zero;
        private Vector2 _flashStepTargetPosition = Vector2.Zero;
        private Vector2 _flashStepDirection = Vector2.Zero;
        private float _flashStepDistance = 0f;
        private bool _flashStepGhostActive = false;
        private List<Vector2> _flashStepPositionHistory = new List<Vector2>();
        private Vector2 _savedVelocityFlash = Vector2.Zero;
        private float _initialRotationFlash = 0f;

        // --- 时间膨胀相关字段 ---
        private int _timeDilationTimer = 0;
        private float _timeDilationRadius = 0f;
        private Dictionary<int, Vector2> _affectedProjectileCache = new Dictionary<int, Vector2>();
        private float _originalFrameSpeed = 0f;
        private int _originalFrameCounter = 0;
        private bool _isTimeDilationActive = false;
        private Vector2 _originalVelocity = Vector2.Zero; // 存储原始速度
        private Vector2 _timeDilationPosition = Vector2.Zero; // 存储时间膨胀开始时的位置

        // --- 影分身相关字段 ---
        private bool _hasShadowClones = false;

        // --- 飞天遁地相关字段 ---
        private DodgeBurrowPhase _burrowPhase = DodgeBurrowPhase.Inactive;
        private int _burrowTimer = 0;
        private Vector2 _burrowTargetPosition = Vector2.Zero;
        private bool _isBurrowingOrInvisible = false;

        // --- 隐身闪避相关字段 ---
        private bool _isInvisible = false;
        private int _invisibilityTimer = 0;
        private int _invisibilityInvincibilityTimer = 0;

        // --- 磁力波相关字段 ---
        private int _magneticWaveTimer = 0;
        private float _magneticWaveMaxRadius = 0f;
        private float _magneticWaveCurrentRadius = 0f;
        private bool _isMagneticWaveActive = false;
        private HashSet<int> _deflectedProjectiles = new HashSet<int>();

        // --- 墨水喷射相关字段 ---
        private bool _isInkSplashActive = false;
        private int _inkSplashTimer = 0;
        private int _inkSplashInterval = 0;
        private int _inkSplashCount = 0;
        private Vector2 _inkSplashDirection = Vector2.Zero;
        private int _inkSplashTargetPlayerId = -1;

        // --- 时间回溯相关字段 ---
        private struct NPCState
        {
            public Vector2 Position;
            public int Health;
        }
        private readonly Queue<NPCState> _history = new Queue<NPCState>();
        private int _timeRewindTimer;
        private const int TimeRewindDuration = 15; // 15帧回溯时间
        private Vector2 _timeRewindStartPosition;
        private Vector2 _timeRewindTargetPosition;
        private int _timeRewindTargetHealth;
        private int _preRewindHealth;
        private int _damageReductionTimer;
        private readonly List<Vector2> _timeRewindAfterimageHistory = new List<Vector2>();

        // --- 缩小闪避相关字段 ---
        private bool _isShrinking = false;
        private int _shrinkDodgeTimer = 0;
        private float _originalScale = 1f;

        // --- 本地AI索引 ---
        private const int DODGE_TIMER = 0;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return false;
            
            // 排除动物类型的NPC
            if (entity.CountsAsACritter || entity.friendly)
                return false;

            // 愚人节彩蛋：骷髅专用模式
            if (config.EnableSkeletonOnlyDodge && (SmartDodgeConfig.IsAprilFoolsDay() || AprilFoolsTestSystem.IsTestMode))
            {
                // 在骷髅专用模式下，只对骷髅类型NPC应用闪避
                return lateInstantiation && SkeletonUtils.IsSkeleton(entity);
            }
                
            if (entity.boss)
                return lateInstantiation && config.EnableBossDodge;
            else
                return lateInstantiation && config.EnableNormalEnemyDodge && !entity.townNPC;
        }

        public override bool PreAI(NPC npc)
        {
            // 每帧默认恢复受击状态，避免闪避结束后仍保持无敌
            if (!_isInvisible) // 如果不在隐身状态，则每帧重置
            {
                npc.dontTakeDamage = false;
            }

            // 如果处于隐身状态，AI仍然运行，但我们需要处理伤害状态
            if (_isInvisible)
            {
                if (_invisibilityInvincibilityTimer > 0)
                {
                    npc.dontTakeDamage = true; // 1秒无敌
                }
                else
                {
                    npc.dontTakeDamage = false; // 无敌结束但仍隐身
                }
                // AI会继续运行，所以这里不返回
            }

            if (_dodgeRollTimer > 0 || _leapPhase != DodgeLeapPhase.Inactive || _flashStepTimer > 0 || _isBurrowingOrInvisible || _isMagneticWaveActive)
            {
                npc.dontTakeDamage = true; // 闪避时无敌
                return false; // 阻止原版AI运行
            }
            
            if (_timeRewindTimer > 0)
            {
                npc.dontTakeDamage = true; // 回溯移动时无敌
                return false; // 阻止原版AI运行
            }
            
            // 时间膨胀状态下允许AI运行，但保持无敌
            if (_isTimeDilationActive)
            {
                npc.dontTakeDamage = true;
                // 返回true允许AI运行
            }

            // 新增：缩小状态下允许AI运行，但保持无敌
            if (_isShrinking)
            {
                // npc.dontTakeDamage = true; // 根据新需求，移除无敌
            }

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
            // --- 状态记录 ---
            // 记录历史状态，用于时间回溯
            if (_history.Count >= 45)
            {
                _history.Dequeue();
            }
            _history.Enqueue(new NPCState { Position = npc.position, Health = npc.life });

            // 递减伤害减免计时器
            if (_damageReductionTimer > 0)
            {
                _damageReductionTimer--;
            }

            // --- 闪避逻辑 ---
            if (_isShrinking)
            {
                _shrinkDodgeTimer--;
                if (_shrinkDodgeTimer <= 0)
                {
                    RestoreShrinkState(npc);
                }
            }
            else if (_timeRewindTimer > 0)
            {
                _timeRewindTimer--;
                
                // 使用插值实现平滑移动
                float progress = 1f - (_timeRewindTimer / (float)TimeRewindDuration);
                npc.position = Vector2.Lerp(_timeRewindStartPosition, _timeRewindTargetPosition, progress);
                
                // 记录残影位置
                if (_timeRewindTimer % 2 == 0)
                {
                    _timeRewindAfterimageHistory.Add(npc.Center);
                }
                if (_timeRewindAfterimageHistory.Count > 12)
                {
                    _timeRewindAfterimageHistory.RemoveAt(0);
                }

                if (_timeRewindTimer == 0)
                {
                    // 回溯结束
                    npc.position = _timeRewindTargetPosition;
                    npc.life = _timeRewindTargetHealth;
                    if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
                    
                    // 开启伤害减免
                    _damageReductionTimer = 20;

                    // 显示治疗文本
                    int restoredHealth = npc.life - _preRewindHealth;
                    if (restoredHealth > 0 && Main.netMode != NetmodeID.Server)
                    {
                        CombatText.NewText(npc.getRect(), Color.Green, $"+{restoredHealth}", true);
                    }
                    
                    _timeRewindAfterimageHistory.Clear();
                }
                return;
            }
            else if (_isTimeDilationActive && _timeDilationTimer > 0)
            {
                // 减少计时器
                _timeDilationTimer--;
                
                // 移除位置固定和速度清零，允许AI正常运行
                // npc.position = _timeDilationPosition;
                // npc.velocity = Vector2.Zero;
                
                // 加速NPC自身的帧动画播放(双速)
                npc.frameCounter += 1;
                
                // 处理周围弹幕减速
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.hostile == false) // 只影响玩家的弹幕
                    {
                        float distance = Vector2.Distance(projectile.Center, npc.Center);
                        
                        // 检查弹幕是否在影响范围内
                        if (distance <= _timeDilationRadius)
                        {
                            // 缓存弹幕原速度
                            if (!_affectedProjectileCache.ContainsKey(i))
                            {
                                _affectedProjectileCache[i] = projectile.velocity;
                            }
                            
                            // 减缓弹幕速度至极慢
                            projectile.velocity *= 0.05f; // 减至原速度的5%
                        }
                        // 如果弹幕已离开范围但在缓存中，恢复速度
                        else if (_affectedProjectileCache.ContainsKey(i))
                        {
                            projectile.velocity = _affectedProjectileCache[i];
                            _affectedProjectileCache.Remove(i);
                        }
                    }
                }
                
                // 在效果期间，创建粒子效果
                if (enableMissParticles && Main.rand.NextBool(3)) // 控制生成频率
                {
                    Vector2 dustPos = npc.Center + Main.rand.NextVector2CircularEdge(_timeDilationRadius, _timeDilationRadius);
                    int dustType = Main.rand.Next(3) switch
                    {
                        0 => DustID.GreenTorch,
                        1 => DustID.BlueTorch,
                        _ => DustID.PurpleTorch
                    };
                        
                    Dust dust = Dust.NewDustDirect(dustPos, 0, 0, dustType);
                    dust.velocity = (npc.Center - dustPos) * 0.03f; // 向中心缓慢移动
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    dust.fadeIn = Main.rand.NextFloat(0.5f, 1f);
                }
                
                if (_timeDilationTimer == 0)
                {
                    // 时间膨胀效果结束
                    _isTimeDilationActive = false;
                    
                    // 将受影响弹幕反弹回它们的所有者，并使其能够伤害玩家
                    foreach (var pair in _affectedProjectileCache)
                    {
                        int projIndex = pair.Key;
                        if (Main.projectile[projIndex].active)
                        {
                            Projectile projectile = Main.projectile[projIndex];
                            
                            // 保存原始伤害值用于计算
                            int originalDamage = projectile.damage;
                            
                            // 修改弹幕属性，使其能够伤害玩家而不是NPC
                            projectile.hostile = true; // 对玩家造成伤害
                            projectile.friendly = false; // 不对NPC造成伤害
                            projectile.damage = (int)(originalDamage * 0.1f); // 伤害削减90%
                            
                            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                            {
                                Player owner = Main.player[projectile.owner];
                                if (owner.active && !owner.dead)
                                {
                                    // 计算朝向玩家的方向
                                    Vector2 reverseDirection = (owner.Center - projectile.Center).SafeNormalize(Vector2.Zero);
                                    // 使用原始速度大小，但方向朝向玩家
                                    float originalSpeed = pair.Value.Length();
                                    projectile.velocity = reverseDirection * originalSpeed;
                                    
                                    // 在弹幕周围生成反弹特效
                                    if (enableMissParticles)
                                    {
                                        for (int i = 0; i < 5; i++)
                                        {
                                            Dust dust = Dust.NewDustDirect(
                                                projectile.position, 
                                                projectile.width, 
                                                projectile.height, 
                                                DustID.PinkTorch,
                                                reverseDirection.X * 2, 
                                                reverseDirection.Y * 2, 
                                                100, 
                                                default, 
                                                1.2f
                                            );
                                            dust.noGravity = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // 如果所有者无效，随机方向反弹
                                    projectile.velocity = pair.Value.RotatedByRandom(MathHelper.Pi) * -1f;
                                }
                            }
                            else
                            {
                                // 如果没有有效所有者，随机方向反弹
                                projectile.velocity = pair.Value.RotatedByRandom(MathHelper.Pi) * -1f;
                            }
                        }
                    }
                    _affectedProjectileCache.Clear();
                    
                    // 恢复NPC的帧动画速度，不再恢复速度（让AI继续正常运行）
                    npc.frameCounter = _originalFrameCounter;
                    // npc.velocity = _originalVelocity; // 移除恢复速度的代码
                    
                    // 根据配置生成结束特效
                    if (enableMissParticles)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 dustPos = npc.Center + Main.rand.NextVector2CircularEdge(_timeDilationRadius, _timeDilationRadius);
                            Vector2 dustVel = (dustPos - npc.Center) * 0.1f;
                            int dustType = Main.rand.Next(3) switch
                            {
                                0 => DustID.GreenTorch,
                                1 => DustID.PinkTorch,
                                _ => DustID.PurpleTorch
                            };
                            
                            Dust dust = Dust.NewDustDirect(dustPos, 0, 0, dustType);
                            dust.velocity = dustVel;
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(1f, 1.8f);
                        }
                    }
                    
                    if (enableMissSound)
                    {
                        SoundEngine.PlaySound(SoundID.Item27, npc.Center); // 时间扭曲音效
                    }
                }
                
                // 时间膨胀效果激活时，不执行后续的瞬移延迟逻辑
                return;
            }
            
            if (_isMagneticWaveActive && _magneticWaveTimer > 0)
            {
                _magneticWaveTimer--;

                // 磁力波从0扩展到最大半径
                float progress = 1f - (_magneticWaveTimer / 60f); // 假设持续时间为60帧 (1秒)
                _magneticWaveCurrentRadius = _magneticWaveMaxRadius * progress;

                // 生成边缘粒子效果
                if (enableMissParticles)
                {
                    // 每帧在环上生成50-90个粒子
                    int particleCount = Main.rand.Next(50, 90);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(_magneticWaveCurrentRadius, _magneticWaveCurrentRadius);
                        Vector2 dustPos = npc.Center + offset;
                        
                        Dust dust = Dust.NewDustDirect(dustPos, 0, 0, DustID.Electric, 0f, 0f, 100, default, 1.5f);
                        dust.noGravity = true;
                        dust.velocity = offset.SafeNormalize(Vector2.Zero) * 2f; // 加快向外移动速度
                    }
                }

                // 检查并偏转弹幕
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    // 只影响敌对弹幕，且未被偏转过
                    if (projectile.active && projectile.hostile == false && !_deflectedProjectiles.Contains(i))
                    {
                        float distance = Vector2.Distance(projectile.Center, npc.Center);
                        // 检查弹幕是否在磁力波的环上（给予一定厚度）
                        if (Math.Abs(distance - _magneticWaveCurrentRadius) < projectile.width + 10) // 10是波的厚度
                        {
                            // 计算切线方向
                            Vector2 vectorToProjectile = projectile.Center - npc.Center;
                            if (vectorToProjectile == Vector2.Zero) continue; // 避免除零

                            // 随机选择一个切线方向
                            Vector2 tangent = Main.rand.NextBool() 
                                ? new Vector2(-vectorToProjectile.Y, vectorToProjectile.X)
                                : new Vector2(vectorToProjectile.Y, -vectorToProjectile.X);
                            
                            tangent.Normalize();
                            
                            // 保持弹幕原有速度
                            projectile.velocity = tangent * projectile.velocity.Length();
                            
                            // 标记为已偏转
                            _deflectedProjectiles.Add(i);
                            
                            // 偏转时产生粒子效果
                            if (enableMissParticles)
                            {
                                for (int d = 0; d < 5; d++)
                                {
                                    Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Electric, 0, 0, 100, default, 1.2f);
                                }
                            }
                        }
                    }
                }

                if (_magneticWaveTimer == 0)
                {
                    // 效果结束
                    _isMagneticWaveActive = false;
                    _deflectedProjectiles.Clear();

                    if (enableMissSound)
                    {
                        SoundEngine.PlaySound(SoundID.Item94, npc.Center); // 磁力音效
                    }
                }
                return; // 磁力波激活时，不执行后续逻辑
            }
            
            if (_isBurrowingOrInvisible)
            {
                npc.velocity = Vector2.Zero; // 遁地/隐身期间不移动
                _burrowTimer--;

                switch (_burrowPhase)
                {
                    case DodgeBurrowPhase.Diving:
                        // 30帧内完成下潜/隐身
                        npc.alpha = (int)(255 * (1 - (_burrowTimer - 30) / 30f));
                        // 创建尘埃效果
                        if (enableMissParticles && Main.rand.NextBool(3))
                        {
                            if (!npc.noGravity && !npc.wet) // 仅地面单位
                            {
                                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Dirt, 0, -1f);
                            }
                            else // 飞行或水中单位的隐身效果
                            {
                                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Vortex, 0, 0, 150, default, 1.2f);
                            }
                        }

                        if (_burrowTimer <= 30)
                        {
                            _burrowPhase = DodgeBurrowPhase.Hidden;
                            npc.alpha = 255;
                        }
                        break;
                    
                    case DodgeBurrowPhase.Hidden:
                        // 在隐藏状态下等待30帧
                        if (_burrowTimer <= 0)
                        {
                            npc.Center = _burrowTargetPosition;
                            _burrowPhase = DodgeBurrowPhase.Emerging;
                            _burrowTimer = 30; // 30帧出现时间

                            // 出现时的音效和粒子
                            if (enableMissSound)
                            {
                                SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                            }
                            if (enableMissParticles)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    int dustID = (!npc.noGravity && !npc.wet) ? DustID.Dirt : DustID.Vortex;
                                    Dust.NewDust(npc.position, npc.width, npc.height, dustID, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3));
                                }
                            }
                        }
                        break;

                    case DodgeBurrowPhase.Emerging:
                        // 30帧内完成出现
                        npc.alpha = (int)(255 * (_burrowTimer / 30f));
                        if (_burrowTimer <= 0)
                        {
                            ResetBurrowState(npc);
                        }
                        break;
                }
                return;
            }

            if (_flashStepTimer > 0)
            {
                // 记录当前位置用于残影，减少记录频率和数量
                if (_flashStepTimer % 2 == 0) // 每2帧记录一次，而不是每帧多次
                {
                    // 只添加当前位置，不再添加多个随机偏移位置
                    _flashStepPositionHistory.Add(npc.Center);
                }
                
                // 减少历史记录上限
                while (_flashStepPositionHistory.Count > 12)
                {
                    _flashStepPositionHistory.RemoveAt(0);
                }

                _flashStepTimer--;
                
                // 使用插值实现平滑移动
                float progress = 1f - (_flashStepTimer / (float)(2 * _flashStepDistance));
                npc.position = Vector2.Lerp(_flashStepStartPosition, _flashStepTargetPosition, progress);
                
                // 在移动过程中生成粒子效果，受配置控制
                if (enableMissParticles && Main.rand.NextBool(2)) // 控制生成频率
                {
                    Color dustColor = Main.rand.NextBool() ? new Color(100, 100, 255) : new Color(180, 100, 255);
                    
                    // 在NPC周围随机位置生成粒子
                    Vector2 dustPos = npc.Center + Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f);
                    int dustType = Main.rand.NextBool() ? DustID.BlueTorch : DustID.PurpleTorch;
                    
                    // 创建粒子
                    Dust dust = Dust.NewDustDirect(dustPos, 0, 0, dustType);
                    dust.velocity = _flashStepDirection * -2f;
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(1f, 1.5f);
                    dust.fadeIn = Main.rand.NextFloat(0.5f, 1f);
                }
                
                // 启用鬼魂碰撞（可以穿过玩家但仍受地形阻碍）
                // 修改NPC的碰撞检测
                if (_flashStepGhostActive)
                {
                    // 禁用与玩家的碰撞
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (player.active && !player.dead && player.Hitbox.Intersects(npc.Hitbox))
                        {
                            // 移动玩家位置避免碰撞
                            Vector2 moveDir = player.Center - npc.Center;
                            if (moveDir != Vector2.Zero)
                            {
                                moveDir.Normalize();
                                moveDir *= -0.1f; // 微小偏移，避免碰撞检测
                                player.position += moveDir;
                            }
                        }
                    }
                }
                
                if (_flashStepTimer == 0)
                {
                    // 闪现步结束
                    npc.position = _flashStepTargetPosition; // 确保最终位置精确
                    npc.velocity = Vector2.Zero; // 清零速度
                    
                    // 恢复原始数据
                    if (_savedVelocityFlash != Vector2.Zero)
                    {
                        npc.velocity = _savedVelocityFlash; // 恢复原速度
                        _savedVelocityFlash = Vector2.Zero;
                    }
                    npc.rotation = _initialRotationFlash; // 恢复原始角度
                    npc.dontTakeDamage = false; // 恢复可受击状态
                    
                    // 关闭鬼魂碰撞，恢复原始碰撞
                    _flashStepGhostActive = false;
                    
                    // 清空位置历史
                    _flashStepPositionHistory.Clear();
                    
                    // 生成结束时的粒子效果，受配置控制
                    if (enableMissParticles)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 dustPos = npc.Center + Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f);
                            Vector2 dustVel = Main.rand.NextVector2Circular(3f, 3f);
                            int dustType = Main.rand.Next(3) switch
                            {
                                0 => DustID.BlueTorch,
                                1 => DustID.PurpleTorch,
                                _ => DustID.WhiteTorch
                            };
                            
                            Dust dust = Dust.NewDustDirect(dustPos, 0, 0, dustType);
                            dust.velocity = dustVel;
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(1f, 2f);
                        }
                    }
                }
                return; // 闪现步时，不执行后续的瞬移延迟逻辑
            }
            
            if (_dodgeRollTimer > 0)
            {
                _dodgeRollTimer--;
                
                // 使用插值实现平滑移动
                float progress = 1f - (_dodgeRollTimer / 15f);
                npc.position = Vector2.Lerp(_dodgeRollStartPosition, _dodgeRollTargetPosition, progress);
                
                // 翻滚动画（旋转）
                int direction = (npc.Center.X < Main.player[npc.target].Center.X) ? 1 : -1;
                npc.rotation += MathHelper.ToRadians(360f / 15f) * direction;

                if (_dodgeRollTimer == 0)
                {
                    // 翻滚结束
                    npc.position = _dodgeRollTargetPosition; // 确保最终位置精确
                    npc.velocity = Vector2.Zero; // 清零速度
                    if (_savedVelocity != Vector2.Zero)
                    {
                        npc.velocity = _savedVelocity; // 恢复原速度
                        _savedVelocity = Vector2.Zero;
                    }
                    npc.rotation = _initialRotation; // 恢复原始角度
                    npc.dontTakeDamage = false; // 恢复可受击状态
                }
                return; // 翻滚时，不执行后续的瞬移延迟逻辑
            }

            if (_leapPhase != DodgeLeapPhase.Inactive)
            {
                _leapTimer--;
                switch (_leapPhase)
                {
                    case DodgeLeapPhase.Rising:
                        // 线性插值上升到顶点
                        float progress = 1f - (_leapTimer / 30f); // 30帧上升时间
                        npc.Center = Vector2.Lerp(_leapStartPosition, _leapApexPosition, progress);
                        npc.velocity = Vector2.Zero;
                        npc.rotation += MathHelper.ToRadians(12f) * npc.direction; // 上升时旋转

                        if (_leapTimer <= 0)
                        {
                            _leapPhase = DodgeLeapPhase.Hovering;
                            _leapTimer = Main.rand.Next(6, 91); // 随机悬停0.1-1.5秒
                            if (Main.netMode != NetmodeID.Server) Main.NewText("触发了跳跃闪避（悬停）", Color.Yellow);
                        }
                        break;

                    case DodgeLeapPhase.Hovering:
                        if (_leapTimer <= 0)
                        {
                            _leapPhase = DodgeLeapPhase.Lunging;
                            _leapTimer = 30; // 冲刺0.5秒
                            Player player = Main.player[npc.target];
                            if (player.active && !player.dead)
                            {
                                Vector2 lungeDir = (player.Center - npc.Center).SafeNormalize(Vector2.Zero);
                                npc.velocity = lungeDir * 15f; // 高速冲刺
                                if (Main.netMode != NetmodeID.Server) Main.NewText("触发了跳跃闪避（冲刺）", Color.Orange);
                            }
                            else
                            {
                                _leapPhase = DodgeLeapPhase.Inactive; // 目标丢失，直接结束
                            }
                        }
                        else
                        {
                            npc.velocity = Vector2.Zero; // 保持悬停
                            // 可以添加轻微的上下浮动效果
                            npc.Center = _leapApexPosition + new Vector2(0, (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 4f);
                        }
                        break;
                     
                    case DodgeLeapPhase.Lunging:
                        // 速度已设定，让NPC自行移动
                        if (_leapTimer <= 0)
                        {
                            _leapPhase = DodgeLeapPhase.Inactive;
                            npc.rotation = _initialRotation;
                            if (_savedVelocity != Vector2.Zero)
                            {
                                npc.velocity = _savedVelocity;
                                _savedVelocity = Vector2.Zero;
                            }
                            else
                            {
                                npc.velocity *= 0.2f; // 减速
                            }
                        }
                        break;
                }
                return;
            }

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
            
            // 检查是否有影分身存在
            if (_hasShadowClones)
            {
                // 检查是否有任何活跃的分身
                bool anyActive = ShadowCloneManager.CloneSources.TryGetValue(npc.whoAmI, out var cloneList) && cloneList.Count > 0;
                
                // 如果没有活跃分身，重置状态
                if (!anyActive)
                {
                    _hasShadowClones = false;
                }
            }

            // --- 隐身逻辑更新 ---
            if (_isInvisible)
            {
                // 计时器递减
                if (_invisibilityInvincibilityTimer > 0)
                {
                    _invisibilityInvincibilityTimer--;
                }
                
                _invisibilityTimer--;
                
                // 隐身效果结束
                if (_invisibilityTimer <= 0)
                {
                    _isInvisible = false;
                    // 恢复原始缩放比例
                    npc.scale = _originalScale;
                    
                    // 可选：添加重新出现的粒子效果
                    if (enableMissParticles)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 150, default, 1.1f);
                        }
                    }
                }
            }
            
            // 处理墨水喷射逻辑
            if (_isInkSplashActive && _inkSplashTimer > 0)
            {
                // 减少计时器
                _inkSplashTimer--;
                
                // 检查是否应该喷射墨水
                int maxInkSplash = 10;
                var modPlayer = Main.player[Main.myPlayer].GetModPlayer<DodgePlayer>();
                if (modPlayer != null)
                {
                    if (modPlayer.ForceInkSplashTo1)
                        maxInkSplash = 1;
                    else
                        maxInkSplash = Math.Max(1, 10 - modPlayer.InkSplashReduction);
                }
                if (_inkSplashTimer % _inkSplashInterval == 0 && _inkSplashCount < maxInkSplash)
                {
                    // 确保目标玩家有效
                    if (_inkSplashTargetPlayerId >= 0 && _inkSplashTargetPlayerId < Main.maxPlayers)
                    {
                        Player targetPlayer = Main.player[_inkSplashTargetPlayerId];
                        if (targetPlayer.active && !targetPlayer.dead)
                        {
                            // 更新朝向玩家的方向
                            _inkSplashDirection = (targetPlayer.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                            
                            // 喷射5个墨水弹幕
                            int inkCount = 1;
                            for (int i = 0; i < inkCount; i++)
                            {
                                // 计算散射角度（15度范围内）
                                float spreadAngle = Main.rand.NextFloat(-0.25f, 0.25f);
                                Vector2 shootVel = _inkSplashDirection.RotatedBy(spreadAngle) * Main.rand.NextFloat(6f, 10f);
                                
                                // 生成墨水弹幕
                                int projIndex = Projectile.NewProjectile(
                                    npc.GetSource_FromAI(),
                                    npc.Center,
                                    shootVel,
                                    ModContent.ProjectileType<Projectiles.InkProjectile>(),
                                    1, // 伤害设为1以确保命中注册
                                    0f,
                                    Main.myPlayer // 所有者设为Main.myPlayer
                                );
                                
                                if (projIndex >= 0 && projIndex < Main.maxProjectiles)
                                {
                                    // 可以设置弹幕的额外属性
                                    Main.projectile[projIndex].timeLeft = 180; // 3秒存活时间
                                    Main.projectile[projIndex].scale = Main.rand.NextFloat(0.8f, 1.2f);
                                }
                            }
                            
                            // 生成额外的墨水喷射特效
                            if (enableMissParticles)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    Vector2 dustVel = _inkSplashDirection * Main.rand.NextFloat(1f, 5f) + 
                                                      Main.rand.NextVector2Circular(1f, 1f);
                                    
                                    Dust dust = Dust.NewDustDirect(
                                        npc.Center,
                                        0, 0,
                                        DustID.Shadowflame,
                                        dustVel.X, dustVel.Y,
                                        100, new Color(20, 20, 40), 1f
                                    );
                                    dust.noGravity = true;
                                }
                            }
                            
                            // 播放喷射音效
                            if (enableMissSound && Main.rand.NextBool(2)) // 50%几率播放音效，避免过于频繁
                            {
                                SoundEngine.PlaySound(SoundID.Drip, npc.Center);
                            }
                        }
                    }
                    
                    _inkSplashCount++;
                }
                
                // 检查效果是否结束
                if (_inkSplashTimer == 0)
                {
                    _isInkSplashActive = false;
                    
                    // 播放结束音效
                    if (enableMissSound)
                    {
                        SoundEngine.PlaySound(SoundID.Splash, npc.Center);
                    }
                }
                
                return; // 墨水喷射期间，不执行其他逻辑
            }

            base.PostAI(npc);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 不再完全隐藏隐身的NPC，而是让它们以1%的尺寸显示
            // 隐身NPC现在通过缩放实现，所以总是返回true允许绘制
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 隐藏或隐身阶段不绘制任何额外效果
            if (_burrowPhase == DodgeBurrowPhase.Hidden)
            {
                return;
            }

            // 绘制时间膨胀视觉效果
            if (_isTimeDilationActive && _timeDilationTimer > 0)
            {
                // 计算影响圈范围
                float radius = _timeDilationRadius;
                
                // 在屏幕上显示时间膨胀的影响范围
                Texture2D ringTexture = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
                Vector2 drawCenter = npc.Center - screenPos;
                
                // 确定影响圈的颜色
                float alpha = (float)_timeDilationTimer / 60f; // 根据剩余时间淡出
                Color ringColor = new Color(180, 120, 255, 50) * (0.3f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.1f)) * alpha;
                
                // 绘制时间膨胀圈
                spriteBatch.Draw(
                    ringTexture,
                    drawCenter,
                    null,
                    ringColor,
                    Main.GameUpdateCount * 0.01f, // 缓慢旋转
                    new Vector2(ringTexture.Width / 2, ringTexture.Height / 2),
                    new Vector2(radius / 120f), // 使用更小的缩放比例
                    SpriteEffects.None,
                    0f
                );
                
                // 绘制内圈
                spriteBatch.Draw(
                    ringTexture,
                    drawCenter,
                    null,
                    new Color(120, 80, 200, 30) * alpha,
                    -Main.GameUpdateCount * 0.02f, // 反向旋转
                    new Vector2(ringTexture.Width / 2, ringTexture.Height / 2),
                    new Vector2(radius / 180f), // 使用更小的缩放比例
                    SpriteEffects.None,
                    0f
                );
            }
            
            // 绘制磁力波效果
            if (_isMagneticWaveActive && _magneticWaveTimer > 0)
            {
                Texture2D texture = TextureAssets.Extra[98].Value; // 一个环形纹理
                Vector2 drawCenter = npc.Center - screenPos;
                float progress = 1f - (_magneticWaveTimer / 60f);
                
                // --- 增强的视觉效果 ---
                
                // 外层辉光环
                float scaleOuter = (_magneticWaveCurrentRadius / (texture.Width / 2f)) * 1.1f;
                float alphaOuter = (1f - progress) * 0.7f;
                Color colorOuter = Color.Cyan * alphaOuter;
                spriteBatch.Draw(texture, drawCenter, null, colorOuter, Main.GameUpdateCount * -0.02f, texture.Size() / 2, scaleOuter, SpriteEffects.None, 0f);

                // 内层实心环
                float scaleInner = _magneticWaveCurrentRadius / (texture.Width / 2f);
                float alphaInner = 1f - progress;
                Color colorInner = Color.White * alphaInner;
                spriteBatch.Draw(texture, drawCenter, null, colorInner, Main.GameUpdateCount * 0.03f, texture.Size() / 2, scaleInner, SpriteEffects.None, 0f);
            }
            
            // 绘制闪现步残影
            if (_flashStepTimer > 0 && _flashStepPositionHistory.Count > 1)
            {
                // 获取NPC的纹理和帧信息
                Texture2D texture = TextureAssets.Npc[npc.type].Value;
                Rectangle frame = npc.frame;
                Vector2 origin = frame.Size() / 2f;
                float scale = npc.scale;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                
                // 绘制历史位置的残影
                for (int i = 0; i < _flashStepPositionHistory.Count - 1; i++)
                {
                    // 计算不透明度（保证有足够可见度但不刺眼）
                    float alpha = 0.15f + 0.4f * (float)i / _flashStepPositionHistory.Count;
                    
                    // 绘制位置（从世界坐标转为屏幕坐标）
                    Vector2 drawPos = _flashStepPositionHistory[i] - screenPos;
                    
                    // 更柔和的残影颜色 - 淡青色到淡紫色过渡
                    Color afterimageColor;
                    if (i < _flashStepPositionHistory.Count / 2)
                    {
                        // 较旧的残影，淡青色
                        afterimageColor = new Color(100, 180, 220, 90) * alpha;
                    }
                    else
                    {
                        // 较新的残影，淡紫色
                        float gradientFactor = (float)(i - _flashStepPositionHistory.Count / 2) / (_flashStepPositionHistory.Count / 2);
                        int r = (int)(100 + gradientFactor * 80);
                        int g = (int)(150 + gradientFactor * 10);
                        int b = (int)(220 - gradientFactor * 20);
                        afterimageColor = new Color(r, g, b, 90) * alpha;
                    }
                    
                    // 绘制残影主体，简化绘制不再使用边框效果
                    spriteBatch.Draw(
                        texture, 
                        drawPos, 
                        frame, 
                        afterimageColor, 
                        npc.rotation, 
                        origin, 
                        scale * (0.95f - 0.05f * i / _flashStepPositionHistory.Count), // 较新的残影更大
                        effects, 
                        0f
                    );
                }
            }
        
            // 原有的瞬移目标位置绘制代码
            if (_dodgeRollTimer > 0 || _leapPhase != DodgeLeapPhase.Inactive) return; // 翻滚或跳跃时，不绘制瞬移幻影
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

            // 绘制墨水喷射效果
            if (_isInkSplashActive && _inkSplashTimer > 0)
            {
                // 计算喷射进度
                float progress = 1f - (_inkSplashTimer / 120f); // 0到1
                
                // 绘制从NPC身上喷出的墨水流
                Texture2D texture = TextureAssets.MagicPixel.Value;
                Vector2 center = npc.Center - screenPos;
                
                // 墨水流长度和宽度
                float length = 80f * (1f - progress * 0.5f); // 随时间略微减小
                float width = 20f * (1f - progress * 0.3f);  // 随时间略微减小
                
                // 喷射方向向量和垂直向量
                Vector2 direction = _inkSplashDirection;
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
                
                // 计算绘制四边形的四个顶点
                Vector2 topLeft = center + perpendicular * (width / 2);
                Vector2 topRight = center - perpendicular * (width / 2);
                Vector2 bottomLeft = center + direction * length + perpendicular * (width / 4);
                Vector2 bottomRight = center + direction * length - perpendicular * (width / 4);
                
                // 绘制墨水流四边形
                DrawInkQuad(spriteBatch, texture, topLeft, topRight, bottomLeft, bottomRight, new Color(10, 10, 30, 150));
                
                // 随机绘制一些墨水飞溅粒子
                if (Main.rand.NextBool(3))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float distance = Main.rand.NextFloat(10f, length);
                        float offset = Main.rand.NextFloat(-width / 2, width / 2);
                        
                        Vector2 position = center + direction * distance + perpendicular * offset;
                        float size = Main.rand.NextFloat(3f, 8f);
                        
                        Rectangle rect = new Rectangle(
                            (int)(position.X - size / 2),
                            (int)(position.Y - size / 2),
                            (int)size,
                            (int)size
                        );
                        
                        spriteBatch.Draw(
                            texture,
                            rect,
                            null,
                            new Color(10, 10, 30, 200),
                            Main.rand.NextFloat(MathHelper.TwoPi),
                            new Vector2(0.5f, 0.5f),
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            // 绘制时间回溯残影
            if (_timeRewindTimer > 0 && _timeRewindAfterimageHistory.Count > 1)
            {
                Texture2D texture = TextureAssets.Npc[npc.type].Value;
                Rectangle frame = npc.frame;
                Vector2 origin = frame.Size() / 2f;
                float scale = npc.scale;
                SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                for (int i = 0; i < _timeRewindAfterimageHistory.Count - 1; i++)
                {
                    float alpha = 0.15f + 0.4f * (float)i / _timeRewindAfterimageHistory.Count;
                    Vector2 drawPos = _timeRewindAfterimageHistory[i] - screenPos;
                    Color afterimageColor = new Color(120, 220, 200, 90) * alpha; // 青绿色

                    spriteBatch.Draw(
                        texture, 
                        drawPos, 
                        frame, 
                        afterimageColor, 
                        npc.rotation, 
                        origin, 
                        scale * (0.95f - 0.05f * i / _timeRewindAfterimageHistory.Count),
                        effects, 
                        0f
                    );
                }
            }
        }
        
        // 绘制墨水四边形的辅助方法
        private void DrawInkQuad(SpriteBatch spriteBatch, Texture2D texture, Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, Color color)
        {
            // 使用三角形绘制不规则四边形
            // 第一个三角形
            DrawInkTriangle(spriteBatch, texture, topLeft, topRight, bottomLeft, color);
            
            // 第二个三角形
            DrawInkTriangle(spriteBatch, texture, bottomLeft, topRight, bottomRight, color);
        }
        
        // 绘制三角形的辅助方法
        private void DrawInkTriangle(SpriteBatch spriteBatch, Texture2D texture, Vector2 point1, Vector2 point2, Vector2 point3, Color color)
        {
            // 计算三角形的中心
            Vector2 center = (point1 + point2 + point3) / 3f;
            
            // 绘制从中心到三个顶点的三条线段，形成一个简单三角形
            for (int i = 0; i < 30; i++)
            {
                // 在中心和各顶点之间随机采样点
                float t1 = Main.rand.NextFloat();
                float t2 = Main.rand.NextFloat();
                
                // 混合两个顶点得到三角形内的点
                Vector2 pos1 = Vector2.Lerp(center, point1, t1);
                Vector2 pos2 = Vector2.Lerp(center, point2, t1);
                Vector2 pos3 = Vector2.Lerp(center, point3, t1);
                
                // 从三个点中随机选一个
                Vector2 position = Main.rand.Next(3) switch
                {
                    0 => pos1,
                    1 => pos2,
                    _ => pos3
                };
                
                // 绘制一个小方块表示墨水粒子
                float size = Main.rand.NextFloat(1f, 3f);
                
                Rectangle rect = new Rectangle(
                    (int)(position.X - size / 2),
                    (int)(position.Y - size / 2),
                    (int)size,
                    (int)size
                );
                
                spriteBatch.Draw(
                    texture,
                    rect,
                    null,
                    color,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // 如果NPC是影分身，则不执行闪避逻辑
            if (ShadowCloneManager.IsClone(npc.whoAmI))
            {
                return;
            }
            
            // 时间回溯后的伤害减免
            if (_damageReductionTimer > 0)
            {
                modifiers.FinalDamage *= 0.5f;
            }

            // 如果NPC隐身，处理受击逻辑
            if (_isInvisible)
            {
                if (_invisibilityInvincibilityTimer > 0)
                {
                    // 在无敌期间，完全免疫伤害
                    modifiers.FinalDamage *= 0;
                    modifiers.SetMaxDamage(0);
                    modifiers.DisableCrit();
                    if (Main.netMode != NetmodeID.Server)
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Center.Y, 1, 1), Color.White, "Immune", true);
                    }
                }
                else
                {
                    // 无敌期结束，受击则解除隐身
                    _isInvisible = false;
                    _invisibilityTimer = 0;
                    // 恢复原始缩放比例
                    npc.scale = _originalScale;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Center.Y, 1, 1), Color.Orange, "Revealed!", true);
                    }
                    if (enableMissParticles)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 150, default, 1.1f);
                        }
                    }
                }
                return; // 结束此方法的执行，让本次攻击正常结算（或被免疫）
            }

            // 检查是否应用闪避逻辑
            bool canApply = (IsConsideredBoss(npc) && enableBossDodge) || (!IsConsideredBoss(npc) && !npc.townNPC && enableNormalEnemyDodge);
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
                                // --- 冷却检查 ---
                                double cooldownInTicks = config.TeleportCooldown * 60; // 将秒转换为ticks
                                if (_lastTeleportTime != -1 && Main.GameUpdateCount - _lastTeleportTime < cooldownInTicks)
                                {
                                    // 冷却中，不执行任何闪避
                                    return;
                                }

                                var possibleDodges = new List<DodgeType>();
                                bool isMultiSegment = npc.realLife != -1 && npc.realLife != npc.whoAmI;

                                if (!isMultiSegment)
                                {
                                    // 瞬移始终是备选方案（但不适用于多节肢体敌怪）
                                    // possibleDodges.Add(DodgeType.Teleport); // 注释掉：会造成位移
                                    
                                    // 对所有NPC（包括Boss）都可用的非干扰性闪避
                                    possibleDodges.Add(DodgeType.ShadowClone);
                                    possibleDodges.Add(DodgeType.InkSplash);
                                    
                                    // 缩小闪避和隐身闪避仅适用于非Boss单位
                                    if (!IsConsideredBoss(npc)) 
                                    {
                                        // if (!_isShrinking) // 注释掉：缩小闪避
                                        // {
                                        //     possibleDodges.Add(DodgeType.Shrink);
                                        // }
                                        // possibleDodges.Add(DodgeType.Invisibility); // 注释掉：隐身闪避
                                    }

                                    // 会干扰AI的闪避方式，仅限于非Boss单位
                                    if (!IsConsideredBoss(npc))
                                    {
                                        // possibleDodges.Add(DodgeType.Roll); // 注释掉：会造成位移且影响AI
                                        // possibleDodges.Add(DodgeType.Flash); // 注释掉：会造成位移且影响AI
                                        possibleDodges.Add(DodgeType.TimeDilation); // 保留：不影响AI
                                        // possibleDodges.Add(DodgeType.BurrowOrInvisibility); // 注释掉：会造成位移且影响AI
                                        // possibleDodges.Add(DodgeType.MagneticWave); // 注释掉：会影响AI
                                        
                                        // 时间回溯需要有足够的历史记录
                                        // if (_history.Count >= 45) // 注释掉：会造成位移且影响AI
                                        // {
                                        //     possibleDodges.Add(DodgeType.TimeRewind);
                                        // }

                                        // 跳跃只适用于非飞行/游泳的地面单位
                                        // if (!npc.noGravity && !npc.wet) // 注释掉：会造成位移且影响AI
                                        // {
                                        //     possibleDodges.Add(DodgeType.Leap);
                                        // }
                                    }
                                }

                                // 随机化闪避尝试顺序
                                var chosenDodge = possibleDodges[Main.rand.Next(possibleDodges.Count)];
                                
                                bool success = false;
                                switch(chosenDodge)
                                {
                                    case DodgeType.TimeDilation:
                                        success = TryStartTimeDilation(npc, projectile);
                                        break;
                                    case DodgeType.ShadowClone:
                                        success = TryStartShadowClones(npc, projectile);
                                        break;
                                    case DodgeType.InkSplash:
                                        success = TryStartInkSplash(npc, targetPlayer);
                                        break;
                                }

                                // 如果选择的闪避方式失败了（比如找不到位置），则退回至瞬移
                                if (!success && chosenDodge != DodgeType.Teleport)
                                {
                                    TryStartTeleport(npc, targetPlayer);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool TryStartDodgeRoll(NPC npc, Projectile projectile)
        {
            Vector2? dodgePos = TeleportUtils.FindDodgeRollPosition(npc, projectile);
            if (dodgePos.HasValue)
            {
                _savedVelocity = npc.velocity;
                npc.velocity = Vector2.Zero;
                _dodgeRollStartPosition = npc.position;
                _dodgeRollTargetPosition = dodgePos.Value;
                _dodgeRollTimer = 15; // 15帧翻滚时间
                _initialRotation = npc.rotation;
                _lastTeleportTime = Main.GameUpdateCount; // 使用相同的冷却计时器
                if (Main.netMode != NetmodeID.Server) Main.NewText("触发了翻滚闪避", Color.Lime);
                return true;
            }
            return false;
        }

        private bool TryStartLeap(NPC npc, Projectile projectile, Player player)
        {
            Vector2? apexPos = TeleportUtils.FindLeapApexPosition(npc, projectile);
            if (apexPos.HasValue)
            {
                _initialRotation = npc.rotation;
                _savedVelocity = npc.velocity;
                npc.velocity = Vector2.Zero;

                _leapStartPosition = npc.Center;
                _leapApexPosition = apexPos.Value;
                _leapPhase = DodgeLeapPhase.Rising;
                _leapTimer = 30; // 30帧到达顶点
                
                _lastTeleportTime = Main.GameUpdateCount;
                if (Main.netMode != NetmodeID.Server) Main.NewText("触发了跳跃闪避（上升）", Color.Yellow);
                return true;
            }
            return false;
        }

        private bool TryStartTeleport(NPC npc, Player player)
        {
            // 已禁用瞬移闪避
            return false;
            // 以下为原有实现：
            // bool isMultiSegment = npc.realLife != -1 && npc.realLife != npc.whoAmI;
            // if (isMultiSegment) return false;
            // Vector2? targetPos = TeleportUtils.FindTeleportPosition(npc, player);
            // if (targetPos.HasValue)
            // {
            //     var dodgePlayer = player.GetModPlayer<DodgePlayer>();
            //     _teleportTargetPosition = targetPos;
            //     _teleportDelayTimer = Math.Max(1, (int)dodgePlayer.TeleportDelayBonus);
            //     _teleportTargetPlayerId = player.whoAmI;
            //     _lastTeleportTime = Main.GameUpdateCount;
            //     if (Main.netMode == NetmodeID.Server)
            //         ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("触发了瞬移闪避"), Color.Cyan);
            //     else
            //         Main.NewText("触发了瞬移闪避", Color.Cyan);
            //     if (_teleportDelayTimer == 1)
            //     {
            //         float originalSpeed = npc.velocity.Length();
            //         TeleportUtils.PerformTeleport(npc, player, _teleportTargetPosition.Value, originalSpeed);
            //         _teleportTargetPosition = null;
            //         _teleportTargetPlayerId = -1;
            //         _teleportDelayTimer = 0;
            //     }
            //     return true;
            // }
            // return false;
        }

        private bool TryStartFlash(NPC npc, Projectile projectile)
        {
            // 随机选择3-5格的位移距离
            float tileDistance = Main.rand.Next(3, 6); // 3, 4, or 5
            float pixelDistance = tileDistance * 16f; // 一个瓦片是16像素
            
            // 确保弹幕所有者是有效的玩家
            Player targetPlayer = null;
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player p = Main.player[projectile.owner];
                if (p.active && !p.dead)
                {
                    targetPlayer = p;
                }
            }

            // 如果找不到有效的玩家，使用NPC的当前目标
            if (targetPlayer == null && npc.target >= 0 && npc.target < Main.maxPlayers)
            {
                targetPlayer = Main.player[npc.target];
                if (!targetPlayer.active || targetPlayer.dead)
                    targetPlayer = null;
            }

            // 如果仍找不到有效的玩家，尝试找到距离最近的玩家
            if (targetPlayer == null)
            {
                float closestDistance = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead)
                    {
                        float distance = Vector2.Distance(npc.Center, p.Center);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            targetPlayer = p;
                        }
                    }
                }
            }

            // 如果仍然找不到有效的玩家，直接失败
            if (targetPlayer == null)
                return false;

            // 计算直接指向玩家的方向向量
            Vector2 dirToPlayer = (targetPlayer.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            
            // 计算目标位置，考虑碰撞体积
            // 计算敌怪边缘到中心的距离（在朝向玩家的方向上）
            float npcEdgeOffset = CalculateEdgeOffset(npc, dirToPlayer);
            
            // 使用边缘作为起点计算目标位置，这样闪现更自然
            Vector2 targetCenter = npc.Center + dirToPlayer * (pixelDistance + npcEdgeOffset);
            Vector2 targetTopLeft = targetCenter - npc.Size / 2f;

            // 检查目标位置是否有效（不会卡墙）
            bool validSpot = !Collision.SolidCollision(targetTopLeft, npc.width, npc.height);

            if (validSpot)
            {
                // 保存原始状态
                _savedVelocityFlash = npc.velocity;
                npc.velocity = Vector2.Zero;
                _flashStepStartPosition = npc.position;
                _flashStepTargetPosition = targetTopLeft;
                _flashStepDirection = dirToPlayer;
                _flashStepDistance = tileDistance;
                _initialRotationFlash = npc.rotation;
                
                // 设置闪现步计时器（距离×2帧）
                _flashStepTimer = (int)(tileDistance * 2);
                
                // 启用鬼魂碰撞（可以穿过玩家但不能穿墙）
                _flashStepGhostActive = true;
                
                // 清空位置历史并添加当前位置作为起点
                _flashStepPositionHistory.Clear();
                _flashStepPositionHistory.Add(npc.Center);
                
                // 使用相同的冷却计时器
                _lastTeleportTime = Main.GameUpdateCount;
                
                // 生成一些初始残影效果，根据配置决定是否生成粒子
                if (enableMissParticles)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 dustPos = npc.Center + Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f);
                        Vector2 dustVel = dirToPlayer * Main.rand.NextFloat(2f, 5f);
                        int dustType = Main.rand.NextBool() ? DustID.WhiteTorch : DustID.BlueTorch;
                        Dust.NewDust(dustPos, 0, 0, dustType, dustVel.X, dustVel.Y, 100, default, 1f);
                    }
                }
                
                if (Main.netMode != NetmodeID.Server) 
                    Main.NewText("触发了闪现步闪避", Color.Cyan);
                
                return true;
            }
            else
            {
                // 如果直接路径不通，则在朝向玩家的半圆内随机尝试10次
                float baseAngle = dirToPlayer.ToRotation();
                for (int i = 0; i < 10; i++)
                {
                    float randomAngleOffset = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                    Vector2 testDir = (baseAngle + randomAngleOffset).ToRotationVector2();
                    
                    float edgeOffset = CalculateEdgeOffset(npc, testDir);
                    Vector2 testCenter = npc.Center + testDir * (pixelDistance + edgeOffset);
                    Vector2 testTopLeft = testCenter - npc.Size / 2f;

                    if (!Collision.SolidCollision(testTopLeft, npc.width, npc.height))
                    {
                        // 找到一个有效位置，使用它
                        _savedVelocityFlash = npc.velocity;
                        npc.velocity = Vector2.Zero;
                        _flashStepStartPosition = npc.position;
                        _flashStepTargetPosition = testTopLeft;
                        _flashStepDirection = testDir;
                        _flashStepDistance = tileDistance;
                        _initialRotationFlash = npc.rotation;
                        
                        _flashStepTimer = (int)(tileDistance * 2);
                        _flashStepGhostActive = true;
                        
                        _flashStepPositionHistory.Clear();
                        _flashStepPositionHistory.Add(npc.Center);
                        
                        _lastTeleportTime = Main.GameUpdateCount;
                        
                        if (enableMissParticles)
                        {
                            for (int d = 0; d < 5; d++)
                            {
                                Vector2 dustPos = npc.Center + Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f);
                                Vector2 dustVel = testDir * Main.rand.NextFloat(2f, 5f);
                                int dustType = Main.rand.NextBool() ? DustID.WhiteTorch : DustID.BlueTorch;
                                Dust.NewDust(dustPos, 0, 0, dustType, dustVel.X, dustVel.Y, 100, default, 1f);
                            }
                        }
                        
                        if (Main.netMode != NetmodeID.Server) 
                            Main.NewText("触发了闪现步闪避 (备用方向)", Color.Cyan);
                        
                        return true;
                    }
                }
            }
            
            // 所有方向都失败
            return false;
        }
        
        private bool TryStartTimeDilation(NPC npc, Projectile projectile)
        {
            // 计算时间膨胀半径：敌怪碰撞体积的1.0倍
            _timeDilationRadius = Math.Max(npc.width, npc.height) * 1.0f;
            _timeDilationTimer = 60; // 持续1秒(60帧)
            _affectedProjectileCache.Clear(); // 清空受影响弹幕缓存
            _isTimeDilationActive = true;
            _originalFrameSpeed = (float)npc.frameCounter; // 保存原始帧速度
            _originalFrameCounter = (int)npc.frameCounter;
            _originalVelocity = npc.velocity; // 保存原始速度
            _timeDilationPosition = npc.position; // 保存敌怪当前位置
            
            // 使用全局冷却计时器
            _lastTeleportTime = Main.GameUpdateCount;
            
            if (Main.netMode != NetmodeID.Server)
                Main.NewText("触发了时间膨胀闪避", Color.Magenta);
            
            return true;
        }
        
        private bool TryStartShadowClones(NPC npc, Projectile projectile)
        {
            // 如果已经有影分身，不再创建
            if (_hasShadowClones || ShadowCloneManager.CloneSources.ContainsKey(npc.whoAmI))
                return false;
                
            // 创建两个幻影影分身
            for (int i = 0; i < 2; i++)
            {
                // 直接使用原始敌怪的位置作为分身生成点
                Vector2 spawnPos = npc.Center;
                
                // 检查位置是否有效（理论上应该总是有效，但为了安全保留检查）
                if (!Collision.SolidCollision(spawnPos - new Vector2(npc.width / 2, npc.height / 2), npc.width, npc.height))
                {
                    // 如果是Boss，使用替代类型以避免触发Boss出现提示
                    int cloneType;
                    bool isBossType = npc.boss;
                    
                    if (isBossType)
                    {
                        if (!npc.noGravity)
                        {
                            // 地面Boss使用僵尸，打到不会有声音
                            cloneType = NPCID.Zombie;
                        }
                        else
                        {
                            // 飞行Boss使用地狱蝙蝠
                            cloneType = NPCID.GiantFlyingFox;
                        }
                    }
                    else
                    {
                        // 非Boss使用原始类型
                        cloneType = npc.type;
                    }
                    
                    // 创建分身NPC，初始位置设置在预计算的生成点
                    int cloneIndex = NPC.NewNPC(
                        npc.GetSource_FromAI(),
                        (int)spawnPos.X,
                        (int)spawnPos.Y,
                        cloneType, // 使用原敌怪类型，继承其AI
                        npc.whoAmI
                    );
                    
                    if (cloneIndex >= 0 && cloneIndex < Main.maxNPCs)
                    {
                        NPC clone = Main.npc[cloneIndex];
                        
                        // 设置分身属性，保留原敌怪的大多数特征
                        clone.life = (int)(npc.life * 0.5f); // 降低生命值，使其更容易被击杀
                        clone.lifeMax = (int)(npc.lifeMax * 0.5f);
                        clone.dontTakeDamage = false; // 确保可以受到伤害
                        clone.defense = (int)(npc.defense * 0.5f); // 降低防御
                        clone.damage = (int)(npc.damage * 0.25f); // 降低伤害，只有原始NPC的四分之一
                        clone.target = npc.target; // 继承目标
                        clone.direction = npc.direction;
                        clone.velocity = Vector2.Zero; // 初始保持静止
                        clone.knockBackResist = Math.Min(1f, npc.knockBackResist * 2); // 增加击退效果
                        
                        // 保持与原敌怪相同的碰撞箱尺寸，确保AI能正常工作
                        // 但是如果是Boss，需要标记为非Boss以避免UI显示
                        if (clone.boss)
                        {
                            clone.boss = false;
                        }
                        
                        // 确保NPC立即进行网络同步
                        clone.netUpdate = true;
                        clone.netUpdate2 = true; // 强制立即同步
                        
                        clone.value = 0; // 设置价值为0，阻止掉落物品
                        clone.npcSlots = 0; // 设置NPC槽位为0
                        
                        // 设置分身特殊属性
                        var cloneGlobal = clone.GetGlobalNPC<ShadowCloneGlobalNPC>();
                        cloneGlobal.IsShadowClone = true;
                        cloneGlobal.OriginalType = npc.type; // 始终存储原始NPC类型，确保正确绘制
                        
                        // 设置一个固定的视觉缩放比例，70-80%
                        float visualScaleRatio = 0.7f;
                        
                        cloneGlobal.VisualScale = visualScaleRatio; // 保存在GlobalNPC中供绘制使用
                        
                        // 一般来说，分身的生命周期较短，60-120帧左右
                        cloneGlobal.MaxLifeTime = Main.rand.Next(60, 121); 
                        
                        // 确保正确设置位置，以防止NPC被错误定位
                        // 由于我们更改了宽高，需要重新调整位置确保中心点不变
                        clone.position = spawnPos - new Vector2(clone.width / 2, clone.height / 2);
                        
                        // 注册分身
                        ShadowCloneManager.RegisterClone(clone.whoAmI, npc.whoAmI);
                    }
                }
            }
            
            // 使用全局冷却
            _lastTeleportTime = Main.GameUpdateCount;
            _hasShadowClones = true;
            
            if (Main.netMode != NetmodeID.Server)
                Main.NewText("触发了影分身闪避", new Color(100, 50, 200));
                
            return true;
        }
        
        private bool TryStartBurrowOrInvisibility(NPC npc, Player player)
        {
            Vector2? targetPos = FindEmergencePosition(npc, player);
            if (targetPos.HasValue)
            {
                _isBurrowingOrInvisible = true;
                _burrowPhase = DodgeBurrowPhase.Diving;
                _burrowTimer = 60; // 30帧下潜 + 30帧隐藏
                _burrowTargetPosition = targetPos.Value;

                _savedVelocity = npc.velocity;
                npc.velocity = Vector2.Zero;
                _initialRotation = npc.rotation;

                _lastTeleportTime = Main.GameUpdateCount;

                if (Main.netMode != NetmodeID.Server)
                {
                    string message = (!npc.noGravity && !npc.wet) ? "触发了遁地闪避" : "触发了隐身闪避";
                    Main.NewText(message, Color.SandyBrown);
                }

                if (enableMissSound)
                {
                    SoundEngine.PlaySound(SoundID.WormDig, npc.Center);
                }

                return true;
            }

            return false;
        }

        private void ResetBurrowState(NPC npc)
        {
            _isBurrowingOrInvisible = false;
            _burrowPhase = DodgeBurrowPhase.Inactive;
            _burrowTimer = 0;
            npc.alpha = 0;
            npc.rotation = _initialRotation;
            if (_savedVelocity != Vector2.Zero)
            {
                npc.velocity = _savedVelocity;
                _savedVelocity = Vector2.Zero;
            }
            npc.dontTakeDamage = false;
        }

        private Vector2? FindEmergencePosition(NPC npc, Player player)
        {
            bool isGroundUnit = !npc.noGravity && !npc.noTileCollide && !npc.wet;
            bool isAquatic = npc.wet;

            for (int i = 0; i < 30; i++) // 尝试30次
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float distance = Main.rand.NextFloat(150, 301);
                Vector2 candidatePoint = player.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;

                if (isGroundUnit)
                {
                    // 从候选点向下扫描找到地面
                    int tileX = (int)(candidatePoint.X / 16);
                    int tileY = (int)(candidatePoint.Y / 16);
                    int groundY = -1;

                    for (int y = tileY; y < tileY + 15; y++)
                    {
                        if (y < Main.maxTilesY - 1 && Main.tile[tileX, y] != null && Main.tile[tileX, y].HasTile && Main.tileSolid[Main.tile[tileX, y].TileType] && !Main.tileSolidTop[Main.tile[tileX, y].TileType])
                        {
                            groundY = y;
                            break;
                        }
                    }

                    if (groundY != -1)
                    {
                        // 找到地面，将出现位置设置在地面之上
                        Vector2 emergencePos = new Vector2(candidatePoint.X, groundY * 16 - npc.height / 2f);
                        // 检查该位置是否足够宽敞
                        if (!Collision.SolidCollision(emergencePos - npc.Size / 2f, npc.width, npc.height))
                        {
                            return emergencePos;
                        }
                    }
                }
                else if (isAquatic)
                {
                    int tileX = (int)(candidatePoint.X / 16);
                    int tileY = (int)(candidatePoint.Y / 16);
                    if (tileX > 0 && tileX < Main.maxTilesX && tileY > 0 && tileY < Main.maxTilesY && Main.tile[tileX, tileY].LiquidAmount > 128)
                    {
                        if (!Collision.SolidCollision(candidatePoint - npc.Size / 2f, npc.width, npc.height))
                        {
                            return candidatePoint;
                        }
                    }
                }
                else // 飞行或穿墙单位
                {
                    if (!Collision.SolidCollision(candidatePoint - npc.Size / 2f, npc.width, npc.height))
                    {
                        return candidatePoint;
                    }
                }
            }

            return null; // 30次尝试后仍未找到
        }

        // 辅助方法：计算从NPC中心到特定方向的边缘距离
        private float CalculateEdgeOffset(NPC npc, Vector2 direction)
        {
            // 假设NPC是一个矩形，我们需要计算从中心到边缘的距离
            // 在指定的方向上
            
            // 通过数学计算，从中心到边缘的距离取决于方向角度以及宽高比
            float width = npc.width / 2f;
            float height = npc.height / 2f;
            
            // 使用极坐标计算方向角度
            float angle = (float)Math.Atan2(direction.Y, direction.X);
            
            // 计算到矩形边缘的距离（使用参数方程）
            float cosAngle = (float)Math.Cos(angle);
            float sinAngle = (float)Math.Sin(angle);
            
            float distance;
            if (Math.Abs(cosAngle) < 0.0001f)
            {
                // 几乎垂直，直接使用高度
                distance = height;
            }
            else if (Math.Abs(sinAngle) < 0.0001f)
            {
                // 几乎水平，直接使用宽度
                distance = width;
            }
            else
            {
                // 一般情况，计算到矩形边缘的距离
                float tx = Math.Abs(width / cosAngle);
                float ty = Math.Abs(height / sinAngle);
                distance = Math.Min(tx, ty);
            }
            
            return distance;
        }

        // 添加对物品（近战）伤害的处理方法
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            // 如果NPC是影分身，则不执行闪避逻辑
            if (ShadowCloneManager.IsClone(npc.whoAmI))
            {
                return;
            }
            
            // 时间回溯后的伤害减免
            if (_damageReductionTimer > 0)
            {
                modifiers.FinalDamage *= 0.5f;
            }

            // 如果NPC隐身，处理受击逻辑
            if (_isInvisible)
            {
                if (_invisibilityInvincibilityTimer > 0)
                {
                    // 在无敌期间，完全免疫伤害
                    modifiers.FinalDamage *= 0;
                    modifiers.SetMaxDamage(0);
                    modifiers.DisableCrit();
                    if (Main.netMode != NetmodeID.Server)
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Center.Y, 1, 1), Color.White, "Immune", true);
                    }
                }
                else
                {
                    // 无敌期结束，受击则解除隐身
                    _isInvisible = false;
                    _invisibilityTimer = 0;
                    // 恢复原始缩放比例
                    npc.scale = _originalScale;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Center.Y, 1, 1), Color.Orange, "Revealed!", true);
                    }
                    if (enableMissParticles)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 150, default, 1.1f);
                        }
                    }
                }
                return; // 结束此方法的执行，让本次攻击正常结算（或被免疫）
            }

            // 注意：我们只在非近战伤害或复合伤害（比如投掷+近战）类武器时才检查闪避
            // 对于纯近战攻击，我们不应用闪避逻辑
            
            // 检查是否是纯近战伤害
            bool isPurelyMelee = item.damage > 0 && item.DamageType == DamageClass.Melee && item.shoot <= ProjectileID.None;
            
            // 对纯近战不应用闪避
            if (isPurelyMelee) return;
            
            // 检查是否应用闪避逻辑
            bool canApply = (IsConsideredBoss(npc) && enableBossDodge) || (!IsConsideredBoss(npc) && !npc.townNPC && enableNormalEnemyDodge);
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

        private bool TryStartInvisibility(NPC npc)
        {
            // 如果是Boss或多节肢体敌怪，直接返回false不允许使用隐身闪避
            if (IsConsideredBoss(npc)) return false;
            bool isMultiSegment = npc.realLife != -1 && npc.realLife != npc.whoAmI;
            if (isMultiSegment) return false;
            
            var config = ModContent.GetInstance<SmartDodgeConfig>();
            if (config == null) return false;

            _isInvisible = true;
            
            // 持续时间为冷却的40%
            double cooldownInTicks = config.TeleportCooldown * 60;
            _invisibilityTimer = (int)(cooldownInTicks * 0.4);
            
            // 进入时有1秒无敌
            _invisibilityInvincibilityTimer = 60;
            
            // 保存原始缩放比例
            _originalScale = npc.scale;
            
            // 缩小到原始大小的1%
            npc.scale *= 0.01f;

            _lastTeleportTime = Main.GameUpdateCount; // 使用全局冷却计时器

            if (enableMissSound)
            {
                SoundEngine.PlaySound(SoundID.Item8, npc.Center); // 消失音效
            }
            if (enableMissParticles)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Vortex, Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4), 150, default, 1.5f);
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("触发了隐身闪避", Color.SlateGray);
            }

            return true;
        }

        private bool TryStartMagneticWave(NPC npc, Projectile projectile)
        {
            _isMagneticWaveActive = true;
            _magneticWaveTimer = 60; // 持续1秒
            _magneticWaveMaxRadius = Math.Max(npc.width, npc.height) * 2.0f; // 范围为受击框最大尺寸的2倍
            _magneticWaveCurrentRadius = 0f;
            _deflectedProjectiles.Clear();

            _lastTeleportTime = Main.GameUpdateCount; // 使用全局冷却

            if (enableMissSound)
            {
                SoundEngine.PlaySound(SoundID.Item122, npc.Center); // 磁力护盾启动音效
            }
            
            if (Main.netMode != NetmodeID.Server)
                Main.NewText("触发了磁力波闪避", Color.Aqua);

            return true;
        }
        
        private bool TryStartInkSplash(NPC npc, Player player)
        {
            // 确保玩家有效
            if (player == null || !player.active || player.dead)
                return false;
                
            // 设置墨水喷射状态
            _isInkSplashActive = true;
            _inkSplashTimer = 120; // 墨水喷射持续2秒
            _inkSplashInterval = 10; // 每10帧喷射一次
            _inkSplashCount = 0; // 重置已喷射次数
            _inkSplashDirection = (player.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            _inkSplashTargetPlayerId = player.whoAmI;
            
            // 使用全局冷却
            _lastTeleportTime = Main.GameUpdateCount;
            
            // 播放墨水喷射音效
            if (enableMissSound)
            {
                SoundEngine.PlaySound(SoundID.Splash, npc.Center);
            }
            
            // 生成初始墨水喷射效果
            if (enableMissParticles)
            {
                // 创建从NPC朝向玩家方向的墨水粒子效果
                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustVel = _inkSplashDirection * Main.rand.NextFloat(3f, 8f) + 
                                      Main.rand.NextVector2Circular(2f, 2f);
                    
                    Dust dust = Dust.NewDustDirect(
                        npc.Center,
                        0, 0,
                        DustID.Shadowflame, // 使用暗影火焰粒子表示墨水
                        dustVel.X, dustVel.Y,
                        100, new Color(20, 20, 40), 1.2f
                    );
                    dust.noGravity = true;
                }
            }
            
            if (Main.netMode != NetmodeID.Server)
                Main.NewText("触发了墨水喷射闪避", Color.DarkBlue);
                
                
            return true;
        }

        private bool TryStartTimeRewind(NPC npc)
        {
            if (_history.Count < 45)
            {
                return false;
            }

            NPCState rewindTargetState = _history.First();
            Vector2 targetPos = rewindTargetState.Position;

            // 检查目标位置是否会被实体方块阻挡
            if (Collision.SolidCollision(targetPos, npc.width, npc.height))
            {
                return false; // 目标位置被阻挡，取消回溯
            }

            // 启动时间回溯
            _preRewindHealth = npc.life;
            _timeRewindStartPosition = npc.position;
            _timeRewindTargetPosition = targetPos;
            _timeRewindTargetHealth = rewindTargetState.Health;
            _timeRewindTimer = TimeRewindDuration;

            _timeRewindAfterimageHistory.Clear();
            _timeRewindAfterimageHistory.Add(npc.Center);

            _lastTeleportTime = Main.GameUpdateCount; // 使用全局冷却

            if (enableMissSound)
            {
                SoundEngine.PlaySound(SoundID.Item4, npc.Center); // "Rewind" sound
            }
            if (enableMissParticles)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.AncientLight, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3), 100, Color.LightCyan, 1.5f);
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("触发了时间回溯闪避", Color.LightCyan);
            }

            return true;
        }

        private bool TryStartShrink(NPC npc)
        {
            // 如果是Boss或多节肢体敌怪，直接返回false不允许使用缩小闪避
            if (IsConsideredBoss(npc)) return false;
            bool isMultiSegment = npc.realLife != -1 && npc.realLife != npc.whoAmI;
            if (isMultiSegment) return false;
            
            _isShrinking = true;
            _shrinkDodgeTimer = 120; // 持续2秒
            _originalScale = npc.scale;
            npc.scale *= 0.5f; // 缩小50%

            _lastTeleportTime = Main.GameUpdateCount; // 使用全局冷却

            if (enableMissSound)
            {
                SoundEngine.PlaySound(SoundID.Item7, npc.Center); // "Shrink" sound
            }
            if (enableMissParticles)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("触发了缩小闪避", Color.Gray);
            }

            return true;
        }

        private Vector2? FindNearbySafeSpot(NPC npc, float scaleToTest)
        {
            Vector2 originalCenter = npc.Center;
            float originalScale = npc.scale;

            npc.scale = scaleToTest;

            for (int i = 0; i < 50; i++) // 增加尝试次数
            {
                float radius = 60f + (i * 10f); // 搜索半径从~4格到~35格
                Vector2 candidateCenter = originalCenter + Main.rand.NextVector2Circular(radius, radius);
                npc.Center = candidateCenter;

                if (!Collision.SolidCollision(npc.position, npc.width, npc.height))
                {
                    npc.Center = originalCenter;
                    npc.scale = originalScale;
                    return candidateCenter;
                }
            }
            
            npc.Center = originalCenter;
            npc.scale = originalScale;
            return null;
        }

        private void RestoreShrinkState(NPC npc)
        {
            Vector2 currentCenter = npc.Center;
            float originalScale = _originalScale;

            // 准备恢复原状
            npc.scale = originalScale;
            npc.Center = currentCenter;

            // 检查恢复后是否会卡住
            if (Collision.SolidCollision(npc.position, npc.width, npc.height))
            {
                // 卡住了，寻找附近安全点
                Vector2? safeSpot = FindNearbySafeSpot(npc, originalScale);
                if (safeSpot.HasValue)
                {
                    npc.Center = safeSpot.Value;
                    if (Main.netMode != NetmodeID.Server) Main.NewText("Shrink: Restored size at a new location.", Color.Yellow);

                    // 产生粒子效果
                    if (enableMissParticles)
                    {
                        for (int i = 0; i < 20; i++)
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));
                    }
                }
                else
                {
                    // 找不到安全点，在原地强行恢复
                    npc.Center = currentCenter;
                    if (Main.netMode != NetmodeID.Server) Main.NewText("Shrink: Could not find a safe spot, forcing restore.", Color.Red);
                }
            }

            // 恢复音效
            if (enableMissSound)
            {
                SoundEngine.PlaySound(SoundID.Item61, npc.Center); // "Grow" sound
            }

            // 重置状态
            _isShrinking = false;
            _originalScale = 1f;
            npc.dontTakeDamage = false;
        }

        private bool IsConsideredBoss(NPC npc)
        {
            return npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }

    }
} 