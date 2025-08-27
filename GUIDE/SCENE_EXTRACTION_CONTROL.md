# 场景数据提取控制指南

## 🎯 问题描述

`SceneDataExtractor` 默认每5秒自动提取一次场景数据，这会导致：
- 控制台不断输出相同的场景数据
- 不必要的性能消耗
- 重复的事件触发

## 🔧 解决方案

### 1. **禁用周期性提取（推荐）**
在 `SceneDataExtractor` 组件中：
- 将 `Enable Periodic Extraction` 设置为 `false`
- 这样只会在启动时提取一次，之后手动控制

### 2. **只在数据变化时提取**
启用 `Only Extract On Change` 选项：
- 系统会智能检测场景数据是否发生变化
- 只有在数据真正变化时才会输出日志和触发事件
- 避免重复的相同数据输出

### 3. **调整提取间隔**
如果确实需要周期性提取：
- 将 `Extraction Interval` 设置为更大的值（如30秒或60秒）
- 减少不必要的提取频率

## 🎮 运行时控制

### ContextMenu 选项
右键点击 `SceneDataExtractor` 组件，选择：

- **手动提取场景数据** - 立即提取一次
- **启用周期性提取** - 开始周期性提取
- **禁用周期性提取** - 停止周期性提取

### 代码控制
```csharp
// 获取SceneDataExtractor组件
var extractor = FindObjectOfType<SceneDataExtractor>();

// 禁用周期性提取
extractor.DisablePeriodicExtraction();

// 设置提取间隔为30秒
extractor.SetExtractionInterval(30f);

// 手动提取一次
extractor.ManualExtract();
```

## 📊 推荐配置

### 开发阶段
```
Auto Extract On Start: true
Enable Periodic Extraction: false
Only Extract On Change: true
Extraction Interval: 5
```

### 生产环境
```
Auto Extract On Start: true
Enable Periodic Extraction: false
Only Extract On Change: true
Extraction Interval: 30
```

### 调试模式
```
Auto Extract On Start: true
Enable Periodic Extraction: true
Only Extract On Change: false
Extraction Interval: 5
```

## 🚀 最佳实践

1. **默认禁用周期性提取**：只在需要时启用
2. **使用变化检测**：避免重复的相同数据
3. **合理设置间隔**：根据实际需求调整
4. **手动控制**：在关键节点手动触发提取
5. **事件驱动**：通过事件系统响应场景变化

## 🔍 常见问题

### Q: 如何知道场景数据是否发生变化？
A: 启用 `Only Extract On Change` 选项，系统会自动检测并只在变化时输出

### Q: 周期性提取会影响性能吗？
A: 是的，频繁的提取会消耗性能，建议禁用或设置较长的间隔

### Q: 什么时候应该手动提取？
A: 在场景切换、玩家状态变化、环境改变等关键节点手动提取

### Q: 如何完全停止提取？
A: 使用 `DisablePeriodicExtraction()` 方法或设置 `Enable Periodic Extraction` 为 false
