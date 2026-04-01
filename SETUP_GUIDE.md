# Fitness3DModule 配置指南

## 📋 目录

- [系统概述](#系统概述)
- [快速开始](#快速开始)
- [详细配置](#详细配置)
- [坐标系转换](#坐标系转换)
- [常见问题](#常见问题)

---

## 系统概述

### 项目目标

使用 **MediaPipe Holistic** 检测人体姿态，通过 **VNectModel** 算法将动作映射到 Unity 3D 角色。

### 核心组件

1. **MediaPipe Holistic** - Google 的人体检测库
2. **VNectModel** - 从 ThreeDPoseTracker 移植的骨骼映射算法
3. **VNectModelAdapter** - 连接 MediaPipe 和 VNectModel 的适配器

---

## 快速开始

### 步骤 1：准备 3D 角色

确保你的 3D 角色模型配置为 **Humanoid** 类型：

```
1. 在 Unity Project 窗口中选择你的角色模型 FBX 文件
2. 在 Inspector 中，将 Animation Type 设置为 Humanoid
3. 点击 Apply
4. 检查 Configure 按钮，确保骨骼映射正确
5. 将模型拖到场景中
```

### 步骤 2：配置 HolisticTrackingSolution

1. 选择场景中的 `HolisticTrackingSolution` 对象
2. 在 **Holistic Tracking Solution** 组件中：

   - **Running Mode**：选择 **Sync**⭐ 重要
   - **Texture**：选择你的摄像头纹理

### 步骤 3：添加 VNectModel 组件

1. 在场景中选择你的角色对象（例如：`Character`）
2. Add Component → **VNectModel**
3. 配置字段：
   - **Model Object**：拖拽角色对象（自身）
   - **Nose**：创建一个空对象 "Nose"，位置在头部前方
   - **Skeleton Material**：默认为空

### 步骤 4：添加 VNectModelAdapter 组件

1. 选择场景中的 `HolisticTrackingSolution` 对象
2. Add Component → **VNectModelAdapter**
3. 配置字段：
   - **Vnect Model**：拖拽步骤 3 中的角色对象
   - **Model Scale**：`1.0`
   - **Z Scale**：`0.8`
   - **Flip X**：✅ 勾选
   - **Flip Y**：✅ 勾选
   - **Flip Z**：✅ 勾选
   - **Vertical Offset**：`1.0`
   - **Auto Initialize**：✅ 勾选
   - **Auto Disable Animator**：✅ 勾选

### 步骤 5：连接组件

在 `HolisticTrackingSolution` 的 **Holistic Tracking Solution** 组件中：

- **Vnect Adapter**：拖拽 VNectModelAdapter 组件

### 步骤 6：运行场景

1. 保存场景
2. 点击 Play
3. 角色应该跟随你的动作！

---

## 配置注意事项

### 1. Holistic 模式设置

**重要**：**使用 Sync 模式**。

**原因**：

- **Async 模式**：会导致模型无法运行

---

### 2. 坐标轴翻转配置

**当前配置**：

```
Flip X: ✅ 勾选
Flip Y: ✅ 勾选
Flip Z: ✅ 勾选
```

**为什么需要翻转？**

| 坐标轴      | MediaPipe Holistic | Unity Humanoid    | 需要翻转？ | 说明             |
| ----------- | ------------------ | ----------------- | ---------- | ---------------- |
| **X** | 右（+） / 左（-）  | 右（+） / 左（-） | ✅ 是      | 修复镜像问题     |
| **Y** | 下（+） / 上（-）  | 上（+） / 下（-） | ✅ 是      | MediaPipe Y 向下 |
| **Z** | 前（+） / 后（-）  | 前（+） / 后（-） | ✅ 是      | Z 轴方向相反     |

**示例**：

```
MediaPipe 检测：
  X: 0.1（右边）
  Y: -0.5（上方）
  Z: 0.3（前方）

翻转后（Unity 坐标）：
  X: -0.1（左边）← 镜像
  Y: 0.5（上方）  ← 正确
  Z: -0.3（后方）← 反转
```

**为什么人物会背对你？**

翻转后的人物是"镜像"的，所以看起来是背对你。

**解决方案**：

```
方法1：旋转角色 180 度
1. 停止 Play
2. 选择角色对象
3. Transform Rotation Y: 180
4. 保存场景
```

---

### 3. 禁用 Animator

**必须禁用 Animator！**

**原因**：

- 我们使用 **代码控制骨骼**（通过 VNectModel）
- Animator 会干扰代码控制的骨骼
- 两者同时控制会导致冲突

**两种禁用方法**：

**方法 A：自动禁用（推荐）** ✅

```
VNectModelAdapter 组件
└─ Auto Disable Animator: ✅ 勾选
```

**方法 B：手动禁用**

```
1. 选择角色对象
2. 在 Animator 组件中
3. 取消勾选 "Animator" 复选框
4. 保存场景
```

---

### 4. Nose 对象配置

**Nose 对象的作用**：

- 用于计算头部旋转的参考点
- 独立放置在场景中，不要配置为角色的一部分
- 对于Nose位置不符合人物模型的情况请手动整至合适

**设置方法**：

```
1. 在场景中创建空对象
2. 命名为 "Nose"
3. 设置位置（默认的character人物模型位置）：
   X: 0（正中间）
   Y: 1.6（头部上方）
   Z: 0.3（头部前方）
4. 拖拽到 VNectModel 的 Nose 字段
```

---

## 坐标系转换

### MediaPipe → Unity 转换流程

```
MediaPipe Holistic 检测
       ↓
原始坐标（MediaPipe 坐标系）
  X: 右（+）/ 左（-）
  Y: 下（+）/ 上（-）  ← 注意：Y 向下
  Z: 前（+）/ 后（-）
       ↓
VNectModelAdapter.GetLandmarkVector()
  ├─ X 轴翻转：flipX = true
  ├─ Y 轴翻转：flipY = true  ← Y 向上
  ├─ Z 轴翻转：flipZ = true
  ├─ 缩放：modelScale * zScale
  └─ 偏移：verticalOffset
       ↓
Unity 世界坐标
  X: 右（+）/ 左（-）
  Y: 上（+）/ 下（-）  ← Unity Y 向上
  Z: 前（+）/ 后（-）
```

### 转换代码

```csharp
private Vector3 GetLandmarkVector(LandmarkList landmarks, MediaPipePoseLandmark landmarkIndex)
{
    var landmark = landmarks.Landmark[idx];

    float x = landmark.X * modelScale;
    float y = landmark.Y * modelScale;
    float z = landmark.Z * modelScale * zScale;

    // 翻转坐标轴
    if (flipX) x = -x;  // 修复镜像
    if (flipY) y = -y;  // Y 向上
    if (flipZ) z = -z;  // Z 轴方向

    // 添加垂直偏移
    y += verticalOffset;

    return new Vector3(x, y, z);
}
```

---

## 常见问题

### Q1：人物完全不动

**可能原因**：

- ❌ VNectModel 未初始化
- ❌ Animator 未禁用
- ❌ Vnect Adapter 未连接

**解决方法**：

```
1. 检查 Console 是否有 "VNectModel 已初始化" 日志
2. 确认 Animator 已禁用（勾选 Auto Disable Animator）
3. 确认 HolisticTrackingSolution 的 Vnect Adapter 已连接
```

---

### Q2：人物动作是镜像的（左右相反）

**原因**：`Flip X` 未勾选

**解决方法**：

```
VNectModelAdapter 组件
└─ Flip X: ✅ 勾选
```

---

### Q3：人物倒立（头部朝下）

**原因**：`Flip Y` 未勾选

**解决方法**：

```
VNectModelAdapter 组件
└─ Flip Y: ✅ 勾选
```

---

### Q4：人物背对摄像机

**原因**：坐标翻转后的正常现象

**解决方法**：

```
1. 停止 Play
2. 选择角色对象
3. Transform Rotation Y: 180
4. 保存场景
```

---

### Q5：头部倒扣或位置异常

**可能原因**：

- ❌ Nose 对象位置错误
- ❌ head.Pos3D 计算错误

**解决方法**：

```
1. 检查 Nose 对象位置：
   - 应该在头部前方（Z > 0）
   - 应该在头部上方（Y > 头部）

2. 确保 FilpZ = true 
```

---

### Q7：Unity 报错 "Animator enabled"

**原因**：Animator 仍然启用

**解决方法**：

```
VNectModelAdapter 组件
└─ Auto Disable Animator: ✅ 勾选

或手动禁用角色的 Animator 组件
```

---

## 数据流图

```
MediaPipe Holistic（摄像头检测）
       ↓
OnPoseWorldLandmarksOutput（回调函数）
       ↓
VNectModelAdapter.UpdateFromHolistic()
       ├─ GetLandmarkVector()（坐标转换）
   │   ├─ Flip X/Y/Z
   │   ├─ Scale
   │   └─ Offset
       ├─ 更新 jointPoints[].Pos3D
       ├─ 设置 Visibled = true
       └─ VNectModel.IsPoseUpdate = true（标记）
       ↓
VNectModel.Update()（每帧检查）
       ├─ 检测 IsPoseUpdate
       ├─ PoseUpdate()
   │   ├─ 计算髋部位置和旋转
   │   ├─ 计算各个骨骼的旋转
   │   └─ 修改 Transform.rotation
       └─ 3D 角色动起来！
```

---

## 诊断工具

使用 **VNectModelDiagnostics** 检查系统状态：

```
1. 选择 HolisticTrackingSolution 对象
2. Add Component → VNectModelDiagnostics
3. 运行场景
4. 右键组件 → "运行完整诊断"
5. 查看 Console 输出
```

**诊断输出示例**：

```
========================================
【VNectModel 诊断报告】
========================================
✅ VNectModel 已连接
✅ JointPoints 已初始化 (数量: 28)

【诊断结果】
总骨骼数: 28
有效骨骼 (Transform != null): 26/28 (92.9%)
有数据的骨骼 (Pos3D != 0): 26/28 (92.9%)
检测质量分数: 1.00
IsPoseUpdate: ✅ 活跃

【关键骨骼检查】
✅ Hips: Pos3D = (0.00, 1.06, 0.02), Score3D = 1.00
✅ Spine: Pos3D = (0.01, 1.12, 0.05), Score3D = 1.00
✅ Neck: Pos3D = (0.01, 1.40, 0.17), Score3D = 1.00
✅ Head: Pos3D = (0.01, 1.58, 0.21), Score3D = 1.00
✅ LeftShoulder: Pos3D = (0.18, 1.40, 0.16), Score3D = 1.00
✅ RightShoulder: Pos3D = (-0.15, 1.40, 0.17), Score3D = 1.00
✅ LeftElbow: Pos3D = (0.18, 1.22, 0.12), Score3D = 1.00
✅ RightElbow: Pos3D = (-0.24, 1.24, 0.19), Score3D = 1.00

【诊断建议】
✅ 所有检查通过！
========================================
```

---

## 进阶配置

### 调整角色高度

如果角色位置太高或太低：

```
VNectModelAdapter 组件
└─ Vertical Offset: 1.0（增加 = 更高，减少 = 更低）
```

### 调整动作幅度

如果角色动作幅度太大或太小：

```
VNectModelAdapter 组件
├─ Model Scale: 1.0（增加 = 幅度更大）
└─ Z Scale: 0.8（增加 = 前后移动幅度更大）
```

### 调试模式

查看实时数据：

```
VNectModelAdapter 组件
└─ Show Debug Info: ✅ 勾选
```

每 60 帧会在 Console 输出 Hips 位置。

---

## 技术细节

### VNectModel 算法来源

- **原始项目**：[ThreeDPoseTracker](https://github.com/HTDithub/ThreeDPoseTracker)
- **核心文件**：VNectModel.cs
- **算法**：InverseRotation 系统 + 父子关系 + Quaternion.LookRotation

### MediaPipe 关节点映射

| MediaPipe Holistic  | VNectModel   | Unity HumanBodyBones |
| ------------------- | ------------ | -------------------- |
| right_shoulder (12) | rShldrBend   | RightUpperArm        |
| right_elbow (14)    | rForearmBend | RightLowerArm        |
| right_wrist (16)    | rHand        | RightHand            |
| left_shoulder (11)  | lShldrBend   | LeftUpperArm         |
| left_elbow (13)     | lForearmBend | LeftLowerArm         |
| left_wrist (15)     | lHand        | LeftHand             |
| right_hip (24)      | rThighBend   | RightUpperLeg        |
| right_knee (26)     | rShin        | RightLowerLeg        |
| right_ankle (28)    | rFoot        | RightFoot            |
| left_hip (23)       | lThighBend   | LeftUpperLeg         |
| left_knee (25)      | lShin        | LeftLowerLeg         |
| left_ankle (27)     | lFoot        | LeftFoot             |

---

## 需要帮助？

查看 Unity Console 的错误信息，使用诊断工具检查系统状态。

如果问题仍然存在，请检查：

1. Unity 版本是否为 2022.3+
2. MediaPipe 包是否正确安装
3. 3D 角色是否配置为 Humanoid

祝你使用愉快！🎉
