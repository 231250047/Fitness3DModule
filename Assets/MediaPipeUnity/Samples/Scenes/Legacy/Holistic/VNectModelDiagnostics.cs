using UnityEngine;
using System.Text;

namespace Mediapipe.Unity.Sample.Holistic
{
    /// <summary>
    /// VNectModel 诊断工具
    /// 用于检测骨骼映射、数据更新、旋转计算等问题
    /// </summary>
    public class VNectModelDiagnostics : MonoBehaviour
{
    [Header("诊断设置")]
    [SerializeField] private VNectModel vnectModel;
    [SerializeField] private VNectModelAdapter vnectAdapter;
    [SerializeField] private bool showDiagnosticsOnStart = true;
    [SerializeField] private bool showDiagnosticsEvery60Frames = true;

    [Header("诊断结果")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private int totalBones = 0;
    [SerializeField] private int validBones = 0;
    [SerializeField] private int bonesWithData = 0;
    [SerializeField] private bool isPoseUpdateActive = false;

    void Start()
    {
        if (showDiagnosticsOnStart)
        {
            Invoke("RunDiagnostics", 1f); // 延迟1秒，等待初始化完成
        }
    }

    void Update()
    {
        if (showDiagnosticsEvery60Frames && Time.frameCount % 60 == 0)
        {
            RunQuickDiagnostics();
        }
    }

    [ContextMenu("运行完整诊断")]
    public void RunDiagnostics()
    {
        Debug.Log("========================================");
        Debug.Log("【VNectModel 诊断报告】");
        Debug.Log("========================================");

        // 1. 检查 VNectModel 是否存在
        if (vnectModel == null)
        {
            Debug.Log("❌ 错误：VNectModel 未设置！");
            return;
        }
        Debug.Log("✅ VNectModel 已连接");

        // 2. 检查是否初始化
        if (vnectModel.JointPoints == null)
        {
            Debug.Log("❌ 错误：JointPoints 为 null！VNectModel 未初始化！");
            return;
        }
        Debug.Log($"✅ JointPoints 已初始化 (数量: {vnectModel.JointPoints.Length})");
        isInitialized = true;

        // 3. 检查每个骨骼的映射情况
        var jointPoints = vnectModel.JointPoints;
        totalBones = jointPoints.Length;
        validBones = 0;
        bonesWithData = 0;

        var sb = new StringBuilder();
        sb.AppendLine("\n【骨骼映射详情】");
        sb.AppendLine("格式: 骨骼名称 | Transform | Pos3D数据 | Score3D");

        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Transform != null)
            {
                validBones++;
                string hasData = jointPoint.Pos3D != Vector3.zero ? "✅" : "❌";
                float score = jointPoint.Score3D;

                if (jointPoint.Pos3D != Vector3.zero)
                {
                    bonesWithData++;
                }

                string boneName = jointPoint.Index.ToString();
                sb.AppendLine($"  {boneName,-20} | ✅ | {hasData} | {score:F2}");
            }
            else
            {
                string boneName = jointPoint.Index.ToString();
                sb.AppendLine($"  {boneName,-20} | ❌ | - | -");
            }
        }

        Debug.Log(sb.ToString());

        // 4. 诊断结果
        Debug.Log("========================================");
        Debug.Log("【诊断结果】");
        Debug.Log($"总骨骼数: {totalBones}");
        Debug.Log($"有效骨骼 (Transform != null): {validBones}/{totalBones} ({(float)validBones/totalBones*100:F1}%)");
        Debug.Log($"有数据的骨骼 (Pos3D != 0): {bonesWithData}/{totalBones} ({(float)bonesWithData/totalBones*100:F1}%)");
        Debug.Log($"检测质量分数: {vnectModel.EstimatedScore:F2}");

        // 5. 检查 IsPoseUpdate
        isPoseUpdateActive = VNectModel.IsPoseUpdate;
        Debug.Log($"IsPoseUpdate: {(isPoseUpdateActive ? "✅ 活跃" : "❌ 未激活")}");

        // 6. 检查关键骨骼（合并输出）
        var criticalSb = new StringBuilder();
        criticalSb.AppendLine("\n【关键骨骼检查】");
        CheckCriticalBone(criticalSb, "Hips", PositionIndex.hip);
        CheckCriticalBone(criticalSb, "Spine", PositionIndex.spine);
        CheckCriticalBone(criticalSb, "Neck", PositionIndex.neck);
        CheckCriticalBone(criticalSb, "Head", PositionIndex.head);
        CheckCriticalBone(criticalSb, "LeftShoulder", PositionIndex.lShldrBend);
        CheckCriticalBone(criticalSb, "RightShoulder", PositionIndex.rShldrBend);
        CheckCriticalBone(criticalSb, "LeftElbow", PositionIndex.lForearmBend);
        CheckCriticalBone(criticalSb, "RightElbow", PositionIndex.rForearmBend);
        Debug.Log(criticalSb.ToString());

