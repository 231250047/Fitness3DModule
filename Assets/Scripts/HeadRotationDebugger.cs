using UnityEngine;

/// <summary>
/// 头部旋转调试工具
/// 用于诊断头部位置和旋转计算问题
/// </summary>
public class HeadRotationDebugger : MonoBehaviour
{
    [Header("调试设置")]
    [SerializeField] private VNectModel vnectModel;
    [SerializeField] private bool showDebugEvery60Frames = true;

    [Header("调试信息")]
    [SerializeField] private Vector3 nosePos;
    [SerializeField] private Vector3 headPos;
    [SerializeField] private Vector3 leftEarPos;
    [SerializeField] private Vector3 rightEarPos;
    [SerializeField] private Vector3 gazeVector;
    [SerializeField] private Vector3 faceNormal;
    [SerializeField] private Quaternion headRotation;

    void Update()
    {
        if (showDebugEvery60Frames && Time.frameCount % 60 == 0)
        {
            RunHeadDiagnostics();
        }
    }

    [ContextMenu("运行头部诊断")]
    public void RunHeadDiagnostics()
    {
        if (vnectModel == null || vnectModel.JointPoints == null)
        {
            Debug.LogError("❌ VNectModel 未初始化");
            return;
        }

        var jointPoints = vnectModel.JointPoints;

        // 获取关键点的 Pos3D
        nosePos = jointPoints[(int)PositionIndex.Nose].Pos3D;
        headPos = jointPoints[(int)PositionIndex.head].Pos3D;
        leftEarPos = jointPoints[(int)PositionIndex.lEar].Pos3D;
        rightEarPos = jointPoints[(int)PositionIndex.rEar].Pos3D;

        // 计算关键向量
        gazeVector = nosePos - headPos;
        faceNormal = TriangleNormal(nosePos, rightEarPos, leftEarPos);

        // 获取头部旋转
        if (jointPoints[(int)PositionIndex.head].Transform != null)
        {
            headRotation = jointPoints[(int)PositionIndex.head].Transform.rotation;
        }

        // 输出诊断信息
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【头部旋转诊断】");
        sb.AppendLine($"Nose Pos3D:    {nosePos}");
        sb.AppendLine($"Head Pos3D:    {headPos}");
        sb.AppendLine($"Left Ear Pos3D: {leftEarPos}");
        sb.AppendLine($"Right Ear Pos3D:{rightEarPos}");
        sb.AppendLine();
        sb.AppendLine($"Gaze Vector (视线方向):   {gazeVector} (长度: {gazeVector.magnitude:F3})");
        sb.AppendLine($"Face Normal (面部法向量): {faceNormal} (长度: {faceNormal.magnitude:F3})");
        sb.AppendLine();
        sb.AppendLine($"Head Rotation: {headRotation.eulerAngles}");

        // 检查问题
        sb.AppendLine();
        sb.AppendLine("【问题检查】");

        if (gazeVector.magnitude < 0.01f)
        {
            sb.AppendLine("❌ 警告：Gaze 向量接近零！鼻子和头部位置太近！");
        }
        else
        {
            sb.AppendLine($"✅ Gaze 向量正常 (长度: {gazeVector.magnitude:F3})");
        }

        if (faceNormal.magnitude < 0.1f)
        {
            sb.AppendLine("❌ 警告：Face Normal 接近零！鼻子和耳朵位置共线！");
            sb.AppendLine("   原因：TriangleNormal() 返回零向量");
            sb.AppendLine("   影响：头部旋转会出错");
        }
        else
        {
            sb.AppendLine($"✅ Face Normal 正常 (长度: {faceNormal.magnitude:F3})");
        }

        if (nosePos == Vector3.zero)
        {
            sb.AppendLine("❌ 警告：Nose Pos3D 为零！VNectModelAdapter 没有更新鼻子数据！");
        }

        if (leftEarPos == Vector3.zero || rightEarPos == Vector3.zero)
        {
            sb.AppendLine("❌ 警告：耳朵 Pos3D 为零！VNectModelAdapter 没有更新耳朵数据！");
        }

        Debug.Log(sb.ToString());
    }

    private Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;
        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();
        return dd;
    }
}
