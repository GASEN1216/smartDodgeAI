方案一：短距翻滚回避
翻滚距离为躲避的弹幕大小的两倍，采用 NPC.velocity.X 连续插值而非瞬移，翻滚期间给 15 帧 iFrame，并同步敌怪显示的翻滚效果，要求一定是360°的倍数，确保敌怪结束时显示正确。翻滚方向应优先远离弹幕中心，并且确保地上的怪不穿墙，若该方案没有可以躲避的地方则使用瞬移到玩家身后的原始方案。
方案二：跳跃式空翻
多节敌怪不适用该方案。空翻高度为敌怪自身碰撞体积+弹幕大小的一到三倍。落点附近需检测斜坡/半砖，以防半空卡住。结束瞬间附加一次突刺冲向玩家，利用 Terraria 的 NPC.NewProjectile 生成短距离近战 hitbox，模拟俯冲反击。
方案三：瞬时侧移（闪现步）
改为 3–5 格线性 Lerp 位移，位移过程帧数 = 距离 × 2（即每格 2 帧），保证高帧率视觉平滑同时可被击中；开启 ghostCollision 标志使其穿越玩家但仍受地形阻挡，位移完成后再开启旧碰撞。
方案四：时停减速
仅对以 NPC 为目标的弹幕调用 SetTimeLeft(timeLeft / 3) 实现“减速”；持续 10 帧内提升自身速度 25%，结束后恢复。多人环境下在 NetUpdate 中广播受影响弹幕 id 列表，避免客户端分歧。
方案五：幻影影分身
使用 NPC.Clone() 快照生成 2 个虚影，并将 realLife 指向本体 id，所有虚影设置 don’tTakeDamage = false 但 lifeMax = 1；虚影随机 30–60 帧后自动消失。若玩家击杀虚影则给本体添加暴击增益 buff（自定义 buff id）。
方案六：格挡／弹反
当检测到弹幕轨迹正对 NPC 中心且剩余飞行时间 < 20 帧时触发举盾动画；格挡窗口 12 帧内将触碰到的 Projectile.friendly 置为 false 并在 velocity = –1.2f 反射回去。冷却 180 帧，防止 spam。
方案七：高度差切换
遁地版本：将 NPC.position.Y 逐帧插值到最近的非实心方块下方 4 格处，同时关闭碰撞；1 秒后在 6–10 格远处重新钻出。飞升版本：若上方 8 格内无顶，可将 velocity.Y = –10f 抛射上升，再缓降。两者均需在 Multiplayer 中用 syncExtraFields 发送状态。
方案八：吸收 → 位移
射弹吸收时将 projectile.hide = true 并记录 damage/type；0.5 s 内位移到玩家左/右 5 格，然后生成新的 projectile 克隆原属性射回。若原弹幕为穿墙型则降低其穿墙层级，防止过穿。
方案九：磁力牵引错位
主动释放 12 格半径的吸附脉冲，将玩家推离自己 10 格并对区域内弹幕 velocity.Y += 5f 让其偏转，效果持续 30 帧；考虑与 Hook、Basilisk Mount 等高速冲撞冲突，需要在玩家受力前检测 Grapple 状态以避免拉扯异常。
方案十：预判加速冲刺
读取玩家过去 10 帧平均 velocity，设定 dashDir 同向，dashDuration 18 帧，dashSpeed 12 u/tick；冲刺期间限制方向改变，若与玩家水平距离 < 3 格则提前结束并转为横扫攻击。通过 localAI[0] 存残余冷却。
方案十一：时间回溯步
缓存最近 45 帧坐标于 circular buffer，触发时检查回溯目标处是否仍为空间（tile collision check）；若被封闭则取消。回溯完成后 20 帧内给予 50% 减伤，避免被 AoE 秒杀。残影用 Dust.NewDust 模拟。
方案十二：随机化 hitbox
在 20 帧窗口内将 NPC.width 与 height 插值到 60% 大小，同时生成 Blur shader 叠色。服务器只改变 hitbox，不同步视觉 shader，客户端根据 flag 渲染淡影。避免与体型极小的 Slime 類 NPC 叠加导致难以选中。