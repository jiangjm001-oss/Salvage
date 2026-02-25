// Assets/Scripts/GamePlay/SpotTheDifference/SpotTheDifferenceManager.cs
// 找茬玩法主控制器 - 世界空间版本（SpriteRenderer）
// 支持圆圈标记动效、物品拾取、返回按钮控制

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 找茬玩法管理器（世界空间版）
/// 控制整个找茬游戏的流程：点击检测 → 标记显示 → 完成奖励
/// </summary>
public class SpotTheDifferenceManager : MonoBehaviour
{
    // ============ 图片设置 ============
    [Header("图片设置")]
    [Tooltip("左侧图片的 SpriteRenderer")]
    [SerializeField] private SpriteRenderer leftImage;

    [Tooltip("右侧图片的 SpriteRenderer")]
    [SerializeField] private SpriteRenderer rightImage;

    // ============ 差异点设置 ============
    [Header("差异点设置")]
    [Tooltip("所有差异点配置（10个）")]
    [SerializeField] private List<DifferenceSpotConfig> differenceSpots = new List<DifferenceSpotConfig>();

    [Tooltip("点击检测半径（世界单位）")]
    [SerializeField] private float clickRadius = 0.3f;

    // ============ 圆圈标记设置 ============
    [Header("圆圈标记设置")]
    [Tooltip("圆圈标记的 Sprite")]
    [SerializeField] private Sprite circleMarkerSprite;

    [Tooltip("圆圈标记的大小（世界单位）")]
    [SerializeField] private float circleMarkerScale = 0.5f;

    [Tooltip("圆圈标记的颜色")]
    [SerializeField] private Color circleMarkerColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    [Tooltip("圆圈出现动画时长")]
    [SerializeField] private float circleAnimationDuration = 0.3f;

    [Tooltip("圆圈标记的 Sorting Order（相对于图片）")]
    [SerializeField] private int circleMarkerSortingOffset = 1;

    // ============ 奖励物品设置 ============
    [Header("奖励物品设置")]
    [Tooltip("完成后出现的可拾取物品（带 SpriteRenderer）")]
    [SerializeField] private GameObject collectableItem;

    [Tooltip("物品的 ItemData（leaf1）")]
    [SerializeField] private ItemData rewardItemData;

    [Tooltip("物品渐显时长")]
    [SerializeField] private float itemFadeDuration = 0.5f;

    // ============ 返回按钮设置 ============
    [Header("返回按钮设置")]
    [Tooltip("返回按钮（带 SpriteRenderer 和 Collider2D）")]
    [SerializeField] private GameObject backButton;

    [Tooltip("返回按钮渐显时长")]
    [SerializeField] private float backButtonFadeDuration = 0.3f;

    // ============ 音效设置 ============
    [Header("音效设置")]
    [Tooltip("找到正确位置的音效")]
    [SerializeField] private string correctSoundName = "spot_correct";

    [Tooltip("点击错误的音效（可选）")]
    [SerializeField] private string wrongSoundName = "";

    [Tooltip("道具出现的音效")]
    [SerializeField] private string itemAppearSoundName = "item_appear";

    [Tooltip("拾取物品的音效")]
    [SerializeField] private string pickupSoundName = "pickup";

    // ============ 事件 ============
    [Header("事件")]
    public UnityEvent OnSpotFound;
    public UnityEvent OnAllSpotsFound;
    public UnityEvent OnItemCollected;

    // ============ 私有变量 ============
    private int foundCount = 0;
    private bool isCompleted = false;
    private bool isItemCollected = false;
    private bool backButtonVisible = false;
    private HashSet<int> foundSpotIndices = new HashSet<int>();
    private List<GameObject> createdMarkers = new List<GameObject>();

    private SpriteRenderer itemSpriteRenderer;
    private SpriteRenderer backButtonSpriteRenderer;
    private Collider2D itemCollider;
    private Collider2D backButtonCollider;

    // 图片边界缓存
    private Bounds leftImageBounds;
    private Bounds rightImageBounds;

    // ============ 生命周期 ============

    private void Awake()
    {
        // 缓存组件引用
        CacheComponents();

        // 初始化 UI 状态
        InitializeState();
    }

    private void OnEnable()
    {
        // 每次启用时重置
        ResetGame();

        // 更新边界缓存
        UpdateBoundsCache();
    }

