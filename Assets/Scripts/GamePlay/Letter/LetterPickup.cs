// Assets/Scripts/GamePlay/Letter/LetterPickup.cs
// 信纸拾取组件 - 挂在可拾取的信纸物体上
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 信纸拾取组件
/// 挂在打字机 ZoomView 中完成打字后显示的信纸上
/// 点击后将信纸添加回背包
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LetterPickup : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("拾取后是否退出放大视图")]
    public bool exitZoomViewOnPickup = true;

    [Tooltip("拾取后是否隐藏此物体")]
    public bool hideOnPickup = true;

    [Header("音效")]
    public string pickupSoundName = "item_pickup";

    [Header("事件")]
    public UnityEvent OnPickedUp;

    private bool hasBeenPickedUp = false;

    private void OnEnable()
    {
        // 每次显示时重置状态（如果已被拾取则隐藏）
        if (hasBeenPickedUp && hideOnPickup)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Pickup();
    }

    /// <summary>
    /// 执行拾取
    /// </summary>
    public void Pickup()
    {
        if (hasBeenPickedUp) return;

        if (LetterManager.Instance == null)
        {
            Debug.LogError("[LetterPickup] LetterManager.Instance 为空！");
            return;
        }

        hasBeenPickedUp = true;
        Debug.Log("[LetterPickup] 拾取信纸");

        // 添加到背包
        LetterManager.Instance.AddLetterToInventory();

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
        {
            AudioManager.Instance.PlaySFX(pickupSoundName);
        }

        // 触发事件
        OnPickedUp?.Invoke();

        // 隐藏自己
        if (hideOnPickup)
        {
            gameObject.SetActive(false);
        }

        // 退出放大视图
        if (exitZoomViewOnPickup && GameManager.Instance != null)
        {
            GameManager.Instance.ExitZoomView();
        }
    }

    /// <summary>
    /// 重置状态（用于测试或重新开始游戏）
    /// </summary>
    public void ResetState()
    {
        hasBeenPickedUp = false;
    }
}