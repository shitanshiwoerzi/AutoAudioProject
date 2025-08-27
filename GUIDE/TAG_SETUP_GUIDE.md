# Unity标签设置指南

## 🏷️ 需要设置的标签

为了避免 `SceneDataExtractor` 出现 "Tag is not defined" 错误，您需要在Unity中设置以下标签：

### 环境标签 (Environment Tags)
1. **Forest** - 森林环境
2. **Dungeon** - 地下城环境  
3. **Urban** - 城市环境
4. **Ocean** - 海洋环境
5. **Desert** - 沙漠环境
6. **Snow** - 雪地环境

### 天气标签 (Weather Tags)
1. **Rain** - 雨天
2. **Snow** - 下雪
3. **Storm** - 暴风雨
4. **Fog** - 雾天

### 敌人标签 (Enemy Tags)
1. **Enemy** - 普通敌人
2. **Boss** - Boss敌人

## 🔧 设置步骤

### 1. 打开标签管理器
- 在Unity菜单栏选择 `Edit` → `Project Settings`
- 选择 `Tags and Layers` 标签页

### 2. 添加新标签
- 在 `Tags` 部分，点击 `+` 按钮
- 输入标签名称（例如：Forest）
- 重复此步骤添加所有需要的标签

### 3. 应用到GameObject
- 选择场景中的GameObject
- 在Inspector中，点击 `Tag` 下拉菜单
- 选择相应的标签

## 📁 音频文件路径设置

### 推荐的项目结构：
```
Assets/
└── AutoAudioDemo/
    ├── MusicPresets/
    │   └── audio/
    │       ├── forest_exploration.mp3
    │       ├── urban_ambient.mp3
    │       ├── dungeon_atmosphere.mp3
    │       └── ...
    └── ...
```

### 音频文件命名建议：
- 使用描述性的名称
- 避免空格和特殊字符
- 使用下划线分隔单词
- 例如：`forest_exploration.mp3`, `urban_ambient.mp3`

## 🚀 快速设置脚本

如果您想要快速设置所有标签，可以创建一个编辑器脚本：

```csharp
using UnityEditor;

public class TagSetupTool
{
    [MenuItem("Tools/Setup Required Tags")]
    public static void SetupRequiredTags()
    {
        string[] requiredTags = {
            "Forest", "Dungeon", "Urban", "Ocean", "Desert", "Snow",
            "Rain", "Storm", "Fog",
            "Enemy", "Boss"
        };
        
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        
        foreach (string tag in requiredTags)
        {
            bool found = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                Debug.Log($"添加标签: {tag}");
            }
        }
        
        tagManager.ApplyModifiedProperties();
        Debug.Log("所有必需标签已设置完成！");
    }
}
```

## ⚠️ 注意事项

1. **标签名称区分大小写**：确保标签名称完全匹配
2. **标签数量限制**：Unity最多支持32个标签
3. **场景保存**：设置标签后记得保存场景
4. **项目同步**：如果使用版本控制，确保标签设置被提交

## 🔍 故障排除

### 如果仍然出现标签错误：
1. 检查标签名称是否完全匹配
2. 确认标签已正确添加到TagManager
3. 重启Unity编辑器
4. 检查脚本中的标签引用

### 如果音频文件路径错误：
1. 确认音频文件存在于正确位置
2. 检查文件名是否包含特殊字符
3. 验证文件路径是否正确
4. 检查文件权限设置