        // 7. 诊断建议
        Debug.Log("\n【诊断建议】");
        if (validBones < totalBones * 0.8f)
        {
            Debug.Log("ℹ️ 提示：部分骨骼 Transform 为 null");
            Debug.Log("   说明：Unity Avatar 可能不包含所有骨骼，这是正常现象");
        }
        else if (bonesWithData < validBones * 0.5f)
        {
            Debug.Log("ℹ️ 提示：部分骨骼没有 Pos3D 数据");
            Debug.Log("   说明：MediaPipe 检测可能不完整，或者需要调整坐标转换");
        }
        else
        {
            Debug.Log("✅ 所有检查通过！");
        }

        Debug.Log("========================================");
    }

    private void CheckCriticalBone(StringBuilder sb, string name, PositionIndex index)
    {
        var jointPoint = vnectModel.JointPoints[index.Int()];

        if (jointPoint.Transform == null)
        {
            sb.AppendLine($"❌ {name}: Transform 为 null！骨骼映射失败！");
        }
        else if (jointPoint.Pos3D == Vector3.zero)
        {
            sb.AppendLine($"⚠️ {name}: Pos3D 为 0，没有数据更新");
        }
        else
        {
            sb.AppendLine($"✅ {name}: Pos3D = {jointPoint.Pos3D}, Score3D = {jointPoint.Score3D:F2}");
        }
    }

    [ContextMenu("快速诊断（每60帧）")]
    public void RunQuickDiagnostics()
    {
        if (vnectModel == null || vnectModel.JointPoints == null)
        {
            return;
        }

        var jointPoints = vnectModel.JointPoints;
        int validCount = 0;
        int dataCount = 0;

        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Transform != null) validCount++;
            if (jointPoint.Pos3D != Vector3.zero) dataCount++;
        }

        Debug.Log($"[快速诊断] 有效骨骼: {validCount}/{jointPoints.Length}, 有数据: {dataCount}/{jointPoints.Length}, IsPoseUpdate: {VNectModel.IsPoseUpdate}, Score: {vnectModel.EstimatedScore:F2}");
    }

    [ContextMenu("显示所有骨骼的 Pos3D 数据")]
    public void ShowAllBonePositions()
    {
        if (vnectModel == null || vnectModel.JointPoints == null)
        {
            Debug.Log("VNectModel 未初始化");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("【所有骨骼的 Pos3D 数据】");

        foreach (var jointPoint in vnectModel.JointPoints)
        {
            if (jointPoint.Transform != null)
            {
                string boneName = jointPoint.Index.ToString();
                sb.AppendLine($"{boneName}: {jointPoint.Pos3D}");
            }
        }

        Debug.Log(sb.ToString());
    }

    [ContextMenu("检查 VNectModelAdapter 配置")]
    public void CheckAdapterConfig()
    {
        if (vnectAdapter == null)
        {
            Debug.Log("VNectModelAdapter 未设置");
            return;
        }

        Debug.Log("【VNectModelAdapter 配置】");
        Debug.Log("✅ VNectModelAdapter 已连接");
        Debug.Log("注意：适配器参数（Scale、Flip 等）请在 Inspector 中查看");
    }

    void OnGUI()
    {
        // 使用 GUILayout 自动布局代替固定 Rect
        GUILayout.BeginArea(new UnityEngine.Rect(10, 10, 400, 250));
        GUILayout.Box("VNectModel 诊断", GUILayout.Width(380), GUILayout.Height(200));

        GUILayout.BeginVertical();
        GUILayout.Label($"初始化: {(isInitialized ? "✅" : "❌")}");
        GUILayout.Label($"有效骨骼: {validBones}/{totalBones}");
        GUILayout.Label($"有数据骨骼: {bonesWithData}/{totalBones}");
        GUILayout.Label($"IsPoseUpdate: {(isPoseUpdateActive ? "✅" : "❌")}");
        GUILayout.Label($"检测质量: {vnectModel?.EstimatedScore ?? 0:F2}");

        if (GUILayout.Button("运行完整诊断", GUILayout.Width(200)))
        {
            RunDiagnostics();
        }
        if (GUILayout.Button("显示所有 Pos3D", GUILayout.Width(200)))
        {
            ShowAllBonePositions();
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
}