    private void Update()
    {
        // 检测点击
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    // ============ 初始化 ============

    /// <summary>
    /// 缓存组件引用
    /// </summary>
    private void CacheComponents()
    {
        // 可拾取物品
        if (collectableItem != null)
        {
            itemSpriteRenderer = collectableItem.GetComponent<SpriteRenderer>();
            itemCollider = collectableItem.GetComponent<Collider2D>();
        }

        // 返回按钮
        if (backButton != null)
        {
            backButtonSpriteRenderer = backButton.GetComponent<SpriteRenderer>();
            backButtonCollider = backButton.GetComponent<Collider2D>();
        }
    }

    /// <summary>
    /// 初始化状态
    /// </summary>
    private void InitializeState()
    {
        // 隐藏可拾取物品（透明 + 禁用碰撞）
        if (itemSpriteRenderer != null)
        {
            SetSpriteAlpha(itemSpriteRenderer, 0f);
        }
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        // 隐藏返回按钮
        if (backButtonSpriteRenderer != null)
        {
            SetSpriteAlpha(backButtonSpriteRenderer, 0f);
        }
        if (backButtonCollider != null)
        {
            backButtonCollider.enabled = false;
        }
    }

    /// <summary>
    /// 更新图片边界缓存
    /// </summary>
    private void UpdateBoundsCache()
    {
        if (leftImage != null)
        {
            leftImageBounds = leftImage.bounds;
        }
        if (rightImage != null)
        {
            rightImageBounds = rightImage.bounds;
        }
    }

    /// <summary>
    /// 重置游戏状态
    /// </summary>
    public void ResetGame()
    {
        foundCount = 0;
        isCompleted = false;
        isItemCollected = false;
        backButtonVisible = false;
        foundSpotIndices.Clear();

        // 清除所有标记
        foreach (var marker in createdMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        createdMarkers.Clear();

        // 重置差异点状态
        foreach (var spot in differenceSpots)
        {
            spot.isFound = false;
        }

        // 重置 UI
        InitializeState();

        Debug.Log($"[SpotTheDifference] 游戏已重置，共 {differenceSpots.Count} 个差异点");
    }

    // ============ 点击处理 ============

    /// <summary>
    /// 处理点击
    /// </summary>
    private void HandleClick()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 优先检查返回按钮
        if (backButtonVisible && backButtonCollider != null && backButtonCollider.enabled)
        {
            if (backButtonCollider.OverlapPoint(worldPos))
            {
                OnBackButtonClicked();
                return;
            }
        }

        // 检查可拾取物品
        if (isCompleted && !isItemCollected && itemCollider != null && itemCollider.enabled)
        {
            if (itemCollider.OverlapPoint(worldPos))
            {
                OnCollectableItemClicked();
                return;
            }
        }

        // 游戏未完成时检测差异点
        if (!isCompleted)
        {
            CheckDifferenceSpot(worldPos);
        }
    }

    /// <summary>
    /// 检测是否点击到差异点
    /// </summary>
    private void CheckDifferenceSpot(Vector2 worldPos)
    {
        // 检查是否点击在左图或右图上
        bool clickedOnLeft = leftImageBounds.Contains(worldPos);
        bool clickedOnRight = rightImageBounds.Contains(worldPos);

        if (!clickedOnLeft && !clickedOnRight)
        {
            return; // 点击不在图片上
        }

        // 使用点击的图片计算归一化坐标
        SpriteRenderer clickedImage = clickedOnLeft ? leftImage : rightImage;
        Bounds bounds = clickedOnLeft ? leftImageBounds : rightImageBounds;

        Vector2 normalizedPos = GetNormalizedPosition(bounds, worldPos);

        // 检查是否命中任何差异点
        int hitIndex = FindHitSpot(normalizedPos);

        if (hitIndex >= 0 && !foundSpotIndices.Contains(hitIndex))
        {
            // 找到新的差异点
            OnSpotHit(hitIndex);
        }
        else if (hitIndex < 0 && !string.IsNullOrEmpty(wrongSoundName))
        {
            // 点击错误位置
            PlaySound(wrongSoundName);
        }
    }

    /// <summary>
    /// 获取归一化坐标（相对于图片左下角，范围 0-1）
    /// </summary>
    private Vector2 GetNormalizedPosition(Bounds bounds, Vector2 worldPos)
    {
        float x = (worldPos.x - bounds.min.x) / bounds.size.x;
        float y = (worldPos.y - bounds.min.y) / bounds.size.y;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 从归一化坐标转换为世界坐标
    /// </summary>
    private Vector3 GetWorldPositionFromNormalized(Bounds bounds, Vector2 normalizedPos)
    {
        float x = bounds.min.x + normalizedPos.x * bounds.size.x;
        float y = bounds.min.y + normalizedPos.y * bounds.size.y;
        return new Vector3(x, y, bounds.center.z - 0.01f); // 稍微靠前
    }

    /// <summary>
    /// 查找命中的差异点索引
    /// </summary>
    private int FindHitSpot(Vector2 normalizedPoint)
    {
        // 将点击半径转换为归一化单位
        float imageWidth = leftImageBounds.size.x;
        float radiusNormalized = clickRadius / imageWidth;

        for (int i = 0; i < differenceSpots.Count; i++)
        {
            if (foundSpotIndices.Contains(i)) continue;

            Vector2 spotPos = differenceSpots[i].normalizedPosition;
            float distance = Vector2.Distance(normalizedPoint, spotPos);

            if (distance <= radiusNormalized)
            {
                return i;
            }
        }

        return -1;
    }

    // ============ 差异点命中处理 ============

    /// <summary>
    /// 差异点被找到时的处理
    /// </summary>
    private void OnSpotHit(int index)
    {
        if (index < 0 || index >= differenceSpots.Count) return;

        var spot = differenceSpots[index];
        spot.isFound = true;
        foundSpotIndices.Add(index);
        foundCount++;

        // 播放正确音效
        PlaySound(correctSoundName);

        // 在两张图上显示圆圈标记
        CreateCircleMarker(leftImage, leftImageBounds, spot.normalizedPosition);
        CreateCircleMarker(rightImage, rightImageBounds, spot.normalizedPosition);

        // 触发事件
        OnSpotFound?.Invoke();

        Debug.Log($"[SpotTheDifference] 找到差异点 {index + 1}，进度: {foundCount}/{differenceSpots.Count}");

        // 检查是否全部找到
        if (foundCount >= differenceSpots.Count)
        {
            OnAllSpotsFoundHandler();
        }
    }

    /// <summary>
    /// 创建圆圈标记
    /// </summary>
    private void CreateCircleMarker(SpriteRenderer parentImage, Bounds bounds, Vector2 normalizedPos)
    {
        // 创建标记 GameObject
        GameObject marker = new GameObject("CircleMarker");
        marker.transform.SetParent(parentImage.transform, false);

        // 计算世界坐标
        Vector3 worldPos = GetWorldPositionFromNormalized(bounds, normalizedPos);
        marker.transform.position = worldPos;

        // 添加 SpriteRenderer
        SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = circleMarkerSprite ?? CreateDefaultCircleSprite();
        markerRenderer.color = new Color(circleMarkerColor.r, circleMarkerColor.g, circleMarkerColor.b, 0f); // 初始透明
        markerRenderer.sortingLayerName = parentImage.sortingLayerName;
        markerRenderer.sortingOrder = parentImage.sortingOrder + circleMarkerSortingOffset;

        // 设置大小
        marker.transform.localScale = Vector3.zero; // 初始大小为0

        // 记录标记
        createdMarkers.Add(marker);

        // 播放出现动画
        StartCoroutine(AnimateMarkerAppear(marker, markerRenderer));
    }

    /// <summary>
    /// 创建默认的圆形 Sprite（程序化生成）
    /// </summary>
    private Sprite CreateDefaultCircleSprite()
    {
        int size = 128;
        int thickness = 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        Color32 transparent = new Color32(0, 0, 0, 0);

        float center = size / 2f;
        float outerRadius = size / 2f - 2;
        float innerRadius = outerRadius - thickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist >= innerRadius && dist <= outerRadius)
                {
                    float alpha = 1f;
                    if (dist > outerRadius - 1) alpha = outerRadius - dist + 1;
                    if (dist < innerRadius + 1) alpha = dist - innerRadius + 1;
                    alpha = Mathf.Clamp01(alpha);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * alpha));
                }
                else
                {
                    pixels[y * size + x] = transparent;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 圆圈标记出现动画
    /// </summary>
    private IEnumerator AnimateMarkerAppear(GameObject marker, SpriteRenderer renderer)
    {
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * circleMarkerScale;
        Vector3 startScale = targetScale * 1.5f;

        while (elapsed < circleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / circleAnimationDuration;

            // 缓出曲线
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // 缩放动画
            marker.transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);

            // 透明度动画
            Color c = renderer.color;
            c.a = Mathf.Lerp(0f, circleMarkerColor.a, easedT);
            renderer.color = c;

            yield return null;
        }

        marker.transform.localScale = targetScale;
        Color finalColor = renderer.color;
        finalColor.a = circleMarkerColor.a;
        renderer.color = finalColor;
    }

