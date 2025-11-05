using UnityEngine;

/// <summary>
/// 场景级墙面管理器
/// 每个关卡场景有独立实例,管理本场景的四面墙
/// </summary>
public class WallManager : MonoBehaviour
{
    [Header("Wall References - Set in Inspector")]
    [Tooltip("拖拽场景中的Wall_A GameObject")]
    [SerializeField] private GameObject wallA;

    [Tooltip("拖拽场景中的Wall_B GameObject")]
    [SerializeField] private GameObject wallB;

    [Tooltip("拖拽场景中的Wall_C GameObject")]
    [SerializeField] private GameObject wallC;

    [Tooltip("拖拽场景中的Wall_D GameObject")]
    [SerializeField] private GameObject wallD;

    private void Awake()
    {
        // 自动注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterWallManager(this);
        }
        else
        {
            Debug.LogError("[WallManager] GameManager not found! Make sure Bootstrap is loaded first.");
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

            // 初始化显示
            OnViewStateChanged(GameManager.Instance.CurrentViewState);
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
    /// 验证所有墙面引用是否设置
    /// </summary>
    private void ValidateReferences()
    {
        bool hasError = false;

        if (wallA == null)
        {
            Debug.LogError("[WallManager] Wall_A reference is missing! Please assign in Inspector.");
            hasError = true;
        }

        if (wallB == null)
        {
            Debug.LogError("[WallManager] Wall_B reference is missing! Please assign in Inspector.");
            hasError = true;
        }

        if (wallC == null)
        {
            Debug.LogError("[WallManager] Wall_C reference is missing! Please assign in Inspector.");
            hasError = true;
        }

        if (wallD == null)
        {
            Debug.LogError("[WallManager] Wall_D reference is missing! Please assign in Inspector.");
            hasError = true;
        }

        if (!hasError)
        {
            Debug.Log($"[WallManager] All wall references validated successfully in {gameObject.scene.name}");
        }
    }

    /// <summary>
    /// 响应视图状态变更
    /// </summary>
    private void OnViewStateChanged(GameManager.ViewState newState)
    {
        // 先全部停用
        if (wallA != null) wallA.SetActive(false);
        if (wallB != null) wallB.SetActive(false);
        if (wallC != null) wallC.SetActive(false);
        if (wallD != null) wallD.SetActive(false);

        // 只激活当前墙面
        switch (newState)
        {
            case GameManager.ViewState.Wall_A:
                if (wallA != null) wallA.SetActive(true);
                break;
            case GameManager.ViewState.Wall_B:
                if (wallB != null) wallB.SetActive(true);
                break;
            case GameManager.ViewState.Wall_C:
                if (wallC != null) wallC.SetActive(true);
                break;
            case GameManager.ViewState.Wall_D:
                if (wallD != null) wallD.SetActive(true);
                break;
                // 如果是放大视图,墙面全部隐藏
        }
    }
}