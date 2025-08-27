# 快速开始指南 - 预设导入功能

## 修复的问题

✅ 修复了 `PresetImportHelper` 中的所有编译错误
✅ 改进了错误处理和异常捕获
✅ 添加了系统状态检查功能
✅ 简化了文件导入流程

## 快速设置步骤

### 1. 场景设置
在Unity场景中创建以下GameObject：

```
场景根目录
├── AudioManager (添加 AudioManager.cs)
├── AIGeneratedMusic (添加 AIGeneratedMusic.cs)
├── MusicPresetManager (添加 MusicPresetManager.cs)
├── PresetImportHelper (添加 PresetImportHelper.cs)
└── StructuredMusicUI (添加 StructuredMusicUI.cs)
```

### 2. 导入音频文件

#### 方法一：使用导入助手（推荐）

1. **找到导入文件夹**
   - 右键点击 `PresetImportHelper` 组件
   - 选择 "显示导入文件夹路径"
   - 复制显示的路径

2. **准备音频文件**
   - 将你从Suno下载的音频文件重命名，例如：
     - `forest_exploration.mp3`
     - `dungeon_combat.mp3`
     - `boss_battle.mp3`

3. **复制文件**
   - 将音频文件复制到导入文件夹中

4. **执行导入**
   - 右键点击 `PresetImportHelper` 组件
   - 选择 "从导入文件夹导入"

#### 方法二：手动创建

1. **找到预设文件夹**
   - 右键点击 `PresetImportHelper` 组件
   - 选择 "显示预设文件夹路径"

2. **创建文件夹结构**
   ```
   MusicPresets/
   ├── music_presets.json
   └── audio/
       ├── forest_exploration.mp3
       ├── dungeon_combat.mp3
       └── boss_battle.mp3
   ```

3. **创建JSON配置**
   - 右键点击 `PresetImportHelper` 组件
   - 选择 "创建示例JSON配置"
   - 编辑生成的JSON文件

## 文件名命名规则

系统会根据文件名自动识别场景属性：

| 关键词 | 环境 | 动作 | 强度 | 时间 | 威胁 |
|--------|------|------|------|------|------|
| forest | 森林 | - | - | - | - |
| dungeon | 地牢 | - | - | - | - |
| combat | - | 战斗 | 0.8 | - | 0.7 |
| stealth | - | 潜行 | 0.4 | - | 0.3 |
| boss | - | 战斗 | 1.0 | - | 1.0 |
| intense | - | - | 0.9 | - | 0.8 |
| calm | - | - | 0.2 | - | 0.1 |
| night | - | - | - | 夜晚 | - |

### 示例文件名
- `forest_exploration_calm_day.mp3` → 平静的森林探索
- `dungeon_combat_intense_night.mp3` → 激烈的夜间地牢战斗
- `boss_battle_epic_night.mp3` → 史诗级夜间Boss战

## 验证导入

### 1. 检查Console输出
导入成功后应该看到：
```
找到 3 个音频文件
正在导入: forest_exploration
成功导入: forest_exploration
正在导入: dungeon_combat
成功导入: dungeon_combat
正在导入: boss_battle
成功导入: boss_battle
导入完成！
```

### 2. 系统状态检查
- 右键点击 `PresetImportHelper` 组件
- 选择 "检查系统状态"
- 查看所有组件是否正常工作

### 3. 测试播放
1. 在场景中设置ZoneTrigger
2. 配置sceneData属性
3. 进入区域测试音乐播放

## 故障排除

### 常见错误及解决方案

#### 1. "MusicPresetManager 实例不存在"
**解决方案：**
- 确保场景中有MusicPresetManager GameObject
- 检查MusicPresetManager.cs脚本是否正确添加

#### 2. "导入文件夹不存在"
**解决方案：**
- 使用"显示导入文件夹路径"功能自动创建
- 或手动创建文件夹

#### 3. "没有找到音频文件"
**解决方案：**
- 检查文件格式（支持MP3, WAV, OGG）
- 确认文件已复制到正确位置
- 检查文件名是否正确

#### 4. "导入文件失败"
**解决方案：**
- 检查文件是否被其他程序占用
- 确认有足够的磁盘空间
- 检查文件权限

### 调试技巧

1. **启用调试信息**
   - 在PresetImportHelper组件中勾选"showDebugInfo"

2. **查看详细日志**
   - 打开Unity Console窗口
   - 查看所有导入过程的日志

3. **分步测试**
   - 先导入一个文件测试
   - 确认成功后批量导入

## 高级功能

### 1. 自定义场景数据
你可以手动编辑JSON文件来精确控制场景数据：

```json
{
  "sceneData": {
    "environment": "Forest",
    "currentAction": "Walking",
    "enemyPresence": "None",
    "gameLevel": "Tutorial",
    "timeOfDay": "Day",
    "threatLevel": 0.1,
    "actionIntensity": 0.3
  }
}
```

### 2. 批量导入
1. 将所有音频文件放入导入文件夹
2. 使用导入助手的批量导入功能
3. 系统会自动为每个文件创建预设

### 3. 备份和恢复
1. 定期备份 `MusicPresets` 文件夹
2. 可以复制到其他项目使用
3. 支持跨平台迁移

## 快捷键

在游戏运行时：
- **F1** - 切换音乐系统UI
- **F2** - 手动提取场景数据
- **F3** - 播放当前音乐

## 总结

现在你应该能够：
✅ 成功导入音频文件到预设系统
✅ 自动识别文件名中的场景信息
✅ 享受低延迟的音乐播放
✅ 使用智能匹配算法选择最合适的音乐

如果还有问题，请检查Console输出并参考故障排除部分！ 