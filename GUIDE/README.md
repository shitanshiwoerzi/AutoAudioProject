# 3D游戏区域音频系统

这个系统允许玩家进入不同区域时自动切换背景音乐，支持淡入淡出效果和UI控制。**新增AI音乐生成功能**，可以调用Suno API动态生成背景音乐。

## 系统组件

### 1. AudioManager.cs
- **功能**: 全局音频管理器，负责音乐播放和切换
- **特性**: 
  - 单例模式，跨场景不销毁
  - 支持音乐淡入淡出
  - 避免重复播放同一音乐
  - 音量控制和静音功能

### 2. ZoneTrigger.cs
- **功能**: 区域触发器，检测玩家进入/离开区域
- **特性**:
  - 自动检测Player标签的对象
  - 在Scene视图中显示触发器范围（不同颜色）
  - 支持调试信息输出
  - 可配置淡入淡出时间

### 3. AudioUI.cs
- **功能**: UI界面，显示当前播放音乐和区域信息
- **特性**:
  - 实时显示当前音乐和区域
  - 音量滑块控制
  - 静音/取消静音按钮
  - 可配置UI更新间隔

### 4. ZoneSetupHelper.cs
- **功能**: 辅助工具，帮助快速设置4个区域
- **特性**:
  - 一键创建和配置所有区域
  - 自动添加必要组件
  - 设置检查功能
  - 右键菜单快速操作

### 5. AIGeneratedMusic.cs
- **功能**: AI音乐生成器，调用Suno API生成背景音乐
- **特性**:
  - 支持Suno API集成
  - 音乐缓存系统
  - 生成进度监控
  - 错误处理和重试
  - 与现有音频系统无缝集成

### 6. AIMusicUI.cs
- **功能**: AI音乐生成UI控制界面
- **特性**:
  - 实时生成状态显示
  - 自定义提示词输入
  - 区域选择下拉菜单
  - 生成进度条
  - 缓存管理

## 设置步骤

### 第一步：创建AudioManager
1. 在场景中创建一个空的GameObject
2. 命名为"AudioManager"
3. 添加AudioManager.cs脚本
4. 添加AudioSource组件
5. 在Inspector中设置4个区域的音频文件

### 第二步：设置区域触发器
**方法一：使用ZoneSetupHelper（推荐）**
1. 创建一个空的GameObject，命名为"ZoneSetupHelper"
2. 添加ZoneSetupHelper.cs脚本
3. 在Inspector中设置4个区域的GameObject和音频文件
4. 右键点击ZoneSetupHelper，选择"创建默认区域GameObject"
5. 右键点击ZoneSetupHelper，选择"自动设置所有区域"

**方法二：手动设置**
1. 为每个区域创建一个空的GameObject
2. 添加BoxCollider组件，设置为Trigger
3. 添加ZoneTrigger.cs脚本
4. 设置区域ID、名称和音频文件

### 第三步：设置UI（可选）
1. 创建Canvas和UI元素
2. 添加AudioUI.cs脚本到Canvas
3. 在Inspector中连接UI组件

### 第四步：设置AI音乐生成（可选）
1. 创建空的GameObject，命名为"AIGeneratedMusic"
2. 添加AIGeneratedMusic.cs脚本
3. 在Inspector中设置Suno API密钥
4. 配置音乐生成参数（模型、时长、质量等）

### 第五步：设置AI音乐UI（可选）
1. 创建Canvas和UI元素
2. 添加AIMusicUI.cs脚本到Canvas
3. 在Inspector中连接UI组件

### 第六步：确保Player标签
确保主角GameObject的Tag设置为"Player"

## 配置说明

### AudioManager配置
- **zone1Music ~ zone4Music**: 4个区域的背景音乐文件
- **defaultVolume**: 默认音量 (0-1)
- **loopMusic**: 是否循环播放音乐
- **showDebugInfo**: 是否显示调试信息

### ZoneTrigger配置
- **zoneID**: 区域ID (1-4)
- **zoneName**: 区域名称
- **zoneMusic**: 该区域的背景音乐
- **fadeTime**: 淡入淡出时间（秒）
- **showDebugInfo**: 是否显示调试信息

