# 结构化音乐生成系统

## 概述

这个系统实现了你导师建议的**预生成+动态选择**音乐生成策略，通过结构化的JSON数据来描述场景信息，实现低延迟的音乐播放。

## 核心特性

### 1. 结构化场景数据
- 使用JSON格式描述场景属性
- 包含环境、动作、威胁等级等维度
- 支持实时场景信息提取

### 2. 预生成+动态选择
- 提前生成多种音乐变体
- 运行时根据场景数据选择最匹配的音乐
- 实现低延迟播放

### 3. 智能匹配算法
- 基于相似度评分选择音乐
- 支持多种选择策略
- 自动缓存管理

## 系统架构

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  SceneDataExtractor │    │  MusicPresetManager │    │  AIGeneratedMusic │
│  (场景数据提取器)   │    │  (预设管理器)      │    │  (AI音乐生成器)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  MusicSceneData  │    │  MusicPreset     │    │  AudioManager   │
│  (场景数据结构)   │    │  (音乐预设)      │    │  (音频管理器)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 文件说明

### 核心脚本

1. **MusicSceneData.cs** - 场景数据结构定义
   - 定义场景的所有属性（环境、动作、威胁等）
   - 提供JSON序列化/反序列化
   - 计算场景强度和生成音乐描述

2. **MusicPresetManager.cs** - 音乐预设管理器
   - 管理预生成的音乐片段
   - 实现智能匹配算法
   - 处理预设的保存和加载

3. **SceneDataExtractor.cs** - 场景数据提取器
   - 从Unity场景中自动提取信息
   - 分析环境、敌人、玩家状态
   - 实时更新场景数据

4. **StructuredMusicUI.cs** - 用户界面
   - 提供可视化的场景数据编辑
   - 管理预设和实时监控
   - 支持键盘快捷键

### 更新的脚本

5. **AIGeneratedMusic.cs** - 增强的AI音乐生成器
   - 支持结构化场景数据
   - 集成预设选择系统
   - 保持向后兼容性

6. **ZoneTrigger.cs** - 更新的区域触发器
   - 支持结构化数据模式
   - 保持传统模式兼容性

## 使用方法

### 1. 基础设置

#### 场景设置
1. 在场景中创建以下GameObject：
   ```
   - AudioManager (添加AudioManager.cs)
   - AIGeneratedMusic (添加AIGeneratedMusic.cs)
   - MusicPresetManager (添加MusicPresetManager.cs)
   - SceneDataExtractor (添加SceneDataExtractor.cs)
   - StructuredMusicUI (添加StructuredMusicUI.cs)
   ```

2. 配置Suno API密钥：
   - 在AIGeneratedMusic组件中设置你的API密钥
   - 确保网络连接正常

#### 区域设置
1. 为每个区域添加ZoneTrigger组件
2. 启用`useStructuredData`选项
3. 配置`sceneData`属性

### 2. 场景数据配置

#### 手动配置
```csharp
// 创建场景数据
MusicSceneData sceneData = new MusicSceneData
{
    sceneName = "森林探索",
    environment = EnvironmentType.Forest,
    currentAction = ActionType.Walking,
    enemyPresence = EnemyPresence.None,
    gameLevel = GameLevel.Tutorial,
    timeOfDay = TimeOfDay.Day,
    threatLevel = 0.1f,
    actionIntensity = 0.3f
};
```

#### 自动提取
SceneDataExtractor会自动分析场景：
- 通过地形高度判断环境类型
- 通过GameObject标签识别环境元素
- 通过玩家速度判断动作类型
- 通过敌人数量和距离计算威胁等级

### 3. 预设管理

#### 生成预设
```csharp
// 手动生成预设
MusicPresetManager.Instance.GeneratePresetBatch();

// 创建特定预设
MusicSceneData data = new MusicSceneData { /* 配置 */ };
AIGeneratedMusic.Instance.GenerateMusicForScene(data);
```

#### 预设选择
系统会自动选择最匹配的预设：
1. 计算场景相似度
2. 选择相似度最高的预设
3. 如果没有匹配的预设，选择强度最接近的

### 4. 实时使用

#### 区域触发
当玩家进入区域时，系统会：
1. 提取当前场景数据
2. 查找匹配的音乐预设
3. 如果没有预设，使用AI生成
4. 播放选择的音乐

#### 动态更新
SceneDataExtractor会定期更新场景数据：
- 监控玩家状态变化
- 检测敌人位置变化
- 更新威胁等级

