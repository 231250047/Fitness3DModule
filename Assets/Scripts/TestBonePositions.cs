using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 测试Unity Humanoid骨骼的实际位置
/// 用于诊断为什么LeftUpperLeg的InitLocalPositionY值这么小
/// </summary>
public class TestBonePositions : MonoBehaviour
{
    public Animator targetCharacter;

    void Start()
    {
        if (targetCharacter == null)
        {
            Debug.LogError("请设置Target Character！");
            return;
        }

        Debug.Log("========== 骨骼位置测试 ==========");
        Debug.Log($"模型名称: {targetCharacter.name}");
        Debug.Log($"模型位置: {targetCharacter.transform.position}");
        Debug.Log($"模型缩放: {targetCharacter.transform.localScale}");

        var hips = targetCharacter.GetBoneTransform(HumanBodyBones.Hips);
        var leftUpperLeg = targetCharacter.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var leftLowerLeg = targetCharacter.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        var leftFoot = targetCharacter.GetBoneTransform(HumanBodyBones.LeftFoot);

        if (hips != null && leftUpperLeg != null && leftLowerLeg != null)
        {
            Debug.Log("\n【绝对位置】（Transform.position）");
            Debug.Log($"Hips:           {hips.position}");
            Debug.Log($"LeftUpperLeg:   {leftUpperLeg.position}");
            Debug.Log($"LeftLowerLeg:   {leftLowerLeg.position}");
            Debug.Log($"LeftFoot:       {leftFoot.position}");

            Debug.Log("\n【相对位置】（相对于Hips）");
            Vector3 leftUpperLegRel = leftUpperLeg.position - hips.position;
            Vector3 leftLowerLegRel = leftLowerLeg.position - hips.position;
            Vector3 leftFootRel = leftFoot.position - hips.position;

            Debug.Log($"LeftUpperLeg相对Hips: {leftUpperLegRel}  长度={leftUpperLegRel.magnitude:F3}米");
            Debug.Log($"LeftLowerLeg相对Hips: {leftLowerLegRel}  长度={leftLowerLegRel.magnitude:F3}米");
            Debug.Log($"LeftFoot相对Hips:     {leftFootRel}  长度={leftFootRel.magnitude:F3}米");

            Debug.Log("\n【骨骼长度】");
            Vector3 upperLegLength = leftLowerLeg.position - leftUpperLeg.position;
            Vector3 lowerLegLength = leftFoot.position - leftLowerLeg.position;

            Debug.Log($"大腿长度（UpperLeg→LowerLeg）: {upperLegLength.magnitude:F3}米");
            Debug.Log($"小腿长度（LowerLeg→Foot）:     {lowerLegLength.magnitude:F3}米");

            Debug.Log("\n【诊断】");
            if (leftUpperLegRel.magnitude < 0.1f)
            {
                Debug.LogError("❌ LeftUpperLeg距离Hips太近！可能Avatar配置有问题！");
                Debug.LogError("   正常应该是0.3-0.4米，现在是" + leftUpperLegRel.magnitude.ToString("F3") + "米");
            }
            else
            {
                Debug.Log("✅ LeftUpperLeg位置正常");
            }

            if (upperLegLength.magnitude < 0.3f || upperLegLength.magnitude > 0.6f)
            {
                Debug.LogWarning($"⚠️ 大腿长度异常：{upperLegLength.magnitude:F3}米（正常应该是0.4-0.5米）");
            }
            else
            {
                Debug.Log("✅ 大腿长度正常");
            }
        }
        else
        {
            Debug.LogError("无法获取骨骼Transform！");
        }

        Debug.Log("==================================");
    }
}
