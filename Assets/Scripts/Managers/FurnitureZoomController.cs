using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 场景级家具放大视图管理器
/// 每个关卡场景有独立实例,管理本场景的所有放大视图
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

        // 验证引用
        ValidateReferences();
    }

    private void Start()
    {
        // 订阅GameManager的视图状态变更事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.AddListener(OnViewStateChanged);

            // 初始化:所有放大视图隐藏
            HideAllZoomViews();
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.RemoveListener(OnViewStateChanged);
        }
    }

    /// <summary>
    /// 验证所有放大视图引用是否设置
    /// </summary>
    private void ValidateReferences()
    {
        bool hasError = false;

        for (int i = 0; i < zoomViews.Count; i++)
        {
            var mapping = zoomViews[i];

            if (mapping.zoomViewObject == null)
            {
                Debug.LogError($"[FurnitureZoom] Zoom view [{i}] ({mapping.viewState}) object is missing! Please assign in Inspector.");
                hasError = true;
            }
        }

        if (!hasError && zoomViews.Count > 0)
        {
            Debug.Log($"[FurnitureZoom] All {zoomViews.Count} zoom view references validated in {gameObject.scene.name}");
        }
    }

    /// <summary>
    /// 响应视图状态变更
    /// </summary>
    private void OnViewStateChanged(GameManager.ViewState newState)
    {
        // 先全部停用
        HideAllZoomViews();

        // 激活匹配的放大视图
        var activeView = zoomViews.Find(m => m.viewState == newState);
        if (activeView != null && activeView.zoomViewObject != null)
        {
            activeView.zoomViewObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏所有放大视图
    /// </summary>
    private void HideAllZoomViews()
    {
        foreach (var mapping in zoomViews)
        {
            if (mapping.zoomViewObject != null)
                mapping.zoomViewObject.SetActive(false);
        }
    }
}