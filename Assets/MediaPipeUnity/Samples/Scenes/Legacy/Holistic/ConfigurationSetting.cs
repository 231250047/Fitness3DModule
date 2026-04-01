using UnityEngine;

/// <summary>
/// VNectModel 的配置参数
/// 从 ThreeDPoseTracker 项目移植
/// </summary>
[System.Serializable]
public class ConfigurationSetting
{
    [Tooltip("是否锁定脚部（0=否，1=是）")]
    public int LockFoot = 0;

    [Tooltip("是否锁定腿部（0=否，1=是）")]
    public int LockLegs = 0;

    [Tooltip("是否锁定手部（0=否，1=是）")]
    public int LockHand = 0;

    [Tooltip("肘部轴向模式（0=使用父关节，1=不使用）")]
    public int ElbowAxisTop = 0;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static ConfigurationSetting CreateDefault()
    {
        return new ConfigurationSetting
        {
            LockFoot = 0,
            LockLegs = 0,
            LockHand = 0,
            ElbowAxisTop = 0
        };
    }
}
