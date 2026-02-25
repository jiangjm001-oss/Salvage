// Assets/Scripts/GamePlay/SpotTheDifference/SpotPositionHelper.cs
// 差异点位置设置辅助工具 - 世界空间版本
// 在 Play 模式下点击图片获取归一化坐标，用于快速确定差异点位置
// 开发完成后可删除此组件

using UnityEngine;

/// <summary>
/// 差异点位置辅助工具（世界空间版）
/// 在运行时点击 SpriteRenderer 图片，会输出归一化坐标到 Console
/// </summary>
public class SpotPositionHelper : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("要检测的图片 SpriteRenderer")]
    [SerializeField] private SpriteRenderer targetImage;

    [Tooltip("是否启用辅助工具")]
    [SerializeField] private bool isEnabled = true;

    [Tooltip("显示点击标记")]
    [SerializeField] private bool showClickMarker = true;

    [Header("标记设置")]
    [SerializeField] private Color markerColor = Color.red;
    [SerializeField] private float markerRadius = 0.1f;

    private int clickCount = 0;
    private GameObject currentMarker;
    private Bounds imageBounds;

    private void Start()
    {
        if (targetImage != null)
        {
            imageBounds = targetImage.bounds;
        }
    }

    private void Update()
    {
        if (!isEnabled) return;
        if (targetImage == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            CheckClick();
        }
    }

    private void CheckClick()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 更新边界（以防图片移动）
        imageBounds = targetImage.bounds;

        // 检查是否点击在图片上
        if (!imageBounds.Contains(worldPos))
        {
            return;
        }

        // 计算归一化坐标
        Vector2 normalizedPos = GetNormalizedPosition(worldPos);

        clickCount++;

        // 输出到 Console
        string output = $"<color=cyan>[SpotPositionHelper]</color> 点击 #{clickCount}\n" +
                       $"  <color=yellow>归一化坐标: ({normalizedPos.x:F3}, {normalizedPos.y:F3})</color>\n" +
                       $"  世界坐标: ({worldPos.x:F2}, {worldPos.y:F2})\n" +
                       $"  <color=lime>复制用: new Vector2({normalizedPos.x:F3}f, {normalizedPos.y:F3}f)</color>";

        Debug.Log(output);

        // 显示标记
        if (showClickMarker)
        {
            ShowMarker(worldPos);
        }
    }

    private Vector2 GetNormalizedPosition(Vector2 worldPos)
    {
        float x = (worldPos.x - imageBounds.min.x) / imageBounds.size.x;
        float y = (worldPos.y - imageBounds.min.y) / imageBounds.size.y;
        return new Vector2(x, y);
    }

    private void ShowMarker(Vector3 worldPosition)
    {
        // 清除之前的标记
        if (currentMarker != null)
        {
            Destroy(currentMarker);
        }

        // 创建新标记
        currentMarker = new GameObject($"ClickMarker_{clickCount}");
        currentMarker.transform.position = new Vector3(worldPosition.x, worldPosition.y, imageBounds.center.z - 0.02f);

        // 添加 SpriteRenderer
        SpriteRenderer sr = currentMarker.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = markerColor;
        sr.sortingLayerName = targetImage.sortingLayerName;
        sr.sortingOrder = targetImage.sortingOrder + 10;

        currentMarker.transform.localScale = Vector3.one * markerRadius * 2;
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color32[] pixels = new Color32[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = dist > radius - 2 ? (radius - dist) / 2f : 1f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * alpha));
                }
                else
                {
                    pixels[y * size + x] = new Color32(0, 0, 0, 0);
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 清除所有标记
    /// </summary>
    [ContextMenu("清除标记并重置计数")]
    public void ClearMarkers()
    {
        if (currentMarker != null)
        {
            if (Application.isPlaying)
                Destroy(currentMarker);
            else
                DestroyImmediate(currentMarker);
        }
        clickCount = 0;
        Debug.Log("[SpotPositionHelper] 标记已清除，计数已重置");
    }

    /// <summary>
    /// 切换启用状态
    /// </summary>
    [ContextMenu("切换启用/禁用")]
    public void ToggleEnabled()
    {
        isEnabled = !isEnabled;
        Debug.Log($"[SpotPositionHelper] {(isEnabled ? "已启用" : "已禁用")}");
    }

    private void OnDrawGizmosSelected()
    {
        if (targetImage == null) return;

        // 绘制图片边界
        Bounds bounds = targetImage.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}