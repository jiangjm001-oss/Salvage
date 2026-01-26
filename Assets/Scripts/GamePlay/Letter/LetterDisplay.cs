// Assets/Scripts/GamePlay/Letter/LetterDisplay.cs
// 信纸显示组件 - 管理信纸上各部件的显示/隐藏
// 放在任何需要显示信纸的物体上（ZoomView中的信纸、桌面上的信纸等）
using UnityEngine;

/// <summary>
/// 信纸显示组件
/// 根据 LetterManager 的状态自动显示/隐藏信纸上的各个部件
/// </summary>
public class LetterDisplay : MonoBehaviour
{
    [Header("信纸部件引用")]
    [Tooltip("基础信纸（始终显示）")]
    public GameObject basePaper;

    [Tooltip("收件人文字（打字机完成后显示）")]
    public GameObject recipientOverlay;

    [Tooltip("标题贴纸（粘贴完成后显示）")]
    public GameObject titleOverlay;

    [Tooltip("Logo 图案（涂抹完成后显示）")]
    public GameObject logoOverlay;

    [Header("设置")]
    [Tooltip("是否在 OnEnable 时自动刷新显示")]
    public bool autoRefreshOnEnable = true;

    private void OnEnable()
    {
        if (autoRefreshOnEnable)
        {
            RefreshDisplay();
        }

        // 订阅状态变化事件
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.OnLetterStateChanged.AddListener(OnStateChanged);
        }
    }

    private void OnDisable()
    {
        // 取消订阅
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.OnLetterStateChanged.RemoveListener(OnStateChanged);
        }
    }

    /// <summary>
    /// 状态变化回调
    /// </summary>
    private void OnStateChanged(int stateIndex)
    {
        RefreshDisplay();
    }

    /// <summary>
    /// 根据 LetterManager 状态刷新显示
    /// </summary>
    public void RefreshDisplay()
    {
        if (LetterManager.Instance == null)
        {
            Debug.LogWarning("[LetterDisplay] LetterManager.Instance 为空，无法刷新");
            return;
        }

        // 基础信纸始终显示
        if (basePaper != null)
        {
            basePaper.SetActive(true);
        }

        // 根据状态显示各部件
        if (recipientOverlay != null)
        {
            recipientOverlay.SetActive(LetterManager.Instance.hasRecipient);
        }

        if (titleOverlay != null)
        {
            titleOverlay.SetActive(LetterManager.Instance.hasTitle);
        }

        if (logoOverlay != null)
        {
            logoOverlay.SetActive(LetterManager.Instance.hasLogo);
        }

        Debug.Log($"[LetterDisplay] 刷新显示 - R:{LetterManager.Instance.hasRecipient} T:{LetterManager.Instance.hasTitle} L:{LetterManager.Instance.hasLogo}");
    }

    /// <summary>
    /// 手动设置某个部件的显示状态（用于动画过渡等特殊情况）
    /// </summary>
    public void SetOverlayVisible(LetterPart part, bool visible)
    {
        switch (part)
        {
            case LetterPart.Recipient:
                if (recipientOverlay != null) recipientOverlay.SetActive(visible);
                break;
            case LetterPart.Title:
                if (titleOverlay != null) titleOverlay.SetActive(visible);
                break;
            case LetterPart.Logo:
                if (logoOverlay != null) logoOverlay.SetActive(visible);
                break;
        }
    }

    /// <summary>
    /// 隐藏所有部件（用于重置）
    /// </summary>
    public void HideAllOverlays()
    {
        if (recipientOverlay != null) recipientOverlay.SetActive(false);
        if (titleOverlay != null) titleOverlay.SetActive(false);
        if (logoOverlay != null) logoOverlay.SetActive(false);
    }

    /// <summary>
    /// 显示所有部件（用于预览完成状态）
    /// </summary>
    public void ShowAllOverlays()
    {
        if (recipientOverlay != null) recipientOverlay.SetActive(true);
        if (titleOverlay != null) titleOverlay.SetActive(true);
        if (logoOverlay != null) logoOverlay.SetActive(true);
    }
}

/// <summary>
/// 信纸部件枚举
/// </summary>
public enum LetterPart
{
    Recipient,  // 收件人
    Title,      // 标题
    Logo        // Logo
}