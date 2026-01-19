// Assets/Scripts/Managers/FurnitureZoomController.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 家具放大视图管理器
/// 管理场景中的所有放大视图
/// </summary>
public class FurnitureZoomController : MonoBehaviour
{
    [System.Serializable]
    public class ZoomViewMapping
    {
        [Tooltip("选择对应的放大视图枚举值")]
        public GameManager.ViewState viewState;

        [Tooltip("拖拽对应的放大视图GameObject")]
        public GameObject zoomViewObject;
    }

    [Header("Zoom View Mappings - Set in Inspector")]
    [Tooltip("为每个家具的放大视图配置映射")]
    public List<ZoomViewMapping> zoomViews = new List<ZoomViewMapping>();

    private void Awake()
    {
        // 自动注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterZoomController(this);
        }
        else
        {
            Debug.LogError("[FurnitureZoom] GameManager not found! Make sure Bootstrap is loaded first.");
        }
    }

    private void Start()
    {
        // 订阅视图状态变更事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.AddListener(OnViewStateChanged);
        }

        // 初始化：所有放大视图隐藏
        HideAllZoomViews();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.RemoveListener(OnViewStateChanged);
        }
    }

    /// <summary>
    /// 响应视图状态变更
    /// </summary>
    private void OnViewStateChanged(GameManager.ViewState newState)
    {
        // ⭐ 关键修复：如果是直接引用模式，完全不处理
        if (GameManager.Instance != null && GameManager.Instance.IsUsingDirectZoomView)
        {
            Debug.Log("[FurnitureZoom] 直接引用模式，跳过处理");
            return; // 直接返回，什么都不做
        }

        // 先隐藏所有放大视图
        HideAllZoomViews();

        // 如果是墙面视图，不需要显示任何放大视图
        if (newState == GameManager.ViewState.Wall_A ||
            newState == GameManager.ViewState.Wall_B ||
            newState == GameManager.ViewState.Wall_C ||
            newState == GameManager.ViewState.Wall_D)
        {
            return;
        }

        // 查找并激活对应的放大视图
        var activeView = zoomViews.Find(m => m.viewState == newState);
        if (activeView != null && activeView.zoomViewObject != null)
        {
            activeView.zoomViewObject.SetActive(true);
            Debug.Log($"[FurnitureZoom] 显示放大视图: {activeView.zoomViewObject.name}");
        }
        else
        {
            Debug.LogWarning($"[FurnitureZoom] 找不到对应的放大视图: {newState}");
        }
    }

    /// <summary>
    /// 隐藏所有放大视图
    /// </summary>
    public void HideAllZoomViews()
    {
        foreach (var mapping in zoomViews)
        {
            if (mapping.zoomViewObject != null)
            {
                mapping.zoomViewObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 手动显示指定的放大视图
    /// </summary>
    public void ShowZoomView(GameObject zoomViewObject)
    {
        if (zoomViewObject == null) return;

        HideAllZoomViews();
        zoomViewObject.SetActive(true);
    }
}