    // ============ 完成处理 ============

    /// <summary>
    /// 所有差异点都找到时的处理
    /// </summary>
    private void OnAllSpotsFoundHandler()
    {
        isCompleted = true;

        // 触发事件
        OnAllSpotsFound?.Invoke();

        Debug.Log("[SpotTheDifference] 所有差异点已找到！");

        // 延迟显示奖励物品
        StartCoroutine(ShowRewardSequence());
    }

    /// <summary>
    /// 显示奖励序列
    /// </summary>
    private IEnumerator ShowRewardSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // 播放道具出现音效
        PlaySound(itemAppearSoundName);

        // 渐显可拾取物品
        if (itemSpriteRenderer != null)
        {
            yield return StartCoroutine(FadeSprite(itemSpriteRenderer, 0f, 1f, itemFadeDuration));

            // 启用碰撞
            if (itemCollider != null)
            {
                itemCollider.enabled = true;
            }
        }

        Debug.Log("[SpotTheDifference] 奖励物品已显示");
    }

    // ============ 物品拾取 ============

    /// <summary>
    /// 可拾取物品被点击
    /// </summary>
    private void OnCollectableItemClicked()
    {
        if (isItemCollected) return;
        if (!isCompleted) return;

        isItemCollected = true;

        // 播放拾取音效
        PlaySound(pickupSoundName);

        // 添加到背包
        if (rewardItemData != null && InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddItem(rewardItemData);
            if (added)
            {
                Debug.Log($"[SpotTheDifference] 物品 '{rewardItemData.displayName}' 已添加到背包");
            }
        }

        // 隐藏物品并显示返回按钮
        StartCoroutine(HideCollectableAndShowBack());

        // 触发事件
        OnItemCollected?.Invoke();
    }

    /// <summary>
    /// 隐藏物品并显示返回按钮
    /// </summary>
    private IEnumerator HideCollectableAndShowBack()
    {
        // 禁用物品碰撞
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        // 渐隐物品
        if (itemSpriteRenderer != null)
        {
            yield return StartCoroutine(FadeSprite(itemSpriteRenderer, 1f, 0f, 0.2f));
            collectableItem.SetActive(false);
        }

        // 渐显返回按钮
        if (backButtonSpriteRenderer != null)
        {
            backButton.SetActive(true);
            yield return StartCoroutine(FadeSprite(backButtonSpriteRenderer, 0f, 1f, backButtonFadeDuration));

            // 启用碰撞
            if (backButtonCollider != null)
            {
                backButtonCollider.enabled = true;
            }
            backButtonVisible = true;
        }

        Debug.Log("[SpotTheDifference] 返回按钮已显示");
    }

    // ============ 返回按钮 ============

    /// <summary>
    /// 返回按钮点击处理
    /// </summary>
    private void OnBackButtonClicked()
    {
        Debug.Log("[SpotTheDifference] 点击返回按钮");

        // 使用 GameManager 返回上一视图
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitZoomView();
        }
        else
        {
            Debug.LogWarning("[SpotTheDifference] GameManager 未找到！");
        }
    }

    // ============ 工具方法 ============

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    /// <summary>
    /// 设置 SpriteRenderer 透明度
    /// </summary>
    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color c = renderer.color;
        c.a = alpha;
        renderer.color = c;
    }

    /// <summary>
    /// SpriteRenderer 渐变动画
    /// </summary>
    private IEnumerator FadeSprite(SpriteRenderer renderer, float from, float to, float duration)
    {
        if (renderer == null) yield break;

        float elapsed = 0f;
        SetSpriteAlpha(renderer, from);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetSpriteAlpha(renderer, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetSpriteAlpha(renderer, to);
    }

    // ============ 编辑器辅助 ============

#if UNITY_EDITOR
    [ContextMenu("初始化10个空差异点")]
    private void AddEmptySpots()
    {
        differenceSpots.Clear();
        for (int i = 0; i < 10; i++)
        {
            differenceSpots.Add(new DifferenceSpotConfig
            {
                spotName = $"Spot_{i + 1}",
                normalizedPosition = new Vector2(0.5f, 0.5f)
            });
        }
        Debug.Log("[SpotTheDifference] 已添加10个空差异点配置");
    }

    private void OnDrawGizmosSelected()
    {
        if (leftImage == null) return;

        // 更新边界
        Bounds bounds = leftImage.bounds;

        // 绘制差异点位置
        for (int i = 0; i < differenceSpots.Count; i++)
        {
            var spot = differenceSpots[i];
            Vector3 worldPos = GetWorldPositionFromNormalized(bounds, spot.normalizedPosition);

            Gizmos.color = spot.isFound ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(worldPos, clickRadius);

            // 绘制序号
            UnityEditor.Handles.Label(worldPos + Vector3.up * clickRadius * 1.2f, $"{i + 1}");
        }

        // 绘制图片边界
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        if (rightImage != null)
        {
            Bounds rightBounds = rightImage.bounds;
            Gizmos.DrawWireCube(rightBounds.center, rightBounds.size);
        }
    }
#endif
}

/// <summary>
/// 差异点配置
/// </summary>
[System.Serializable]
public class DifferenceSpotConfig
{
    [Tooltip("差异点名称（用于调试）")]
    public string spotName;

    [Tooltip("在图片上的归一化位置（0-1范围，左下角为原点）")]
    public Vector2 normalizedPosition;

    [HideInInspector]
    public bool isFound = false;
}