### ZoneSetupHelper配置
- **zone1Trigger ~ zone4Trigger**: 4个区域的GameObject
- **zone1Audio ~ zone4Audio**: 4个区域的音频文件
- **zone1Name ~ zone4Name**: 4个区域的名称
- **zone1Size ~ zone4Size**: 4个区域的触发器大小

### AIGeneratedMusic配置
- **apiKey**: Suno API密钥
- **baseUrl**: API基础URL (https://api.sunoapi.org/api/v1)
- **defaultModel**: 音乐生成模型 (V3_5, V3, V2, V1)
- **defaultCustomMode**: 自定义模式 (boolean)
- **defaultInstrumental**: 纯音乐模式 (boolean)
- **callBackUrl**: 回调URL (可选)
- **enableCaching**: 是否启用音乐缓存
- **maxCacheSize**: 最大缓存数量

### ZoneTrigger AI配置
- **useAIGeneratedMusic**: 是否使用AI生成音乐
- **customPrompt**: 自定义音乐提示词

## 使用技巧

### 1. 快速设置
使用ZoneSetupHelper的右键菜单功能：
- "创建默认区域GameObject": 自动创建4个区域对象
- "自动设置所有区域": 一键配置所有区域
- "检查设置": 验证所有设置是否正确

### 2. 调试
- 启用showDebugInfo查看控制台输出
- 在Scene视图中可以看到不同颜色的触发器范围
- 使用AudioUI实时监控音乐播放状态

### 3. 音频文件建议
- 使用循环播放的音频文件
- 确保音频文件格式兼容（.wav, .mp3, .ogg等）
- 建议音频长度在1-3分钟之间

### 4. AI音乐生成
- 获取Suno API密钥：访问 https://sunoapi.org 注册并获取API密钥
- 设置正确的API参数：
  - **model**: 选择模型版本 (V3_5, V3, V2, V1)
  - **customMode**: 是否启用自定义模式
  - **instrumental**: 是否生成纯音乐（无歌词）
  - **callBackUrl**: 可选的回调URL
- 合理设置音乐生成参数，避免过度消耗API配额
- 使用缓存功能减少重复生成
- 自定义提示词以获得更好的音乐效果

### 5. 性能优化
- 合理设置触发器大小，避免过大
- 适当调整UI更新间隔
- 使用音频压缩减少文件大小
- 启用音乐缓存减少网络请求

## 故障排除

### 常见问题
1. **音乐不播放**: 检查AudioManager是否正确设置，Player标签是否正确
2. **触发器不工作**: 检查Collider是否设置为Trigger，Player标签是否正确
3. **UI不显示**: 检查Canvas设置和UI组件连接
4. **音频文件问题**: 检查音频文件格式和导入设置
5. **AI音乐生成失败**: 检查API密钥是否正确，网络连接是否正常
6. **API配额超限**: 检查Suno API使用量，合理设置缓存

### 调试命令
在控制台中可以使用以下命令：
```csharp
// 检查AudioManager状态
Debug.Log(AudioManager.Instance.GetCurrentMusic()?.name);
Debug.Log(AudioManager.Instance.GetCurrentZoneName());

// 手动播放音乐
AudioManager.Instance.PlayMusic(yourAudioClip, 1.0f, "测试区域");

// AI音乐生成测试
AIGeneratedMusic aiGenerator = FindObjectOfType<AIGeneratedMusic>();
aiGenerator.GenerateMusicForZone("Peaceful forest music", "测试区域");

// 检查AI生成状态
Debug.Log(aiGenerator.IsGenerating());
Debug.Log(aiGenerator.GetCacheInfo());
```

## 扩展功能

系统设计为可扩展的，您可以：
- 添加更多区域
- 实现音量渐变效果
- 添加音效系统
- 实现音乐播放列表
- 添加音频混音功能
- 集成其他AI音乐生成API
- 实现音乐风格迁移
- 添加音乐情感分析
- 实现动态音乐适配 