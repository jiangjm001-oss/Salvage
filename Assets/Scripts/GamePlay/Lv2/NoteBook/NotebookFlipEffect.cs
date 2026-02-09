// Assets/Scripts/GamePlay/Notebook/NotebookFlipEffect.cs
// 高级翻页特效 - 支持3D透视翻页效果和页面弯曲
using UnityEngine;
using System.Collections;

/// <summary>
/// 笔记本高级翻页特效
/// 提供更真实的翻书视觉效果
/// </summary>
public class NotebookFlipEffect : MonoBehaviour
{
    [Header("关联控制器")]
    [Tooltip("笔记本控制器引用")]
    public NotebookController notebookController;

    [Header("翻页页面")]
    [Tooltip("翻页动画用的页面 Transform")]
    public Transform flipPageTransform;

    [Tooltip("翻页页面正面 SpriteRenderer")]
    public SpriteRenderer flipPageFront;

    [Tooltip("翻页页面背面 SpriteRenderer（可选）")]
    public SpriteRenderer flipPageBack;

    [Header("翻页动画参数")]
    [Tooltip("翻页时间")]
    public float flipDuration = 0.5f;

    [Tooltip("翻页动画曲线")]
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("页面旋转轴位置（相对于页面中心的偏移）")]
    public Vector3 pivotOffset = new Vector3(-100f, 0f, 0f);

    [Header("3D透视效果")]
    [Tooltip("启用3D透视效果")]
    public bool enable3DEffect = true;

    [Tooltip("透视深度（Z轴偏移）")]
    public float perspectiveDepth = 50f;

    [Tooltip("页面弯曲强度")]
    [Range(0f, 1f)]
    public float bendIntensity = 0.3f;

    [Header("阴影效果")]
    [Tooltip("书脊阴影")]
    public SpriteRenderer spineShader;

    [Tooltip("翻页阴影")]
    public SpriteRenderer pageShadow;

    [Tooltip("最大阴影透明度")]
    [Range(0f, 1f)]
    public float maxShadowAlpha = 0.4f;

    [Header("粒子效果")]
    [Tooltip("翻页时的粒子效果（可选）")]
    public ParticleSystem pageParticles;

    // 内部状态
    private bool isFlipping = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Awake()
    {
        if (flipPageTransform != null)
        {
            originalPosition = flipPageTransform.localPosition;
            originalRotation = flipPageTransform.localRotation;
        }

        // 初始隐藏翻页页面
        if (flipPageFront != null)
        {
            flipPageFront.gameObject.SetActive(false);
        }
        if (flipPageBack != null)
        {
            flipPageBack.gameObject.SetActive(false);
        }

        // 初始化阴影
        if (spineShader != null)
        {
            SetSpriteAlpha(spineShader, 0f);
        }
        if (pageShadow != null)
        {
            SetSpriteAlpha(pageShadow, 0f);
        }
    }

    /// <summary>
    /// 执行向右翻页动画（翻到下一页）
    /// </summary>
    public IEnumerator FlipToNextPage(Sprite currentRightPage, Sprite nextLeftPage, System.Action onMidPoint, System.Action onComplete)
    {
        if (isFlipping) yield break;
        isFlipping = true;

        // 设置初始状态
        if (flipPageFront != null)
        {
            flipPageFront.sprite = currentRightPage;
            flipPageFront.gameObject.SetActive(true);
        }
        if (flipPageBack != null)
        {
            flipPageBack.sprite = nextLeftPage;
            flipPageBack.gameObject.SetActive(false);
        }

        // 播放粒子效果
        if (pageParticles != null)
        {
            pageParticles.Play();
        }

        float elapsed = 0f;
        bool midPointReached = false;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / flipDuration);

            // 计算翻页角度（0 -> 180度）
            float angle = t * 180f;

            // 应用旋转
            if (flipPageTransform != null)
            {
                // 绕Y轴旋转模拟翻页
                flipPageTransform.localRotation = originalRotation * Quaternion.Euler(0f, -angle, 0f);

                // 3D透视效果：翻页时Z轴偏移
                if (enable3DEffect)
                {
                    float zOffset = Mathf.Sin(t * Mathf.PI) * perspectiveDepth;
                    flipPageTransform.localPosition = originalPosition + new Vector3(0f, 0f, -zOffset);

                    // 页面弯曲效果（通过缩放模拟）
                    float scaleX = Mathf.Cos(angle * Mathf.Deg2Rad);
                    scaleX = Mathf.Abs(scaleX);
                    scaleX = Mathf.Max(scaleX, 0.1f); // 防止完全消失

                    // 添加弯曲变形
                    float bendEffect = Mathf.Sin(t * Mathf.PI) * bendIntensity;
                    flipPageTransform.localScale = new Vector3(
                        scaleX,
                        1f + bendEffect * 0.1f,
                        1f
                    );
                }
            }

            // 中点切换正反面
            if (t >= 0.5f && !midPointReached)
            {
                midPointReached = true;

                // 切换显示
                if (flipPageFront != null)
                {
                    flipPageFront.gameObject.SetActive(false);
                }
                if (flipPageBack != null)
                {
                    flipPageBack.gameObject.SetActive(true);
                }

                // 触发中点回调
                onMidPoint?.Invoke();
            }

            // 阴影效果
            UpdateShadows(t);

            yield return null;
        }

        // 重置状态
        ResetFlipPage();

        isFlipping = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 执行向左翻页动画（翻到上一页）
    /// </summary>
    public IEnumerator FlipToPreviousPage(Sprite currentLeftPage, Sprite prevRightPage, System.Action onMidPoint, System.Action onComplete)
    {
        if (isFlipping) yield break;
        isFlipping = true;

        // 移动到左侧位置
        if (flipPageTransform != null)
        {
            // 调整初始位置到左页位置
            flipPageTransform.localPosition = originalPosition + pivotOffset * 2f;
        }

        // 设置初始状态
        if (flipPageFront != null)
        {
            flipPageFront.sprite = currentLeftPage;
            flipPageFront.gameObject.SetActive(true);
        }
        if (flipPageBack != null)
        {
            flipPageBack.sprite = prevRightPage;
            flipPageBack.gameObject.SetActive(false);
        }

        // 播放粒子效果
        if (pageParticles != null)
        {
            pageParticles.Play();
        }

        float elapsed = 0f;
        bool midPointReached = false;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / flipDuration);

            // 计算翻页角度（0 -> -180度，向右翻）
            float angle = t * 180f;

            // 应用旋转
            if (flipPageTransform != null)
            {
                flipPageTransform.localRotation = originalRotation * Quaternion.Euler(0f, angle, 0f);

                if (enable3DEffect)
                {
                    float zOffset = Mathf.Sin(t * Mathf.PI) * perspectiveDepth;
                    flipPageTransform.localPosition = originalPosition + pivotOffset * 2f + new Vector3(0f, 0f, -zOffset);

                    float scaleX = Mathf.Cos(angle * Mathf.Deg2Rad);
                    scaleX = Mathf.Abs(scaleX);
                    scaleX = Mathf.Max(scaleX, 0.1f);

                    float bendEffect = Mathf.Sin(t * Mathf.PI) * bendIntensity;
                    flipPageTransform.localScale = new Vector3(
                        scaleX,
                        1f + bendEffect * 0.1f,
                        1f
                    );
                }
            }

            // 中点切换
            if (t >= 0.5f && !midPointReached)
            {
                midPointReached = true;

                if (flipPageFront != null)
                {
                    flipPageFront.gameObject.SetActive(false);
                }
                if (flipPageBack != null)
                {
                    flipPageBack.gameObject.SetActive(true);
                }

                onMidPoint?.Invoke();
            }

            UpdateShadows(t);

            yield return null;
        }

        ResetFlipPage();

        isFlipping = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 更新阴影效果
    /// </summary>
    private void UpdateShadows(float t)
    {
        // 书脊阴影：翻页时加深
        if (spineShader != null)
        {
            float spineAlpha = Mathf.Sin(t * Mathf.PI) * maxShadowAlpha * 0.5f;
            SetSpriteAlpha(spineShader, spineAlpha);
        }

        // 页面阴影：跟随翻页页面
        if (pageShadow != null)
        {
            float shadowAlpha = Mathf.Sin(t * Mathf.PI) * maxShadowAlpha;
            SetSpriteAlpha(pageShadow, shadowAlpha);

            // 阴影位置跟随
            if (flipPageTransform != null)
            {
                pageShadow.transform.position = flipPageTransform.position + new Vector3(5f, -5f, 1f);
            }
        }
    }

    /// <summary>
    /// 重置翻页页面状态
    /// </summary>
    private void ResetFlipPage()
    {
        if (flipPageTransform != null)
        {
            flipPageTransform.localPosition = originalPosition;
            flipPageTransform.localRotation = originalRotation;
            flipPageTransform.localScale = Vector3.one;
        }

        if (flipPageFront != null)
        {
            flipPageFront.gameObject.SetActive(false);
        }
        if (flipPageBack != null)
        {
            flipPageBack.gameObject.SetActive(false);
        }

        if (spineShader != null)
        {
            SetSpriteAlpha(spineShader, 0f);
        }
        if (pageShadow != null)
        {
            SetSpriteAlpha(pageShadow, 0f);
        }
    }

    /// <summary>
    /// 设置精灵透明度
    /// </summary>
    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color c = renderer.color;
        renderer.color = new Color(c.r, c.g, c.b, alpha);
    }

    /// <summary>
    /// 检查是否正在翻页
    /// </summary>
    public bool IsFlipping => isFlipping;
}