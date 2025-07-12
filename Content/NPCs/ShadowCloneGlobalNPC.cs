using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using smartDodgeAI.Content.Buffs;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace smartDodgeAI.Content.NPCs
{
    public class ShadowCloneGlobalNPC : GlobalNPC
    {
        public bool IsShadowClone { get; set; } = false;
        public int LifeTime { get; set; } = 0;
        public int MaxLifeTime { get; set; } = 0;
        public float AlphaValue { get; set; } = 1f;
        public int OriginalType { get; set; } = -1; // 存储原始NPC类型，用于绘制
        public float VisualScale { get; set; } = 0.7f; // 视觉缩放比例，默认为0.7
        private Vector2 _lastKnownSourcePosition = Vector2.Zero; // 上次已知的源NPC位置
        
        public override bool InstancePerEntity => true;

        // 重写AI方法，让影分身继承原敌怪的AI行为
        public override bool PreAI(NPC npc)
        {
            if (IsShadowClone)
            {
                // 确保Boss分身不触发Boss UI，无论使用什么类型
                if (npc.boss)
                {
                    npc.boss = false;
                }
                
                // 增加生命时间计数
                LifeTime++;
                
                // 临近消失时开始淡出
                if (LifeTime > MaxLifeTime - 30)
                {
                    AlphaValue = 1f - (LifeTime - (MaxLifeTime - 30)) / 30f;
                    if (AlphaValue < 0.1f) AlphaValue = 0.1f;
                }
                
                // 如果已达到最大生命时间，移除分身
                if (LifeTime >= MaxLifeTime)
                {
                    npc.active = false;
                    npc.life = 0;
                    ShadowCloneManager.UnregisterClone(npc.whoAmI);
                    return false;
                }
                
                // 返回true允许执行原始AI，继承原敌怪的行为
                return true;
            }

            // 非影分身执行正常的AI
            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (IsShadowClone)
            {
                // 确保不掉落物品
                npc.value = 0;
                npc.npcSlots = 0;
                
                // 如果是Boss，将其标记为非Boss以避免UI显示
                if (npc.boss)
                {
                    npc.boss = false;
                }
                
                // 如有必要，添加随机的小移动，使分身看起来更"鬼魅"
                if (Main.rand.NextBool(30))
                {
                    npc.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                }
                
                // 匹配源NPC的朝向
                int sourceNpcId = ShadowCloneManager.GetCloneSource(npc.whoAmI);
                if (sourceNpcId != -1 && sourceNpcId < Main.maxNPCs && Main.npc[sourceNpcId].active)
                {
                    NPC sourceNpc = Main.npc[sourceNpcId];
                    _lastKnownSourcePosition = sourceNpc.Center;
                    
                    // 匹配源NPC的朝向
                    npc.direction = sourceNpc.direction;
                    npc.spriteDirection = sourceNpc.spriteDirection;
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (IsShadowClone && OriginalType != -1)
            {
                // 始终使用原始NPC的纹理进行绘制，即使当前是替代类型
                Texture2D originalTexture = TextureAssets.Npc[OriginalType].Value;
                
                if (originalTexture != null)
                {
                    // 获取原始NPC的帧信息
                    Rectangle frame;
                    
                    // 尝试获取合适的帧信息
                    int frameCount = Main.npcFrameCount[OriginalType];
                    if (frameCount <= 1)
                    {
                        // 单帧NPC
                        frame = originalTexture.Frame();
                    }
                    else
                    {
                        // 多帧NPC - 处理帧动画
                        int frameHeight = originalTexture.Height / frameCount;
                        int frameY;
                        
                        // 如果当前NPC类型与原始类型不同（比如Boss替换为普通敌怪），使用特殊的帧计算
                        if (npc.type != OriginalType)
                        {
                            // 使用生命周期作为动画基础，使动画流畅播放
                            frameY = (LifeTime / 5) % frameCount * frameHeight;
                        }
                        else
                        {
                            // 正常情况，使用当前NPC的帧
                            frameY = Math.Min((npc.frame.Y * frameHeight) / Math.Max(1, npc.frame.Height), 
                                           (frameCount - 1) * frameHeight);
                        }
                        
                        frame = new Rectangle(0, frameY, originalTexture.Width, frameHeight);
                    }

                    // 修改颜色，使分身更暗且半透明
                    Color shadowColor = drawColor;
                    shadowColor.R = (byte)(shadowColor.R * 0.6 * AlphaValue);
                    shadowColor.G = (byte)(shadowColor.G * 0.4 * AlphaValue);
                    shadowColor.B = (byte)(shadowColor.B * 0.8 * AlphaValue);
                    shadowColor.A = (byte)(shadowColor.A * AlphaValue);
                    
                    // 绘制位置和方向
                    Vector2 origin = frame.Size() / 2f;
                    SpriteEffects spriteEffects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    
                    // 使用视觉比例缩放而非NPC的实际缩放
                    float drawScale = VisualScale;
                    
                    // 绘制阴影幻影
                    spriteBatch.Draw(
                        originalTexture,
                        npc.Center - screenPos,
                        frame,
                        shadowColor,
                        npc.rotation,
                        origin,
                        drawScale,
                        spriteEffects,
                        0f
                    );
                    
                    // 计算余辉效果位置偏移
                    Vector2 afterimageOffset = new Vector2(
                        (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 2f,
                        (float)Math.Cos(Main.GameUpdateCount * 0.1f) * 2f
                    );
                    
                    // 减少余辉效果的数量和强度，避免过于明显
                    Vector2 drawPos = npc.Center - screenPos + afterimageOffset;
                    
                    // 余辉颜色，更亮更透明
                    Color afterimageColor = new Color(150, 100, 255, 80) * (AlphaValue * 0.3f);
                    
                    // 只绘制一个余辉，降低视觉复杂度
                    spriteBatch.Draw(
                        originalTexture,
                        drawPos,
                        frame,
                        afterimageColor,
                        npc.rotation,
                        origin,
                        drawScale * 1.03f, // 略微放大
                        spriteEffects,
                        0f
                    );
                    
                    // 返回false阻止默认绘制
                    return false;
                }
            }
            
            // 非分身或者没有原始类型信息，使用默认绘制
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }
        
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (IsShadowClone && OriginalType == -1) // 只在没有自定义绘制时应用颜色修改
            {
                // 修改绘制颜色使分身更暗且半透明
                drawColor.R = (byte)(drawColor.R * 0.7 * AlphaValue);
                drawColor.G = (byte)(drawColor.G * 0.5 * AlphaValue);
                drawColor.B = (byte)(drawColor.B * 0.9 * AlphaValue);
                drawColor.A = (byte)(drawColor.A * AlphaValue);
            }
        }
        
        public override bool CheckDead(NPC npc)
        {
            if (IsShadowClone)
            {
                // 如果是分身被杀死，给击杀的玩家施加debuff
                if (npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers)
                {
                    Player player = Main.player[npc.lastInteraction];
                    if (player.active && !player.dead)
                    {
                        // 施加减攻击力debuff，持续2秒（120帧）
                        player.AddBuff(ModContent.BuffType<ShadowConfusion>(), 120);
                    }
                }
                
                // 注销这个分身
                ShadowCloneManager.UnregisterClone(npc.whoAmI);
                
                // 播放击杀特效
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(
                        npc.position, 
                        npc.width, 
                        npc.height, 
                        DustID.Shadowflame, // 使用暗影火焰粒子
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f),
                        100, default, 1.2f
                    );
                }
                
                // 根据NPC类型选择适当的死亡音效
                if (npc.type == NPCID.Mimic)
                {
                    // 宝箱怪使用NPCDeath6音效
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath6, npc.Center);
                }
                else if (npc.type == NPCID.GiantFlyingFox)
                {
                    // 地狱蝙蝠使用NPCDeath4音效
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                }
                else
                {
                    // 默认使用原始的NPCDeath6音效
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath1, npc.Center);
                }
                
                // 返回 false 来阻止原版的死亡逻辑，包括掉落物品
                return false;
            }
            
            return base.CheckDead(npc);
        }
        
        // 添加ModifyTypeName方法，修复鼠标悬停时显示的名称
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            if (IsShadowClone && OriginalType != -1)
            {
                // 使用原始NPC类型的名称，而不是替代NPC的名称
                typeName = Lang.GetNPCNameValue(OriginalType);
            }
        }
        
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 如果是分身，阻止掉落物品
            if (IsShadowClone)
            {
                // 清空所有掉落规则
                npcLoot.RemoveWhere(rule => true);
            }
        }
        
        // 添加HitEffect方法处理分身被击中时的音效
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            if (IsShadowClone)
            {
                // 所有影分身被击中时都使用僵尸音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);
                // 可以根据需要添加更多NPC类型的音效处理
            }
        }
    }
} 