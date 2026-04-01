using System.Collections.Generic;
using UnityEngine;
using Mediapipe;

namespace Mediapipe.Unity.Sample.Holistic
{
    /// <summary>
    /// MediaPipe Holistic 到 VNectModel 的适配器
    /// 将 MediaPipe Holistic 的 33 个关键点映射到 VNectModel 的关节点
    /// </summary>
    public class VNectModelAdapter : MonoBehaviour
    {
        [Header("VNectModel 设置")]
        [Tooltip("VNectModel 组件（从 ThreeDPoseTracker 项目复制）")]
        public VNectModel vnectModel;

        [Header("坐标设置")]
        [Tooltip("模型缩放")]
        public float modelScale = 1.0f;

        [Tooltip("Z轴缩放（深度）")]
        public float zScale = 0.8f;

        [Tooltip("X轴翻转（修复镜像问题）")]
        public bool flipX = false;

        [Tooltip("Y轴翻转（MediaPipe转换成Unity坐标系）")]
        public bool flipY = true;

        [Tooltip("Z轴翻转(默认别动)")]
        public bool flipZ = true;

        [Tooltip("垂直偏移（调整角色高度）")]
        public float verticalOffset = 1.0f;

        [Header("调试")]
        [Tooltip("是否显示调试信息")]
        public bool showDebugInfo = true;

        // 用于控制调试日志频率的计数器（线程安全）
        private static int debugLogCounter = 0;

        [Header("自动初始化")]
        [Tooltip("是否在 Start 时自动初始化 VNectModel")]
        public bool autoInitialize = true;

        [Tooltip("MediaPipe 输入图像尺寸")]
        [SerializeField] private int inputImageSize = 256;

        [Tooltip("是否自动禁用 Animator（需禁用）")]
        [SerializeField] private bool autoDisableAnimator = true;

        // MediaPipe Holistic Pose 关键点索引
        private enum MediaPipePoseLandmark
        {
            Nose = 0,
            LeftEyeInner = 1,
            LeftEye = 2,
            LeftEyeOuter = 3,
            RightEyeInner = 4,
            RightEye = 5,
            RightEyeOuter = 6,
            LeftEar = 7,
            RightEar = 8,
            MouthLeft = 9,
            MouthRight = 10,
            LeftShoulder = 11,
            RightShoulder = 12,
            LeftElbow = 13,
            RightElbow = 14,
            LeftWrist = 15,
            RightWrist = 16,
            LeftPinky = 17,
            RightPinky = 18,
            LeftIndex = 19,
            RightIndex = 20,
            LeftThumb = 21,
            RightThumb = 22,
            LeftHip = 23,
            RightHip = 24,
            LeftKnee = 25,
            RightKnee = 26,
            LeftAnkle = 27,
            RightAnkle = 28,
            LeftHeel = 29,
            RightHeel = 30,
            LeftFootIndex = 31,
            RightFootIndex = 32
        }

        void Start()
        {
            if (vnectModel == null)
            {
                Debug.LogError("【错误】VNectModel 未设置！请在 Inspector 中分配 VNectModel 组件。");
                return;
            }

            // 自动初始化 VNectModel
            if (autoInitialize)
            {
                InitializeVNectModel();
            }

            if (showDebugInfo)
            {
                Debug.Log("【VNectModelAdapter】初始化完成");
            }
        }

        /// <summary>
        /// 初始化 VNectModel
        /// </summary>
        private void InitializeVNectModel()
        {
            if (vnectModel == null)
            {
                Debug.LogError("【错误】无法初始化：VNectModel 为 null");
                return;
            }

            // 创建配置
            var config = ConfigurationSetting.CreateDefault();

            // ⭐ 关键修复：必须调用 Init() 来初始化骨骼映射！
            vnectModel.Init(inputImageSize, config);

            Debug.Log($"【VNectModelAdapter】VNectModel 已初始化 (inputSize={inputImageSize})");

            // 自动禁用 Animator
            if (autoDisableAnimator)
            {
                var animator = vnectModel.ModelObject?.GetComponent<Animator>();
                if (animator != null && animator.enabled)
                {
                    animator.enabled = false;
                    Debug.Log("【VNectModelAdapter】Animator 已自动禁用");
                }
            }

            // 设置初始位置和旋转
            vnectModel.ResetPosition(0, 0, 0);
            vnectModel.transform.rotation = Quaternion.Euler(0, 0, 0);
            vnectModel.SetScale(modelScale);
            vnectModel.SetZScale(zScale);
        }

