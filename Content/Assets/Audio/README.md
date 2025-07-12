# 音频资源说明

## Megalovania音频文件

为了支持愚人节彩蛋功能，需要在此目录下添加一个压缩版的Megalovania音频文件。

### 文件要求：
- 文件名：`Megalovania_Compressed.wav`
- 格式：WAV格式（Terraria支持的音频格式）
- 音质：故意压缩得很差，体现愚人节幽默效果
- 大小：建议控制在1MB以内

### 获取方式：
1. 从Undertale游戏中提取Megalovania音乐
2. 使用音频编辑软件（如Audacity）进行压缩处理
3. 降低比特率和采样率，制造"压缩得很差"的效果
4. 保存为WAV格式

### 注意事项：
- 确保音频文件的使用符合版权规定
- 仅用于愚人节彩蛋，不会影响正常游戏体验
- 文件路径将在代码中引用为：`smartDodgeAI/Content/Assets/Audio/Megalovania_Compressed`

### 代码引用示例：
```csharp
SoundEngine.PlaySound(ModContent.Request<SoundStyle>("smartDodgeAI/Content/Assets/Audio/Megalovania_Compressed"));
``` 