## 配置示例

### 探索场景
```json
{
  "sceneName": "和平探索",
  "environment": "Grasslands",
  "currentAction": "Walking",
  "enemyPresence": "None",
  "gameLevel": "Tutorial",
  "timeOfDay": "Day",
  "threatLevel": 0.0,
  "actionIntensity": 0.2
}
```

### 战斗场景
```json
{
  "sceneName": "激烈战斗",
  "environment": "DarkDungeon",
  "currentAction": "Combat",
  "enemyPresence": "Many",
  "gameLevel": "Mid",
  "timeOfDay": "Night",
  "threatLevel": 0.8,
  "actionIntensity": 0.9
}
```

### Boss战场景
```json
{
  "sceneName": "Boss战",
  "environment": "DarkDungeon",
  "currentAction": "Combat",
  "enemyPresence": "Boss",
  "gameLevel": "FinalBoss",
  "timeOfDay": "Night",
  "threatLevel": 1.0,
  "actionIntensity": 1.0,
  "isBossFight": true
}
```

## UI使用

### 快捷键
- **F1** - 切换UI显示
- **F2** - 手动提取场景数据
- **F3** - 播放当前音乐

### 面板功能

#### 场景数据面板
- 环境类型选择
- 动作类型选择
- 威胁等级调节
- 强度滑块控制

#### 预设管理面板
- 查看预设统计
- 批量生成预设
- 清除所有预设

#### 实时数据面板
- 显示当前场景信息
- 实时威胁等级
- 手动触发功能

#### 调试面板
- 显示详细系统状态
- JSON数据查看
- 错误信息显示

## 高级功能

### 1. 自定义匹配算法
```csharp
// 修改相似度计算权重
private float CalculateSimilarityScore(MusicSceneData data1, MusicSceneData data2)
{
    // 自定义权重配置
    float envWeight = 0.4f;    // 环境权重
    float actionWeight = 0.3f; // 动作权重
    float threatWeight = 0.2f; // 威胁权重
    float timeWeight = 0.1f;   // 时间权重
    
    // 实现自定义计算逻辑
}
```

### 2. 扩展场景属性
```csharp
// 在MusicSceneData中添加新属性
public enum NewProperty
{
    Value1, Value2, Value3
}

public NewProperty newProperty = NewProperty.Value1;
```

### 3. 自定义提取器
```csharp
// 继承SceneDataExtractor并重写方法
public class CustomDataExtractor : SceneDataExtractor
{
    protected override void AnalyzeEnvironment()
    {
        // 自定义环境分析逻辑
    }
}
```

## 性能优化

### 1. 缓存策略
- 音乐片段自动缓存
- 预设数据持久化
- 相似度计算结果缓存

### 2. 异步处理
- 音乐生成异步执行
- UI更新非阻塞
- 数据提取后台运行

### 3. 内存管理
- 自动清理过期缓存
- 预设数量限制
- 资源池化管理

## 故障排除

### 常见问题

1. **API连接失败**
   - 检查网络连接
   - 验证API密钥
   - 确认API端点正确

2. **预设匹配失败**
   - 检查预设数据完整性
   - 调整相似度阈值
   - 增加预设数量

3. **场景数据提取错误**
   - 检查GameObject标签
   - 验证组件配置
   - 查看调试日志

### 调试技巧

1. 启用调试模式
2. 查看Console日志
3. 使用UI调试面板
4. 检查JSON数据格式

## 扩展建议

### 1. 集成其他AI服务
- 支持多种AI音乐生成API
- 实现音乐风格迁移
- 添加音乐情感分析

### 2. 增强场景分析
- 集成机器学习模型
- 支持更复杂的环境识别
- 添加玩家行为预测

### 3. 优化用户体验
- 添加音乐预览功能
- 实现音乐混合过渡
- 支持用户偏好设置

## 总结

这个结构化音乐生成系统实现了你导师建议的核心概念：

1. **结构化数据驱动** - 使用JSON描述场景信息
2. **预生成+动态选择** - 提前生成音乐，运行时选择
3. **低延迟播放** - 通过预设匹配实现快速响应
4. **设计师友好** - 提供直观的UI界面
5. **可扩展架构** - 支持自定义和扩展

系统既保持了原有功能的兼容性，又提供了新的结构化工作流程，为游戏音频设计提供了更强大和灵活的工具。 