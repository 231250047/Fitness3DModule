using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VNectModel - 从 ThreeDPoseTracker 项目移植
/// 用于将 MediaPipe/Holistic 的姿态数据映射到 Unity 3D 角色
///
/// 移植说明：
/// 1. 移除了原始的 VNect 网络相关代码
/// 2. 保留了核心的骨骼映射算法
/// 3. 通过外部更新 jointPoints[].Pos3D 来驱动
/// </summary>

public enum PositionIndex : int
{
    rShldrBend = 0,
    rForearmBend,
    rHand,
    rThumb2,
    rMid1,

    lShldrBend,
    lForearmBend,
    lHand,
    lThumb2,
    lMid1,

    lEar,
    lEye,
    rEar,
    rEye,
    Nose,

    rThighBend,
    rShin,
    rFoot,
    rToe,

    lThighBend,
    lShin,
    lFoot,
    lToe,

    abdomenUpper,

    //Calculated coordinates
    hip,
    head,
    neck,
    spine,

    Count,
    None,
}

public static partial class EnumExtend
{
    public static int Int(this PositionIndex i)
    {
        return (int)i;
    }
}

public class VNectModel : MonoBehaviour
{

    public class JointPoint  // JointPoint 类
    {
        public PositionIndex Index;

        public Vector3 Pos3D = new Vector3();  // MediaPipe的3D坐标

        public float Score3D; // 置信度
        public bool Visibled;
        public int Error;
        public bool UpperBody;
        public bool Lock;

        // Unity 骨骼绑定 
        public Transform Transform = null; // 对应的 Unity 骨骼 Transform
        public Quaternion InitRotation;   // 初始旋转 
        public Quaternion Inverse;   // 校正旋转的逆矩阵 
        public Quaternion InverseRotation;  // 组合：Inverse * InitRotation

        // 骨骼关系
        public JointPoint Child = null;   // 子关节 
        public JointPoint Parent = null;   // 父关节
    }

    public class Skeleton
    {
        public GameObject LineObject;
        public LineRenderer Line;

        public JointPoint start = null;
        public JointPoint end = null;
        public bool upperBody = false;
    }

    private List<Skeleton> Skeletons = new List<Skeleton>();
    public Material SkeletonMaterial;

    public float SkeletonX;
    public float SkeletonY;
    public float SkeletonZ;
    public float SkeletonScale;

    // Joint position and bone
    private JointPoint[] jointPoints;
    public JointPoint[] JointPoints { get { return jointPoints; } }

    private Vector3 initPosition; // Initial center position

    private Quaternion InitGazeRotation;
    private Quaternion gazeInverse;

    // UnityChan
    public GameObject ModelObject;
    public GameObject Nose;
    private Animator anim;

    private float movementScale = 0.01f;
    private float centerTall = 224 * 0.75f;
    private float tall = 224 * 0.75f;
    private float prevTall = 224 * 0.75f;
    public float ZScale = 0.8f;

    private bool LockFoot = false;
    private bool LockLegs = false;
    private bool LockHand = false;
    private float FootIKY = 0f;
    private float ToeIKY = 0f;

    private bool UpperBodyMode = false;
    private float UpperBodyF = 1f;

    /**** Foot IK ****/
    [SerializeField]
    private bool useIK = true;
    // IK 是否有效控制角度
    [SerializeField]
    private bool useIKRot = true;
    // 右脚权重
    private float rightFootWeight = 0f;
    // 左脚权重
    private float leftFootWeight = 0f;
    // 右脚位置
    private Vector3 rightFootPos;
    // 左脚位置
    private Vector3 leftFootPos;
    // 右脚角度
    private Quaternion rightFootRot;
    // 左脚角度
    private Quaternion leftFootRot;
    // 双脚距离
    private float distance;
    // 脚部贴地偏移值
    [SerializeField]
    private float offset = 0.1f;
    // 玩家者中心位置
    private Vector3 defaultCenter;
    // 射线检测距离
    [SerializeField]
    private float rayRange = 1f;

    // 调整玩家位置时的速度
    [SerializeField]
    private float smoothing = 100f;

    // 射线发射位置的调整值
    [SerializeField]
    private Vector3 rayPositionOffset = Vector3.up * 0.3f;


