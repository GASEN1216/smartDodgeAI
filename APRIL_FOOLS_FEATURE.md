# 愚人节彩蛋功能说明

## 概述

这个愚人节彩蛋功能为智能躲避AI模组添加了一个特殊的骷髅专用模式，仅在4月1日（愚人节）显示和使用。

## 功能特性

### 🎃 骷髅专用闪避模式
- **启用条件**：仅在4月1日显示配置选项
- **功能**：当启用时，只有骷髅类型的NPC会尝试闪避玩家的攻击
- **兼容性**：支持原版骷髅和Mod骷髅（通过名称关键词识别）

### 🎵 Megalovania音乐播放
- **触发条件**：骷髅专用模式启用 + 骷髅NPC在附近
- **音乐**：播放压缩版的Megalovania音乐（Undertale经典BGM）
- **音质**：故意压缩得很差，体现愚人节幽默效果
- **持续时间**：30秒后自动停止

### 🧪 测试模式
- **激活方式**：按F12键激活测试模式
- **功能**：允许在任何时间测试愚人节彩蛋功能
- **持续时间**：10秒后自动结束
- **用途**：方便开发和调试

## 技术实现

### 文件结构
```
Content/
├── Assets/
│   └── Audio/
│       ├── README.md                    # 音频文件说明
│       └── Megalovania_Compressed.wav   # 压缩版Megalovania（需要手动添加）
├── Config/
│   └── SmartDodgeConfig.cs              # 配置类（已扩展）
├── NPCs/
│   └── SmartDodgeGlobalNPC.cs           # NPC逻辑（已修改）
├── Systems/
│   ├── AprilFoolsAudioSystem.cs         # 音频管理系统
│   └── AprilFoolsTestSystem.cs          # 测试系统
└── Utils/
    └── SkeletonUtils.cs                 # 骷髅识别工具
```

### 核心组件

#### 1. 配置系统 (SmartDodgeConfig.cs)
- 添加了`EnableSkeletonOnlyDodge`配置选项
- 实现了日期检测逻辑`IsAprilFoolsDay()`
- 支持条件显示`[ShowWhen("ShouldShowAprilFoolsOption")]`

#### 2. 骷髅识别系统 (SkeletonUtils.cs)
- 支持原版骷髅类型识别
- 支持Mod骷髅通过名称关键词识别
- 可扩展支持特定Mod的骷髅类型

#### 3. 音频管理系统 (AprilFoolsAudioSystem.cs)
- 管理Megalovania音乐的播放和停止
- 检测附近骷髅NPC的存在
- 处理音频文件缺失的异常情况

#### 4. 测试系统 (AprilFoolsTestSystem.cs)
- 提供F12键激活的测试模式
- 允许在任何时间测试功能
- 提供测试状态信息

## 使用方法

### 正常使用（4月1日）
1. 启动游戏并加载模组
2. 打开Mod配置菜单
3. 在"AprilFoolsSpecial"部分找到"EnableSkeletonOnlyDodge"选项
4. 启用该选项
5. 寻找骷髅NPC，当它们出现时会播放Megalovania音乐

### 测试模式（任何时间）
1. 在游戏中按F12键激活测试模式
2. 系统会自动启用骷髅专用模式
3. 寻找骷髅NPC测试功能
4. 10秒后测试模式自动结束

## 骷髅类型支持

### 原版骷髅
- 普通骷髅 (Skeleton)
- 骷髅弓箭手 (SkeletonArcher)
- 骷髅突击队员 (SkeletonCommando)
- 骷髅狙击手 (SkeletonSniper)
- 骷髅礼帽 (SkeletonTopHat)
- 骷髅宇航员 (SkeletonAstonaut)
- 骷髅外星人 (SkeletonAlien)

### Mod骷髅
通过名称关键词识别：
- "skeleton", "skel", "bone", "undead", "skele"
- "骷髅", "骨头"

### 特定Mod支持
- Calamity Mod骷髅
- Thorium Mod骷髅
- 可扩展支持更多Mod

## 音频文件要求

### Megalovania_Compressed.wav
- **格式**：WAV格式
- **音质**：故意压缩得很差（低比特率、低采样率）
- **大小**：建议控制在1MB以内
- **获取方式**：
  1. 从Undertale游戏中提取Megalovania音乐
  2. 使用音频编辑软件进行压缩处理
  3. 降低比特率和采样率制造"压缩得很差"的效果
  4. 保存为WAV格式

### 注意事项
- 确保音频文件的使用符合版权规定
- 仅用于愚人节彩蛋，不会影响正常游戏体验
- 如果音频文件不存在，系统会使用替代音效

## 配置选项

| 选项 | 描述 | 默认值 | 显示条件 |
|------|------|--------|----------|
| EnableSkeletonOnlyDodge | 仅对骷髅类型NPC启用闪避功能 | false | 4月1日或测试模式 |

## 兼容性

- **原版兼容性**：完全兼容原版游戏
- **Mod兼容性**：支持大部分Mod的骷髅类型
- **多人游戏**：支持多人游戏模式
- **性能影响**：最小化性能影响，仅在需要时检测

## 故障排除

### 常见问题

1. **配置选项不显示**
   - 检查当前日期是否为4月1日
   - 尝试使用F12键激活测试模式

2. **Megalovania音乐不播放**
   - 确认音频文件已正确放置在`Content/Assets/Audio/`目录
   - 检查音频文件格式是否为WAV
   - 查看游戏日志中的错误信息

3. **骷髅识别不准确**
   - 检查`SkeletonUtils.cs`中的关键词设置
   - 可以手动添加特定Mod的骷髅类型

### 调试信息

使用测试模式时，系统会显示详细的调试信息：
- 测试模式状态
- 骷髅专用模式状态
- Megalovania播放状态
- 剩余测试时间

## 扩展开发

### 添加新的骷髅类型
在`SkeletonUtils.cs`中的`IsVanillaSkeleton`或`IsSpecificModSkeleton`方法中添加新的NPC类型ID或Mod名称。

### 修改音频文件
替换`Content/Assets/Audio/Megalovania_Compressed.wav`文件即可更改播放的音乐。

### 调整检测范围
在`AprilFoolsAudioSystem.cs`中修改`detectionRange`变量来调整骷髅检测范围。

---

*这是一个有趣的愚人节彩蛋，结合了Undertale文化和泰拉瑞亚游戏体验！* 