# 实验数据包（Experiment Data Package）

本目录提供三类可直接使用的数据，帮助评估你的自适应音乐系统：
- static_states.csv：单帧状态表（像 Excel）。用于检验“选得准不准”。
- trajectories/*.json：三条 2–3 分钟的脚本化剧情轨迹。用于检验“切得稳不稳、快不快”。
- subjective_clips_plan.csv：主观试听裁剪计划（方便统一收集听感评分）。

快速使用：
1) 静态准确率
- 打开 static_states.csv，在 targetNotes 一列补上你认为“合适的音乐说明/标签”（未来也可替换为 presetID）。
- 将每行状态喂给系统，记录系统选中的音乐，与 targetNotes 对比即可得到准确率/Top-K。

2) 轨迹回放
- 依次读取 trajectories 下的 JSON，按顺序播放每个 segment（start+duration），在段切换时更新场景状态。
- 同时记录：时间戳、状态、最高分、选中曲目、是否触发回退、开始播放时间，用于计算切换次数、驻留时间与端到端延迟（状态变化→可听到）。

3) 主观试听
- 按 subjective_clips_plan.csv 从轨迹录音中裁 20–30 秒片段；涉及切换的片段，前后各保留 3–5 秒。
- 让 5–10 位同学为每段打两项分：Scene Fit（1–5）与 Transition Smoothness（1–5）。

说明：
- 枚举名与工程一致：EnvironmentType/TimeOfDay/WeatherType/ActionType/EnemyPresence/GameLevel。
- 浮点取值范围 [0,1]；可自行修改制造“临界/极端”样本。
- 可直接在 CSV/JSON 末尾追加更多样本，流程不受影响。