    public JointPoint[] Init(int inputImageSize, ConfigurationSetting config)
    {
        movementScale = 0.01f * 224f / inputImageSize;
        centerTall = inputImageSize * 0.75f;
        tall = inputImageSize * 0.75f;
        prevTall = inputImageSize * 0.75f;
        // 步骤 1: 创建关节点数组
        jointPoints = new JointPoint[PositionIndex.Count.Int()];
        for (var i = 0; i < PositionIndex.Count.Int(); i++)
        {
            jointPoints[i] = new JointPoint();
            jointPoints[i].Index = (PositionIndex)i;
            jointPoints[i].Score3D = 1;
            jointPoints[i].UpperBody = false;
            jointPoints[i].Lock = false;
            jointPoints[i].Error = 0;
        }
        // 步骤 2: 绑定 Unity 骨骼 
        anim = ModelObject.GetComponent<Animator>();
        // 将 MediaPipe 关节映射到 Unity HumanBodyBones
        jointPoints[PositionIndex.hip.Int()].Transform = transform;

        // Right Arm
        jointPoints[PositionIndex.rShldrBend.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        jointPoints[PositionIndex.rForearmBend.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        jointPoints[PositionIndex.rHand.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightHand);
        jointPoints[PositionIndex.rThumb2.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
        jointPoints[PositionIndex.rMid1.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        // Left Arm
        jointPoints[PositionIndex.lShldrBend.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        jointPoints[PositionIndex.lForearmBend.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        jointPoints[PositionIndex.lHand.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        jointPoints[PositionIndex.lThumb2.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
        jointPoints[PositionIndex.lMid1.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
        // Face
        jointPoints[PositionIndex.lEar.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        jointPoints[PositionIndex.lEye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftEye);
        jointPoints[PositionIndex.rEar.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        jointPoints[PositionIndex.rEye.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightEye);
        jointPoints[PositionIndex.Nose.Int()].Transform = Nose.transform;

        // Right Leg
        jointPoints[PositionIndex.rThighBend.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        jointPoints[PositionIndex.rShin.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        jointPoints[PositionIndex.rFoot.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        jointPoints[PositionIndex.rToe.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.RightToes);

        // Left Leg
        jointPoints[PositionIndex.lThighBend.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        jointPoints[PositionIndex.lShin.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        jointPoints[PositionIndex.lFoot.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        jointPoints[PositionIndex.lToe.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.LeftToes);

        // etc
        jointPoints[PositionIndex.abdomenUpper.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Spine);
        jointPoints[PositionIndex.head.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        jointPoints[PositionIndex.hip.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Hips);
        jointPoints[PositionIndex.neck.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Neck);
        jointPoints[PositionIndex.spine.Int()].Transform = anim.GetBoneTransform(HumanBodyBones.Spine);

        // UpperBody Settings
        jointPoints[PositionIndex.hip.Int()].UpperBody = true;
        // Right Arm
        jointPoints[PositionIndex.rShldrBend.Int()].UpperBody = true;
        jointPoints[PositionIndex.rForearmBend.Int()].UpperBody = true;
        jointPoints[PositionIndex.rHand.Int()].UpperBody = true;
        jointPoints[PositionIndex.rThumb2.Int()].UpperBody = true;
        jointPoints[PositionIndex.rMid1.Int()].UpperBody = true;
        // Left Arm
        jointPoints[PositionIndex.lShldrBend.Int()].UpperBody = true;
        jointPoints[PositionIndex.lForearmBend.Int()].UpperBody = true;
        jointPoints[PositionIndex.lHand.Int()].UpperBody = true;
        jointPoints[PositionIndex.lThumb2.Int()].UpperBody = true;
        jointPoints[PositionIndex.lMid1.Int()].UpperBody = true;
        // Face
        jointPoints[PositionIndex.lEar.Int()].UpperBody = true;
        jointPoints[PositionIndex.lEye.Int()].UpperBody = true;
        jointPoints[PositionIndex.rEar.Int()].UpperBody = true;
        jointPoints[PositionIndex.rEye.Int()].UpperBody = true;
        jointPoints[PositionIndex.Nose.Int()].UpperBody = true;
        // etc
        jointPoints[PositionIndex.spine.Int()].UpperBody = true;
        jointPoints[PositionIndex.neck.Int()].UpperBody = true;

        // Parent and Child Settings
        // Right Arm
        jointPoints[PositionIndex.rShldrBend.Int()].Child = jointPoints[PositionIndex.rForearmBend.Int()];
        jointPoints[PositionIndex.rForearmBend.Int()].Child = jointPoints[PositionIndex.rHand.Int()];
        if (config.ElbowAxisTop == 0)
        {
            jointPoints[PositionIndex.rForearmBend.Int()].Parent = jointPoints[PositionIndex.rShldrBend.Int()];
        }

        // Left Arm
        jointPoints[PositionIndex.lShldrBend.Int()].Child = jointPoints[PositionIndex.lForearmBend.Int()];
        jointPoints[PositionIndex.lForearmBend.Int()].Child = jointPoints[PositionIndex.lHand.Int()];
        if (config.ElbowAxisTop == 0)
        {
            jointPoints[PositionIndex.lForearmBend.Int()].Parent = jointPoints[PositionIndex.lShldrBend.Int()];
        }

        // Right Leg
        jointPoints[PositionIndex.rThighBend.Int()].Child = jointPoints[PositionIndex.rShin.Int()];
        jointPoints[PositionIndex.rShin.Int()].Child = jointPoints[PositionIndex.rFoot.Int()];
        jointPoints[PositionIndex.rFoot.Int()].Child = jointPoints[PositionIndex.rToe.Int()];
        jointPoints[PositionIndex.rFoot.Int()].Parent = jointPoints[PositionIndex.rShin.Int()];

        // Left Leg
        jointPoints[PositionIndex.lThighBend.Int()].Child = jointPoints[PositionIndex.lShin.Int()];
        jointPoints[PositionIndex.lShin.Int()].Child = jointPoints[PositionIndex.lFoot.Int()];
        jointPoints[PositionIndex.lFoot.Int()].Child = jointPoints[PositionIndex.lToe.Int()];
        jointPoints[PositionIndex.lFoot.Int()].Parent = jointPoints[PositionIndex.lShin.Int()];

        // etc (spine, neck, head)
        jointPoints[PositionIndex.spine.Int()].Child = jointPoints[PositionIndex.neck.Int()];
        jointPoints[PositionIndex.neck.Int()].Child = jointPoints[PositionIndex.head.Int()];

        // Line Child Settings
        // Right Arm
        AddSkeleton(PositionIndex.rShldrBend, PositionIndex.rForearmBend, true);
        AddSkeleton(PositionIndex.rForearmBend, PositionIndex.rHand, true);
        AddSkeleton(PositionIndex.rHand, PositionIndex.rThumb2, true);
        AddSkeleton(PositionIndex.rHand, PositionIndex.rMid1, true);

        // Left Arm
        AddSkeleton(PositionIndex.lShldrBend, PositionIndex.lForearmBend, true);
        AddSkeleton(PositionIndex.lForearmBend, PositionIndex.lHand, true);
        AddSkeleton(PositionIndex.lHand, PositionIndex.lThumb2, true);
        AddSkeleton(PositionIndex.lHand, PositionIndex.lMid1, true);

        // Face
        AddSkeleton(PositionIndex.lEar, PositionIndex.Nose, true);
        AddSkeleton(PositionIndex.rEar, PositionIndex.Nose, true);

        // Right Leg
        AddSkeleton(PositionIndex.rThighBend, PositionIndex.rShin, false);
        AddSkeleton(PositionIndex.rShin, PositionIndex.rFoot, false);
        AddSkeleton(PositionIndex.rFoot, PositionIndex.rToe, false);

        // Left Leg
        AddSkeleton(PositionIndex.lThighBend, PositionIndex.lShin, false);
        AddSkeleton(PositionIndex.lShin, PositionIndex.lFoot, false);
        AddSkeleton(PositionIndex.lFoot, PositionIndex.lToe, false);

        // etc
        AddSkeleton(PositionIndex.spine, PositionIndex.neck, true);
        AddSkeleton(PositionIndex.neck, PositionIndex.head, true);
        AddSkeleton(PositionIndex.head, PositionIndex.Nose, true);
        AddSkeleton(PositionIndex.neck, PositionIndex.rShldrBend, true);
        AddSkeleton(PositionIndex.neck, PositionIndex.lShldrBend, true);
        AddSkeleton(PositionIndex.rThighBend, PositionIndex.rShldrBend, true);
        AddSkeleton(PositionIndex.lThighBend, PositionIndex.lShldrBend, true);
        AddSkeleton(PositionIndex.rShldrBend, PositionIndex.abdomenUpper, true);
        AddSkeleton(PositionIndex.lShldrBend, PositionIndex.abdomenUpper, true);
        AddSkeleton(PositionIndex.rThighBend, PositionIndex.abdomenUpper, true);
        AddSkeleton(PositionIndex.lThighBend, PositionIndex.abdomenUpper, true);
        AddSkeleton(PositionIndex.lThighBend, PositionIndex.rThighBend, true);

        // 步骤 4: 计算 InverseRotation ⭐核心算法
        // 计算身体朝向（三点法向量）
        var forward = TriangleNormal(jointPoints[PositionIndex.hip.Int()].Transform.position, jointPoints[PositionIndex.lThighBend.Int()].Transform.position, jointPoints[PositionIndex.rThighBend.Int()].Transform.position);
        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Transform != null)
            {
                jointPoint.InitRotation = jointPoint.Transform.rotation;
            }

            if (jointPoint.Child != null)
            {
                // 计算校正旋转
                jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, forward);
                // 存储初始状态
                jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.InitRotation;
                /*
                InverseRotation 的作用：
                不同 3D 模型的初始姿态不同（T-pose、A-pose 等）
                需要一个"校正矩阵"让所有模型统一到同一坐标系
                公式：最终旋转 = LookRotation(目标方向) * InverseRotation
                */
            }
        }
        var hip = jointPoints[PositionIndex.hip.Int()];
        initPosition = transform.position;
        hip.Inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
        hip.InverseRotation = hip.Inverse * hip.InitRotation;

        // For Head Rotation
        var head = jointPoints[PositionIndex.head.Int()];
        head.InitRotation = jointPoints[PositionIndex.head.Int()].Transform.rotation;
        var gaze = jointPoints[PositionIndex.Nose.Int()].Transform.position - jointPoints[PositionIndex.head.Int()].Transform.position;
        head.Inverse = Quaternion.Inverse(Quaternion.LookRotation(gaze));
        head.InverseRotation = head.Inverse * head.InitRotation;

        // // ========== 日志：头部初始化 ==========
        // var initSb = new System.Text.StringBuilder();
        // initSb.AppendLine("========================================");
        // initSb.AppendLine("【头部初始化】T-pose 时的 Unity 世界坐标");
        // initSb.AppendLine($"Nose.Transform.position: {jointPoints[PositionIndex.Nose.Int()].Transform.position}");
        // initSb.AppendLine($"head.Transform.position: {jointPoints[PositionIndex.head.Int()].Transform.position}");
        // initSb.AppendLine($"lEar.Transform.position: {jointPoints[PositionIndex.lEar.Int()].Transform.position}");
        // initSb.AppendLine($"rEar.Transform.position: {jointPoints[PositionIndex.rEar.Int()].Transform.position}");
        // initSb.AppendLine($"初始 gaze = Nose - head: {gaze}");
        // initSb.AppendLine($"初始 gaze 长度: {gaze.magnitude:F3}");
        // initSb.AppendLine($"初始 gaze 归一化: {gaze.normalized}");
        // initSb.AppendLine($"head.InitRotation: {head.InitRotation.eulerAngles}");
        // initSb.AppendLine($"head.InverseRotation: {head.InverseRotation.eulerAngles}");
        // initSb.AppendLine("========================================");
        // Debug.Log(initSb.ToString());

        var lHand = jointPoints[PositionIndex.lHand.Int()];
        var lf = TriangleNormal(lHand.Pos3D, jointPoints[PositionIndex.lMid1.Int()].Pos3D, jointPoints[PositionIndex.lThumb2.Int()].Pos3D);
        lHand.InitRotation = lHand.Transform.rotation;
        lHand.Inverse = Quaternion.Inverse(Quaternion.LookRotation(jointPoints[PositionIndex.lThumb2.Int()].Transform.position - jointPoints[PositionIndex.lMid1.Int()].Transform.position, lf));
        lHand.InverseRotation = lHand.Inverse * lHand.InitRotation;

        var rHand = jointPoints[PositionIndex.rHand.Int()];
        var rf = TriangleNormal(rHand.Pos3D, jointPoints[PositionIndex.rThumb2.Int()].Pos3D, jointPoints[PositionIndex.rMid1.Int()].Pos3D);
        rHand.InitRotation = jointPoints[PositionIndex.rHand.Int()].Transform.rotation;
        rHand.Inverse = Quaternion.Inverse(Quaternion.LookRotation(jointPoints[PositionIndex.rThumb2.Int()].Transform.position - jointPoints[PositionIndex.rMid1.Int()].Transform.position, rf));
        rHand.InverseRotation = rHand.Inverse * rHand.InitRotation;

        jointPoints[PositionIndex.hip.Int()].Score3D = 1f;
        jointPoints[PositionIndex.neck.Int()].Score3D = 1f;
        jointPoints[PositionIndex.Nose.Int()].Score3D = 1f;
        jointPoints[PositionIndex.head.Int()].Score3D = 1f;
        jointPoints[PositionIndex.spine.Int()].Score3D = 1f;

        SetPredictSetting(config);

        defaultCenter = new Vector3(transform.position.x, (jointPoints[PositionIndex.rToe.Int()].Transform.position.y + jointPoints[PositionIndex.lToe.Int()].Transform.position.y) / 2f, transform.position.z);
        FootIKY = (jointPoints[PositionIndex.rFoot.Int()].Transform.position.y + jointPoints[PositionIndex.lFoot.Int()].Transform.position.y) / 2f + 0.1f;
        ToeIKY = (jointPoints[PositionIndex.rToe.Int()].Transform.position.y + jointPoints[PositionIndex.lToe.Int()].Transform.position.y) / 2f;

        Debug.Log("[VNectModel] 初始化完成，jointPoints 数组已创建");

        return JointPoints;
    }

    public void SetNose(float x, float y, float z)
    {
        if (this.Nose == null)
        {
            this.Nose = new GameObject(this.name + "_Nose");
        }
        var ani = ModelObject.GetComponent<Animator>();
        var t = ani.GetBoneTransform(HumanBodyBones.Head);
        this.Nose.transform.position = new Vector3(t.position.x + x, t.position.y + y, t.position.z + z);

    }

    public void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetZScale(float zScale)
    {
        ZScale = zScale;
    }

    public void SetSkeleton(bool flag)
    {
        foreach (var sk in Skeletons)
        {
            sk.LineObject.SetActive(flag);
        }
    }

    public void ResetPosition(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
        initPosition = transform.position;
    }

    public Vector3 GetHeadPosition()
    {
        return anim.GetBoneTransform(HumanBodyBones.Head).position;
    }

    public void Show()
    {
    }

    public void Hide()
    {
        SetSkeleton(false);
    }

    public void SetUpperBodyMode(bool upper)
    {
        UpperBodyMode = upper;
        UpperBodyF = upper ? 0f : 1f;
    }

    public void SetPredictSetting(ConfigurationSetting config)
    {
        if (jointPoints == null)
        {
            return;
        }

        LockFoot = config.LockFoot == 1;
        LockLegs = config.LockLegs == 1;
        LockHand = config.LockHand == 1;
        jointPoints[PositionIndex.lToe.Int()].Lock = LockFoot || LockLegs;
        jointPoints[PositionIndex.rToe.Int()].Lock = LockFoot || LockLegs;
        jointPoints[PositionIndex.lFoot.Int()].Lock = LockFoot || LockLegs;
        jointPoints[PositionIndex.rFoot.Int()].Lock = LockFoot || LockLegs;
        jointPoints[PositionIndex.lShin.Int()].Lock = LockLegs;
        jointPoints[PositionIndex.rShin.Int()].Lock = LockLegs;
        jointPoints[PositionIndex.lHand.Int()].Lock = LockHand;
        jointPoints[PositionIndex.rHand.Int()].Lock = LockHand;

        if (config.ElbowAxisTop == 0)
        {
            jointPoints[PositionIndex.rForearmBend.Int()].Parent = jointPoints[PositionIndex.rShldrBend.Int()];
            jointPoints[PositionIndex.lForearmBend.Int()].Parent = jointPoints[PositionIndex.lShldrBend.Int()];
        }
        else
        {
            jointPoints[PositionIndex.rForearmBend.Int()].Parent = null;
            jointPoints[PositionIndex.lForearmBend.Int()].Parent = null;
        }
    }

    private float tallHeadNeck;
    private float tallNeckSpine;
    private float tallSpineCrotch;
    private float tallThigh;
    private float tallShin;
    public float EstimatedScore;
    private float VisibleThreshold = 0.05f;

    public void PoseUpdate()
    {
        // 从身高计算Z轴移动量
        tallHeadNeck = Vector3.Distance(jointPoints[PositionIndex.head.Int()].Pos3D, jointPoints[PositionIndex.neck.Int()].Pos3D);
        tallNeckSpine = Vector3.Distance(jointPoints[PositionIndex.neck.Int()].Pos3D, jointPoints[PositionIndex.spine.Int()].Pos3D);

        // 如果检测质量低（Score3D < 阈值），Visibled = false 这样低质量的检测不会导致骨骼乱动
        jointPoints[PositionIndex.lToe.Int()].Visibled = jointPoints[PositionIndex.lToe.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.rToe.Int()].Visibled = jointPoints[PositionIndex.rToe.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.lFoot.Int()].Visibled = jointPoints[PositionIndex.lFoot.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.rFoot.Int()].Visibled = jointPoints[PositionIndex.rFoot.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.lShin.Int()].Visibled = jointPoints[PositionIndex.lShin.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.rShin.Int()].Visibled = jointPoints[PositionIndex.rShin.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.lThighBend.Int()].Visibled = jointPoints[PositionIndex.lThighBend.Int()].Score3D < VisibleThreshold ? false : true;
        jointPoints[PositionIndex.rThighBend.Int()].Visibled = jointPoints[PositionIndex.rThighBend.Int()].Score3D < VisibleThreshold ? false : true;

        var leftShin = 0f;
        var rightShin = 0f;
        var shinCnt = 0;
        if (jointPoints[PositionIndex.lShin.Int()].Visibled && jointPoints[PositionIndex.lFoot.Int()].Visibled)
        {
            leftShin = Vector3.Distance(jointPoints[PositionIndex.lShin.Int()].Pos3D, jointPoints[PositionIndex.lFoot.Int()].Pos3D);
            shinCnt++;
        }
        if (jointPoints[PositionIndex.rShin.Int()].Visibled && jointPoints[PositionIndex.rFoot.Int()].Visibled)
        {
            rightShin = Vector3.Distance(jointPoints[PositionIndex.rShin.Int()].Pos3D, jointPoints[PositionIndex.rFoot.Int()].Pos3D);
            shinCnt++;
        }
        if (shinCnt != 0)
        {
            tallShin = (rightShin + leftShin) / shinCnt;
        }

        var rightThigh = 0f;
        var leftThigh = 0f;
        var thighCnt = 0;
        if (jointPoints[PositionIndex.rThighBend.Int()].Visibled && jointPoints[PositionIndex.rShin.Int()].Visibled)
        {
            rightThigh = Vector3.Distance(jointPoints[PositionIndex.rThighBend.Int()].Pos3D, jointPoints[PositionIndex.rShin.Int()].Pos3D);
            thighCnt++;
        }
        if (jointPoints[PositionIndex.lThighBend.Int()].Visibled && jointPoints[PositionIndex.lShin.Int()].Visibled)
        {
            leftThigh = Vector3.Distance(jointPoints[PositionIndex.lThighBend.Int()].Pos3D, jointPoints[PositionIndex.lShin.Int()].Pos3D);
            thighCnt++;
        }
        if (thighCnt != 0)
        {
            tallThigh = (rightThigh + leftThigh) / 2f;
        }

        var crotch = (jointPoints[PositionIndex.rThighBend.Int()].Pos3D + jointPoints[PositionIndex.lThighBend.Int()].Pos3D) / 2f;
        tallSpineCrotch = Vector3.Distance(jointPoints[PositionIndex.spine.Int()].Pos3D, crotch);

        if (tallThigh <= 0.01f && tallShin <= 0.01f)
        {
            tallThigh = tallNeckSpine;
            tallShin = tallNeckSpine;
        }
        else if (tallShin <= 0.01f)
        {
            tallShin = tallThigh;
        }
        else if (tallThigh <= 0.01f)
        {
            tallThigh = tallShin;
        }

        var t = tallHeadNeck + tallNeckSpine + tallSpineCrotch + (tallThigh + tallShin) * UpperBodyF;

        tall = t * 0.7f + prevTall * 0.3f;
        prevTall = tall;

        var dz = (tall / centerTall - 1f);

        var score = 0f;
        var scoreCnt = 0;
        for (var i = 0; i < 24; i++)
        {
            if (!jointPoints[i].Visibled)
            {
                continue;
            }

            if (jointPoints[i].Child != null)
            {
                score += jointPoints[i].Score3D;
                scoreCnt++;
            }
        }

        if (scoreCnt > 0)
        {
            EstimatedScore = score / scoreCnt;
        }
        else
        {
            EstimatedScore = 0f;
        }

        if (EstimatedScore < 0.03f)
        {
            return;
        }
        // 身体中心的移动和旋转
        var forward = TriangleNormal(jointPoints[PositionIndex.hip.Int()].Pos3D, jointPoints[PositionIndex.lThighBend.Int()].Pos3D, jointPoints[PositionIndex.rThighBend.Int()].Pos3D);
        transform.position = jointPoints[PositionIndex.hip.Int()].Pos3D * movementScale + new Vector3(initPosition.x, initPosition.y, initPosition.z - dz * ZScale);
        jointPoints[PositionIndex.hip.Int()].Transform.rotation = Quaternion.LookRotation(forward) * jointPoints[PositionIndex.hip.Int()].InverseRotation;

        // 更新各个骨骼的旋转
        foreach (var jointPoint in jointPoints)
        {
            if (this.UpperBodyMode && !jointPoint.UpperBody)
            {
                continue;
            }
            if (jointPoint.Lock)
            {
                if (LockLegs)
                {
                    if (jointPoint.Index == PositionIndex.lShin || jointPoint.Index == PositionIndex.rShin)
                    {
                        jointPoint.Transform.rotation = Quaternion.LookRotation(Vector3.up, forward) * jointPoint.InverseRotation;
                    }
                }
                continue;
            }
            if (!jointPoint.Visibled)
            {
                continue;
            }

            if (jointPoint.Parent != null)
            {
                var fv = jointPoint.Parent.Pos3D - jointPoint.Pos3D;
                jointPoint.Transform.rotation = Quaternion.LookRotation(jointPoint.Pos3D - jointPoint.Child.Pos3D, fv) * jointPoint.InverseRotation;
            }
            else if (jointPoint.Child != null)
            {
                if (!jointPoint.Child.Visibled)
                {
                    continue;
                }
                jointPoint.Transform.rotation = Quaternion.LookRotation(jointPoint.Pos3D - jointPoint.Child.Pos3D, forward) * jointPoint.InverseRotation;
            }

        }



        // Head Rotation
        var gaze = jointPoints[PositionIndex.Nose.Int()].Pos3D - jointPoints[PositionIndex.head.Int()].Pos3D;
        var f = TriangleNormal(jointPoints[PositionIndex.Nose.Int()].Pos3D, jointPoints[PositionIndex.rEar.Int()].Pos3D, jointPoints[PositionIndex.lEar.Int()].Pos3D);
        var head = jointPoints[PositionIndex.head.Int()];
        head.Transform.rotation = Quaternion.LookRotation(gaze, f) * head.InverseRotation;

        // // ========== 日志：运行时头部旋转（每60帧输出一次）==========
        // if (Time.frameCount % 60 == 0)
        // {
        //     var runtimeSb = new System.Text.StringBuilder();
        //     runtimeSb.AppendLine("========================================");
        //     runtimeSb.AppendLine($"【运行时头部旋转】Frame: {Time.frameCount}");
        //     runtimeSb.AppendLine($"Nose.Pos3D: {jointPoints[PositionIndex.Nose.Int()].Pos3D}");
        //     runtimeSb.AppendLine($"head.Pos3D: {jointPoints[PositionIndex.head.Int()].Pos3D}");
        //     runtimeSb.AppendLine($"lEar.Pos3D: {jointPoints[PositionIndex.lEar.Int()].Pos3D}");
        //     runtimeSb.AppendLine($"rEar.Pos3D: {jointPoints[PositionIndex.rEar.Int()].Pos3D}");
        //     runtimeSb.AppendLine($"运行时 gaze = Nose - head: {gaze}");
        //     runtimeSb.AppendLine($"运行时 gaze 长度: {gaze.magnitude:F3}");
        //     runtimeSb.AppendLine($"运行时 gaze 归一化: {gaze.normalized}");
        //     runtimeSb.AppendLine($"运行时 faceNormal (f): {f}");
        //     runtimeSb.AppendLine($"运行时 faceNormal 长度: {f.magnitude:F3}");
        //     runtimeSb.AppendLine($"最终旋转 = LookRotation(gaze, f) * InverseRotation");
        //     runtimeSb.AppendLine($"目标旋转: {Quaternion.LookRotation(gaze, f).eulerAngles}");
        //     runtimeSb.AppendLine($"最终旋转: {head.Transform.rotation.eulerAngles}");
        //     runtimeSb.AppendLine("========================================");
        //     Debug.Log(runtimeSb.ToString());
        // }

        // Wrist rotation (Test code)
        var lHand = jointPoints[PositionIndex.lHand.Int()];
        if (!lHand.Lock && lHand.Visibled)
        {
            var lf = TriangleNormal(lHand.Pos3D, jointPoints[PositionIndex.lMid1.Int()].Pos3D, jointPoints[PositionIndex.lThumb2.Int()].Pos3D);
            lHand.Transform.rotation = Quaternion.LookRotation(jointPoints[PositionIndex.lThumb2.Int()].Pos3D - jointPoints[PositionIndex.lMid1.Int()].Pos3D, lf) * lHand.InverseRotation;
        }
        var rHand = jointPoints[PositionIndex.rHand.Int()];
        if (!rHand.Lock && rHand.Visibled)
        {
            var rf = TriangleNormal(rHand.Pos3D, jointPoints[PositionIndex.rThumb2.Int()].Pos3D, jointPoints[PositionIndex.rMid1.Int()].Pos3D);
            rHand.Transform.rotation = Quaternion.LookRotation(jointPoints[PositionIndex.rThumb2.Int()].Pos3D - jointPoints[PositionIndex.rMid1.Int()].Pos3D, rf) * rHand.InverseRotation;
        }

        foreach (var sk in Skeletons)
        {
            if (this.UpperBodyMode && !sk.upperBody)
            {
                continue;
            }

            var s = sk.start;
            var e = sk.end;

            sk.Line.SetPosition(0, new Vector3(s.Pos3D.x * SkeletonScale + SkeletonX, s.Pos3D.y * SkeletonScale + SkeletonY, s.Pos3D.z * SkeletonScale + SkeletonZ));
            sk.Line.SetPosition(1, new Vector3(e.Pos3D.x * SkeletonScale + SkeletonX, e.Pos3D.y * SkeletonScale + SkeletonY, e.Pos3D.z * SkeletonScale + SkeletonZ));
        }
    }

    Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    private Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.Transform.position - p2.Transform.position, forward));
    }

    private void AddSkeleton(PositionIndex s, PositionIndex e, bool upperBody)
    {
        var sk = new Skeleton()
        {
            LineObject = new GameObject(this.name + "_Skeleton" + (Skeletons.Count + 1).ToString("00")),
            start = jointPoints[s.Int()],
            end = jointPoints[e.Int()],
            upperBody = upperBody,
        };

        sk.Line = sk.LineObject.AddComponent<LineRenderer>();
        sk.Line.startWidth = 0.04f;
        sk.Line.endWidth = 0.01f;
        sk.Line.positionCount = 2;
        sk.Line.material = SkeletonMaterial;

        Skeletons.Add(sk);
    }

    public static bool IsPoseUpdate = false;

    private void Update()
    {
        if (jointPoints != null)
        {
            if (IsPoseUpdate)
            {
                PoseUpdate();
            }
            IsPoseUpdate = false;
        }
    }

    void OnAnimatorIK()
    {
        if (!useIK)
        {
            return;
        }

        rightFootWeight = 1f;
        leftFootWeight = 1f;

        Debug.DrawRay(anim.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);
        var ray = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + rayPositionOffset, -transform.up);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayRange))
        {
            rightFootPos = hit.point;

            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPos + new Vector3(0f, offset, 0f));
            if (useIKRot)
            {
                rightFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
                anim.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRot);
            }
        }

        ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up);
        Debug.DrawRay(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + rayPositionOffset, -transform.up * rayRange, Color.red);

        if (Physics.Raycast(ray, out hit, rayRange))
        {
            leftFootPos = hit.point;

            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPos + new Vector3(0f, offset, 0f));

            if (useIKRot)
            {
                leftFootRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
                anim.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRot);
            }
        }
    }
}
