using UnityEngine;

/// <summary>
/// 自动定位 Nose 对象到正确的面部位置
/// </summary>
public class AutoPositionNose : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private GameObject characterModel;
    [SerializeField] private GameObject noseObject;
    [SerializeField] private bool autoPositionOnStart = true;

    [Header("Nose 偏移设置")]
    [SerializeField] private float forwardOffset = 0.25f;  // 鼻子在头部前方的距离
    [SerializeField] private float upwardOffset = 0.05f;   // 鼻子比头部高一点
    [SerializeField] private float sideOffset = 0.0f;      // 左右偏移（通常为0）

    void Start()
    {
        if (autoPositionOnStart)
        {
            AutoPosition();
        }
    }

    [ContextMenu("自动定位 Nose")]
    public void AutoPosition()
    {
        if (characterModel == null)
        {
            Debug.LogError("❌ Character Model 未设置！");
            return;
        }

        if (noseObject == null)
        {
            Debug.LogError("❌ Nose Object 未设置！");
            return;
        }

        // 获取 Animator
        Animator animator = characterModel.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("❌ Character Model 没有 Animator 组件！");
            return;
        }

        // 获取头部骨骼
        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        if (headBone == null)
        {
            Debug.LogError("❌ 找不到 Head 骨骼！请检查 Avatar 配置");
            return;
        }

        // 计算 Nose 位置
        // Nose 应该在头部骨骼的前方
        Vector3 headPosition = headBone.position;
        Vector3 headForward = headBone.forward;

        Vector3 nosePosition = headPosition
                            + headBone.forward * forwardOffset  // 前方
                            + headBone.up * upwardOffset         // 上方
                            + headBone.right * sideOffset;      // 左右

        // 设置 Nose 位置
        noseObject.transform.position = nosePosition;

        // 设置 Nose 的父对象为角色（保持跟随）
        noseObject.transform.SetParent(characterModel.transform, false);

        Debug.Log("✅ Nose 对象已自动定位：");
        Debug.Log($"   Head Position: {headPosition}");
        Debug.Log($"   Nose Position: {nosePosition}");
        Debug.Log($"   Forward Offset: {forwardOffset}, Upward Offset: {upwardOffset}");
    }

    [ContextMenu("测试不同位置")]
    public void TestDifferentPositions()
    {
        if (characterModel == null || noseObject == null)
        {
            Debug.LogError("请先设置 Character Model 和 Nose Object");
            return;
        }

        Animator animator = characterModel.GetComponent<Animator>();
        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        if (headBone == null) return;

        // 测试多个位置
        Vector3[] testOffsets = new Vector3[]
        {
            new Vector3(0, 0.05f, 0.20f),   // 保守位置
            new Vector3(0, 0.05f, 0.25f),   // 默认位置
            new Vector3(0, 0.05f, 0.30f),   // 更靠前
            new Vector3(0, 0.10f, 0.25f),   // 更靠上
        };

        for (int i = 0; i < testOffsets.Length; i++)
        {
            Vector3 offset = testOffsets[i];
            Vector3 nosePos = headBone.position
                            + headBone.forward * offset.z
                            + headBone.up * offset.y
                            + headBone.right * offset.x;

            Debug.Log($"测试位置 {i + 1}: {nosePos} (偏移: {offset})");
        }

        Debug.Log("请在 Inspector 中调整 forwardOffset 和 upwardOffset，然后点击 '自动定位 Nose'");
    }
}