        /// <summary>
        /// 从 MediaPipe Holistic LandmarkList 更新 VNectModel
        /// 这是主入口函数，从 HolisticTrackingSolution 调用
        /// </summary>
        public void UpdateFromHolistic(LandmarkList poseWorldLandmarks)
        {
            if (vnectModel == null || poseWorldLandmarks == null)
            {
                return;
            }

            // 获取 VNectModel 的关节点数组
            var jointPoints = vnectModel.JointPoints;
            if (jointPoints == null)
            {
                return;
            }

            // 第1步：更新所有关节的 3D 位置
            UpdateJointPositions(jointPoints, poseWorldLandmarks);

            // 第2步：设置标志位，让 VNectModel.Update() 在主线程中调用 PoseUpdate()
            // ⚠️ 不能直接调用 PoseUpdate()，因为它使用 transform（只能在主线程）
            VNectModel.IsPoseUpdate = true;
        }

        /// <summary>
        /// 更新所有关节的 3D 位置
        /// 将 MediaPipe Holistic 的 33 个关键点映射到 VNectModel 的关节点
        /// 使用与 VNectBarracudaRunner 相同的计算方法
        /// </summary>
        private void UpdateJointPositions(VNectModel.JointPoint[] jointPoints, LandmarkList landmarks)
        {
            // 获取基础关节点
            Vector3 leftShoulder = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftShoulder);
            Vector3 rightShoulder = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightShoulder);
            Vector3 leftHip = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftHip);
            Vector3 rightHip = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightHip);
            // 注意：MediaPipe 没有 LeftThigh/RightThigh，直接使用 Hip 代表大腿根部

            // 计算 abdomenUpper（估算）
            // VNect 直接检测 abdomenUpper，但 MediaPipe 没有
            // 使用髋部和肩膀的中间点，但偏向髋部（腹部在髋部上方）
            Vector3 hipsCenter = (leftHip + rightHip) * 0.5f;
            Vector3 shouldersCenter = (leftShoulder + rightShoulder) * 0.5f;
            Vector3 abdomenUpper = hipsCenter + (shouldersCenter - hipsCenter) * 0.3f;
            jointPoints[(int)PositionIndex.abdomenUpper].Pos3D = abdomenUpper;

            // 按照 VNectBarracudaRunner 的公式计算衍生关节点 
            // Calculate hip location
            // hip = (abdomenUpper + (左髋+右髋)/2) / 2
            Vector3 lc = (leftHip + rightHip) * 0.5f;
            jointPoints[(int)PositionIndex.hip].Pos3D = (abdomenUpper + lc) * 0.5f;

            // Calculate neck location
            // neck = (左肩+右肩) / 2
            jointPoints[(int)PositionIndex.neck].Pos3D = (leftShoulder + rightShoulder) * 0.5f;

            // Calculate head location
            // head = neck + 法向量 * 投影
            Vector3 leftEar = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftEar);
            Vector3 rightEar = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightEar);
            Vector3 cEar = (leftEar + rightEar) * 0.5f;
            Vector3 hv = cEar - jointPoints[(int)PositionIndex.neck].Pos3D;
            Vector3 nhv = hv.normalized;
            Vector3 nose = GetLandmarkVector(landmarks, MediaPipePoseLandmark.Nose);
            Vector3 nv = nose - jointPoints[(int)PositionIndex.neck].Pos3D;
            jointPoints[(int)PositionIndex.head].Pos3D = jointPoints[(int)PositionIndex.neck].Pos3D + nhv * Vector3.Dot(nhv, nv);

            // Calculate spine location：spine = abdomenUpper
            jointPoints[(int)PositionIndex.spine].Pos3D = abdomenUpper;

            // ========== 左臂（使用右侧MediaPipe数据修复镜像）==========

            jointPoints[(int)PositionIndex.lShldrBend].Pos3D = rightShoulder;
            jointPoints[(int)PositionIndex.lForearmBend].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightElbow);
            jointPoints[(int)PositionIndex.lHand].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightWrist);

            // 左拇指（使用 RightThumb）
            jointPoints[(int)PositionIndex.lThumb2].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightThumb);
            // 左中指（使用 RightIndex）
            jointPoints[(int)PositionIndex.lMid1].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightIndex);

            // ========== 右臂（使用左侧MediaPipe数据修复镜像）==========

            jointPoints[(int)PositionIndex.rShldrBend].Pos3D = leftShoulder;
            jointPoints[(int)PositionIndex.rForearmBend].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftElbow);
            jointPoints[(int)PositionIndex.rHand].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftWrist);

            // 右拇指（使用 LeftThumb）
            jointPoints[(int)PositionIndex.rThumb2].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftThumb);
            // 右中指（使用 LeftIndex）
            jointPoints[(int)PositionIndex.rMid1].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftIndex);

            // ========== 左腿（使用右侧MediaPipe数据修复镜像）==========

            jointPoints[(int)PositionIndex.lThighBend].Pos3D = rightHip;
            jointPoints[(int)PositionIndex.lShin].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightKnee);
            jointPoints[(int)PositionIndex.lFoot].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightAnkle);
            jointPoints[(int)PositionIndex.lToe].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightFootIndex);

            // ========== 右腿（使用左侧MediaPipe数据修复镜像）==========

            jointPoints[(int)PositionIndex.rThighBend].Pos3D = leftHip;
            jointPoints[(int)PositionIndex.rShin].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftKnee);
            jointPoints[(int)PositionIndex.rFoot].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftAnkle);
            jointPoints[(int)PositionIndex.rToe].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftFootIndex);

            // ========== 面部（左右交换修复镜像）==========

            jointPoints[(int)PositionIndex.Nose].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.Nose);
            jointPoints[(int)PositionIndex.lEar].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightEar);
            jointPoints[(int)PositionIndex.rEar].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftEar);
            jointPoints[(int)PositionIndex.lEye].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.RightEye);
            jointPoints[(int)PositionIndex.rEye].Pos3D = GetLandmarkVector(landmarks, MediaPipePoseLandmark.LeftEye);

            // !设置 Visibled = true 
            // 在每次更新数据后都必须设置 Visibled = true
            // 否则 PoseUpdate() 会跳过这些骨骼（骨骼不会动）
            foreach (var jointPoint in jointPoints)
            {
                if (jointPoint.Pos3D != Vector3.zero)
                {
                    jointPoint.Visibled = true;
                }
            }

            // 调试日志（每60帧一次）- 使用线程安全的计数器
            if (showDebugInfo)
            {
                debugLogCounter++;
                if (debugLogCounter % 60 == 0)
                {
                    Debug.Log($"【VNectModelAdapter】Hips: {jointPoints[(int)PositionIndex.hip].Pos3D}");
                }
            }
        }

        /// <summary>
        /// 从 MediaPipe LandmarkList 获取 Vector3 坐标
        /// 处理坐标系转换和缩放
        /// </summary>
        private Vector3 GetLandmarkVector(LandmarkList landmarks, MediaPipePoseLandmark landmarkIndex)
        {
            int idx = (int)landmarkIndex;

            // 边界检查
            if (idx < 0 || idx >= landmarks.Landmark.Count)
            {
                return Vector3.zero;
            }

            var landmark = landmarks.Landmark[idx];

            float x = landmark.X * modelScale;
            float y = landmark.Y * modelScale;
            float z = landmark.Z * modelScale * zScale;

            // X轴翻转（修复镜像问题）
            if (flipX)
            {
                x = -x;
            }

            // Y轴翻转（MediaPipe Y向下，Unity Y向上）
            if (flipY)
            {
                y = -y;
            }

            // Z轴翻转
            if (flipZ)
            {
                z = -z;
            }

            // 添加垂直偏移
            y += verticalOffset;

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 初始化 VNectModel（可选）
        /// 如果 autoInitialize = false，需要手动调用此方法
        /// </summary>
        public void InitializeVNect(int inputImageSize, ConfigurationSetting config)
        {
            if (vnectModel != null)
            {
                vnectModel.Init(inputImageSize, config);

                // 禁用 Animator
                if (autoDisableAnimator)
                {
                    var animator = vnectModel.ModelObject?.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.enabled = false;
                    }
                }
            }
        }
    }
}
