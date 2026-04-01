using UnityEngine;

/// <summary>
/// 测试脚本：用于验证character.fbx的Humanoid配置是否正确
/// </summary>
public class TestCharacterBones : MonoBehaviour
{
    [Header("拖拽你的Character到这里")]
    public Animator characterAnimator;

    void Start()
    {
        if (characterAnimator == null)
        {
            Debug.LogError("❌ 请在Inspector中拖拽Character的Animator组件！");
            return;
        }

        Debug.Log("========== 开始测试骨骼映射 ==========");

        // 测试关键骨骼是否可以获取
        TestBone(HumanBodyBones.Hips, "Hips (臀部)");
        TestBone(HumanBodyBones.Spine, "Spine (脊柱)");
        TestBone(HumanBodyBones.Chest, "Chest (胸部)");
        TestBone(HumanBodyBones.Neck, "Neck (脖子)");
        TestBone(HumanBodyBones.Head, "Head (头部)");

        TestBone(HumanBodyBones.LeftShoulder, "Left Shoulder (左肩)");
        TestBone(HumanBodyBones.LeftUpperArm, "Left Upper Arm (左上臂)");
        TestBone(HumanBodyBones.LeftLowerArm, "Left Lower Arm (左前臂)");
        TestBone(HumanBodyBones.LeftHand, "Left Hand (左手)");

        TestBone(HumanBodyBones.RightShoulder, "Right Shoulder (右肩)");
        TestBone(HumanBodyBones.RightUpperArm, "Right Upper Arm (右上臂)");
        TestBone(HumanBodyBones.RightLowerArm, "Right Lower Arm (右前臂)");
        TestBone(HumanBodyBones.RightHand, "Right Hand (右手)");

        TestBone(HumanBodyBones.LeftUpperLeg, "Left Upper Leg (左大腿)");
        TestBone(HumanBodyBones.LeftLowerLeg, "Left Lower Leg (左小腿)");
        TestBone(HumanBodyBones.LeftFoot, "Left Foot (左脚)");

        TestBone(HumanBodyBones.RightUpperLeg, "Right Upper Leg (右大腿)");
        TestBone(HumanBodyBones.RightLowerLeg, "Right Lower Leg (右小腿)");
        TestBone(HumanBodyBones.RightFoot, "Right Foot (右脚)");

        Debug.Log("========== 测试完成 ==========");
    }

    void TestBone(HumanBodyBones boneType, string boneName)
    {
        Transform bone = characterAnimator.GetBoneTransform(boneType);

        if (bone != null)
        {
            Debug.Log($"✅ {boneName}: 已找到 → {bone.name}");
        }
        else
        {
            Debug.LogError($"❌ {boneName}: 未找到！请检查Avatar配置！");
        }
    }
}
