// Assets/Scripts/GamePlay/Notebook/NotebookPageIndicator.cs
// 笔记本页码指示器 - 显示当前页面位置的小点
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 笔记本页码指示器
/// 使用小点显示当前页面在总页数中的位置
/// </summary>
public class NotebookPageIndicator : MonoBehaviour
{
    [Header("关联控制器")]
    [Tooltip("笔记本控制器引用")]
    public NotebookController notebookController;

    [Header("指示器设置")]
    [Tooltip("指示器点预制体")]
    public GameObject dotPrefab;

    [Tooltip("点之间的间距")]
    public float dotSpacing = 30f;

    [Tooltip("当前页面点的颜色")]
    public Color activeColor = Color.white;

    [Tooltip("非当前页面点的颜色")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.4f);

    [Tooltip("点的大小（当前页）")]
    public float activeScale = 1.2f;

    [Tooltip("点的大小（非当前页）")]
    public float inactiveScale = 1f;

    [Tooltip("切换动画时间")]
    public float animationDuration = 0.2f;

    // 内部状态
    private List<SpriteRenderer> dots = new List<SpriteRenderer>();
    private int lastIndex = -1;

    private void Start()
    {
        if (notebookController == null)
        {
            notebookController = GetComponentInParent<NotebookController>();
        }

        if (notebookController != null)
        {
            // 订阅页面变化事件
            notebookController.OnPageChanged.AddListener(OnPageChanged);

            // 创建指示器点
            CreateDots();

            // 初始化显示
            UpdateIndicator(notebookController.CurrentSpreadIndex);
        }
        else
        {
            Debug.LogWarning("[NotebookPageIndicator] 未找到 NotebookController！");
        }
    }

    private void OnDestroy()
    {
        if (notebookController != null)
        {
            notebookController.OnPageChanged.RemoveListener(OnPageChanged);
        }
    }

    /// <summary>
    /// 创建指示器点
    /// </summary>
    private void CreateDots()
    {
        // 清除旧的点
        foreach (var dot in dots)
        {
            if (dot != null)
            {
                Destroy(dot.gameObject);
            }
        }
        dots.Clear();

        int totalPages = notebookController.TotalSpreads;
        if (totalPages <= 0) return;

        // 计算起始位置（居中排列）
        float totalWidth = (totalPages - 1) * dotSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < totalPages; i++)
        {
            GameObject dotObj;

            if (dotPrefab != null)
            {
                dotObj = Instantiate(dotPrefab, transform);
            }
            else
            {
                // 如果没有预制体，创建一个简单的圆点
                dotObj = CreateDefaultDot();
            }

            dotObj.transform.localPosition = new Vector3(startX + i * dotSpacing, 0f, 0f);
            dotObj.name = $"Dot_{i}";

            SpriteRenderer renderer = dotObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                dots.Add(renderer);
            }
        }
    }

    /// <summary>
    /// 创建默认圆点
    /// </summary>
    private GameObject CreateDefaultDot()
    {
        GameObject dotObj = new GameObject("Dot");
        dotObj.transform.SetParent(transform);

        SpriteRenderer renderer = dotObj.AddComponent<SpriteRenderer>();

        // 创建一个简单的圆形精灵（使用Unity内置的圆形）
        // 或者使用纯白色精灵
        renderer.sprite = CreateCircleSprite();
        renderer.sortingOrder = 100;

        return dotObj;
    }

    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        // 创建一个8x8的圆形纹理
        int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance <= radius)
                {
                    // 边缘抗锯齿
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 页面变化回调
    /// </summary>
    private void OnPageChanged(int newIndex)
    {
        UpdateIndicator(newIndex);
    }

    /// <summary>
    /// 更新指示器显示
    /// </summary>
    private void UpdateIndicator(int currentIndex)
    {
        if (dots.Count == 0) return;

        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i] == null) continue;

            bool isActive = (i == currentIndex);

            // 直接设置或使用动画
            if (animationDuration > 0 && lastIndex >= 0)
            {
                StartCoroutine(AnimateDot(dots[i], isActive));
            }
            else
            {
                dots[i].color = isActive ? activeColor : inactiveColor;
                dots[i].transform.localScale = Vector3.one * (isActive ? activeScale : inactiveScale);
            }
        }

        lastIndex = currentIndex;
    }

    /// <summary>
    /// 点切换动画
    /// </summary>
    private System.Collections.IEnumerator AnimateDot(SpriteRenderer dot, bool toActive)
    {
        Color startColor = dot.color;
        Color targetColor = toActive ? activeColor : inactiveColor;

        Vector3 startScale = dot.transform.localScale;
        Vector3 targetScale = Vector3.one * (toActive ? activeScale : inactiveScale);

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            dot.color = Color.Lerp(startColor, targetColor, t);
            dot.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        dot.color = targetColor;
        dot.transform.localScale = targetScale;
    }

    /// <summary>
    /// 刷新指示器（当页数变化时调用）
    /// </summary>
    public void RefreshIndicator()
    {
        CreateDots();
        if (notebookController != null)
        {
            UpdateIndicator(notebookController.CurrentSpreadIndex);
        }
    }
}