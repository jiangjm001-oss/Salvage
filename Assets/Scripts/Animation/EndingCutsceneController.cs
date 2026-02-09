// Assets/Scripts/Animation/EndingCutsceneController.cs
// Lv2 第五水晶触发的结局演出控制器
// 完整版 - 包含所有图片切换、音效、动效和最终UI

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro;

/// <summary>
/// 结局演出控制器
/// 管理从第五水晶点击到游戏结束的完整演出流程
/// </summary>
public class EndingCutsceneController : MonoBehaviour
{
    public static EndingCutsceneController Instance { get; private set; }

    // ============ 演出阶段枚举 ============
    public enum CutscenePhase
    {
        Idle,           // 等待触发
        Blackout,       // 瞬间黑屏
        ImageA,         // 图片A显示（等待点击）
        ImageB,         // 图片B显示（等待点击）
        ImageC,         // 图片C显示（等待点击）
        ImageD,         // 图片D显示（等待点击）
        Vortex,         // 旋涡动画
        TVShutdown,     // 电视关机效果
        ImageF,         // 图片F显示
        ImageG,         // 图片G + 按钮显示
        Complete        // 演出完成
    }

    [Header("当前状态（只读）")]
    [SerializeField] private CutscenePhase currentPhase = CutscenePhase.Idle;
    public CutscenePhase CurrentPhase => currentPhase;

    // ============ UI引用 ============
    [Header("Canvas设置")]
    [Tooltip("演出使用的Canvas（需要覆盖全屏）")]
    public Canvas cutsceneCanvas;

    [Tooltip("黑色遮罩Image（用于渐变效果）")]
    public Image blackOverlay;

    [Header("图片A-D设置")]
    [Tooltip("图片A的Image组件")]
    public Image imageA;

    [Tooltip("图片B的Image组件")]
    public Image imageB;

    [Tooltip("图片C的Image组件")]
    public Image imageC;

    [Tooltip("图片D的Image组件")]
    public Image imageD;

    [Header("旋涡效果设置")]
    [Tooltip("旋涡图片E的Image组件")]
    public Image imageE_Vortex;

    [Tooltip("旋涡旋转速度（度/秒）")]
    public float vortexRotateSpeed = 180f;

    [Tooltip("旋涡旋转持续时间")]
    public float vortexRotateDuration = 1f;

    [Tooltip("旋涡缩小消失时间")]
    public float vortexShrinkDuration = 0.8f;

    [Tooltip("旋涡缩小曲线")]
    public AnimationCurve vortexShrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("电视关机效果设置")]
    [Tooltip("电视关机效果组件")]
    public TVShutdownEffect tvShutdownEffect;

    [Tooltip("如果没有TVShutdownEffect，使用这个Image")]
    public Image tvShutdownImage;

    [Tooltip("电视关机效果持续时间")]
    public float tvShutdownDuration = 0.5f;

    [Header("图片F-G设置")]
    [Tooltip("图片F的Image组件")]
    public Image imageF;

    [Tooltip("图片F显示时间")]
    public float imageFDisplayDuration = 3f;

    [Tooltip("图片G的Image组件")]
    public Image imageG;

    [Header("最终按钮设置")]
    [Tooltip("重新开始按钮")]
    public Button restartButton;

    [Tooltip("返回主菜单按钮")]
    public Button mainMenuButton;

    [Tooltip("退出游戏按钮")]
    public Button quitButton;

    [Tooltip("游戏标题图片")]
    public Image titleImage;

    [Tooltip("按钮容器（用于整体淡入）")]
    public CanvasGroup buttonsContainer;

    // ============ 动画时间设置 ============
    [Header("动画时间设置")]
    [Tooltip("黑屏延迟时间（显示图片A前）")]
    public float blackoutDelay = 1f;

    [Tooltip("图片渐入时间")]
    public float imageFadeInDuration = 1.5f;

    [Tooltip("图片瞬间切换时间（惊吓效果）")]
    public float imageSnapDuration = 0.05f;

    [Tooltip("图片缓缓切换时间")]
    public float imageCrossfadeDuration = 1f;

    [Tooltip("渐黑时间")]
    public float fadeToBlackDuration = 1f;

    [Tooltip("按钮淡入时间")]
    public float buttonsFadeInDuration = 1f;

    // ============ 音效设置 ============
    [Header("音效设置")]
    [Tooltip("黑屏重音音效")]
    public string blackoutSoundPath = "Audio/SFX/ending_boom";

    [Tooltip("惊吓音效（A→B）")]
    public string scareSoundPath = "Audio/SFX/ending_scare";

    [Tooltip("开玻璃柜门音效（B→C）")]
    public string cabinetOpenSoundPath = "Audio/SFX/cabinet_open";

    [Tooltip("放置音效（C→D）")]
    public string placeSoundPath = "Audio/SFX/item_place";

    [Tooltip("电视关机音效")]
    public string tvShutdownSoundPath = "Audio/SFX/tv_shutdown";

    [Tooltip("恐怖音效（图片F）")]
    public string horrorSoundPath = "Audio/SFX/ending_horror";

    // ============ 事件 ============
    [Header("事件")]
    public UnityEvent OnCutsceneStart;
    public UnityEvent OnCutsceneComplete;
    public UnityEvent OnRestartGame;
    public UnityEvent OnReturnToMenu;

    // ============ 私有变量 ============
    private bool isPlaying = false;
    private bool waitingForClick = false;
    private Coroutine currentCoroutine;

    // ============ 生命周期 ============

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[EndingCutsceneController] 检测到重复实例，销毁自身");
            Destroy(gameObject);
            return;
        }

        InitializeUI();
    }

    private void Start()
    {
        BindButtonEvents();
    }

    private void OnDestroy()
    {
        UnbindButtonEvents();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // 检测点击（仅在等待点击状态）
        if (waitingForClick && Input.GetMouseButtonDown(0))
        {
            OnScreenClicked();
        }
    }

    // ============ 初始化 ============

    private void InitializeUI()
    {
        // 隐藏所有UI元素
        SetImageAlpha(blackOverlay, 0f);
        SetImageAlpha(imageA, 0f);
        SetImageAlpha(imageB, 0f);
        SetImageAlpha(imageC, 0f);
        SetImageAlpha(imageD, 0f);
        SetImageAlpha(imageE_Vortex, 0f);
        SetImageAlpha(imageF, 0f);
        SetImageAlpha(imageG, 0f);
        SetImageAlpha(titleImage, 0f);

        if (tvShutdownImage != null)
        {
            SetImageAlpha(tvShutdownImage, 0f);
        }

        // 隐藏按钮
        if (buttonsContainer != null)
        {
            buttonsContainer.alpha = 0f;
            buttonsContainer.interactable = false;
            buttonsContainer.blocksRaycasts = false;
        }

        // 隐藏Canvas
        if (cutsceneCanvas != null)
        {
            cutsceneCanvas.gameObject.SetActive(false);
        }

        Debug.Log("[EndingCutsceneController] UI初始化完成");
    }

    private void BindButtonEvents()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void UnbindButtonEvents()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    // ============ 公共API ============

    /// <summary>
    /// 开始结局演出（由第五水晶触发）
    /// </summary>
    public void StartCutscene()
    {
        if (isPlaying)
        {
            Debug.LogWarning("[EndingCutsceneController] 演出已在进行中");
            return;
        }

        Debug.Log("[EndingCutsceneController] ========== 开始结局演出 ==========");

        isPlaying = true;
        currentPhase = CutscenePhase.Blackout;

        // 显示Canvas
        if (cutsceneCanvas != null)
        {
            cutsceneCanvas.gameObject.SetActive(true);
        }

        // 隐藏背包UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInventoryUI();
        }

        OnCutsceneStart?.Invoke();

        // 开始演出
        currentCoroutine = StartCoroutine(PlayCutsceneCoroutine());
    }

    /// <summary>
    /// 跳过当前演出（调试用）
    /// </summary>
    [ContextMenu("跳过演出")]
    public void SkipCutscene()
    {
        if (!isPlaying) return;

        StopAllCoroutines();
        ShowFinalUI();
    }

    // ============ 主演出协程 ============

    private IEnumerator PlayCutsceneCoroutine()
    {
        // ========== 阶段1：瞬间黑屏 ==========
        Debug.Log("[EndingCutsceneController] 阶段1: 瞬间黑屏");
        currentPhase = CutscenePhase.Blackout;

        // 瞬间变黑
        SetImageAlpha(blackOverlay, 1f);

        // 播放重音音效
        PlaySound(blackoutSoundPath);

        // 等待1秒
        yield return new WaitForSeconds(blackoutDelay);

        // ========== 阶段2：图片A渐入 ==========
        Debug.Log("[EndingCutsceneController] 阶段2: 图片A渐入");
        currentPhase = CutscenePhase.ImageA;

        yield return StartCoroutine(FadeInImage(imageA, imageFadeInDuration));

        // 等待点击
        yield return StartCoroutine(WaitForClickCoroutine());

        // ========== 阶段3：惊吓切换到图片B ==========
        Debug.Log("[EndingCutsceneController] 阶段3: 惊吓切换到图片B");
        currentPhase = CutscenePhase.ImageB;

        // 播放惊吓音效
        PlaySound(scareSoundPath);

        // 瞬间切换
        SetImageAlpha(imageA, 0f);
        SetImageAlpha(imageB, 1f);

        // 等待点击
        yield return StartCoroutine(WaitForClickCoroutine());

        // ========== 阶段4：缓缓切换到图片C ==========
        Debug.Log("[EndingCutsceneController] 阶段4: 切换到图片C");
        currentPhase = CutscenePhase.ImageC;

        // 播放开柜门音效
        PlaySound(cabinetOpenSoundPath);

        // 交叉淡入淡出
        yield return StartCoroutine(CrossfadeImages(imageB, imageC, imageCrossfadeDuration));

        // 等待点击
        yield return StartCoroutine(WaitForClickCoroutine());

        // ========== 阶段5：缓缓切换到图片D ==========
        Debug.Log("[EndingCutsceneController] 阶段5: 切换到图片D");
        currentPhase = CutscenePhase.ImageD;

        // 播放放置音效
        PlaySound(placeSoundPath);

        // 交叉淡入淡出
        yield return StartCoroutine(CrossfadeImages(imageC, imageD, imageCrossfadeDuration));

        // 等待点击
        yield return StartCoroutine(WaitForClickCoroutine());

        // ========== 阶段6：渐黑 → 旋涡动画 ==========
        Debug.Log("[EndingCutsceneController] 阶段6: 旋涡动画");
        currentPhase = CutscenePhase.Vortex;

        // 渐黑
        yield return StartCoroutine(FadeToBlack(fadeToBlackDuration));

        // 隐藏图片D
        SetImageAlpha(imageD, 0f);

        // 显示旋涡并开始旋转
        yield return StartCoroutine(PlayVortexAnimation());

        // ========== 阶段7：电视关机效果 ==========
        Debug.Log("[EndingCutsceneController] 阶段7: 电视关机效果");
        currentPhase = CutscenePhase.TVShutdown;

        yield return StartCoroutine(PlayTVShutdownEffect());

        // ========== 阶段8：图片F渐入 ==========
        Debug.Log("[EndingCutsceneController] 阶段8: 图片F显示");
        currentPhase = CutscenePhase.ImageF;

        // 播放恐怖音效
        PlaySound(horrorSoundPath);

        // 确保黑屏
        SetImageAlpha(blackOverlay, 1f);

        // 图片F渐入
        yield return StartCoroutine(FadeInImage(imageF, imageFadeInDuration));

        // 显示3秒
        yield return new WaitForSeconds(imageFDisplayDuration);

        // ========== 阶段9：过渡到图片G + 按钮 ==========
        Debug.Log("[EndingCutsceneController] 阶段9: 图片G + 最终UI");
        currentPhase = CutscenePhase.ImageG;

        // 图片F渐黑
        yield return StartCoroutine(FadeOutImage(imageF, fadeToBlackDuration));

        // 显示最终UI
        yield return StartCoroutine(ShowFinalUICoroutine());

        // ========== 演出完成 ==========
        Debug.Log("[EndingCutsceneController] ========== 结局演出完成 ==========");
        currentPhase = CutscenePhase.Complete;
        isPlaying = false;

        OnCutsceneComplete?.Invoke();
    }

    // ============ 等待点击 ============

    private IEnumerator WaitForClickCoroutine()
    {
        waitingForClick = true;

        // 等待一小段时间再允许点击（防止误触）
        yield return new WaitForSeconds(0.2f);

        // 等待直到点击
        while (waitingForClick)
        {
            yield return null;
        }
    }

    private void OnScreenClicked()
    {
        if (!waitingForClick) return;

        Debug.Log($"[EndingCutsceneController] 屏幕被点击，当前阶段: {currentPhase}");
        waitingForClick = false;
    }

    // ============ 图片动画协程 ============

    /// <summary>
    /// 图片渐入
    /// </summary>
    private IEnumerator FadeInImage(Image image, float duration)
    {
        if (image == null) yield break;

        image.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.SmoothStep(0f, 1f, t);
            SetImageAlpha(image, alpha);
            yield return null;
        }

        SetImageAlpha(image, 1f);
    }

    /// <summary>
    /// 图片渐出
    /// </summary>
    private IEnumerator FadeOutImage(Image image, float duration)
    {
        if (image == null) yield break;

        float elapsed = 0f;
        float startAlpha = image.color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            SetImageAlpha(image, alpha);
            yield return null;
        }

        SetImageAlpha(image, 0f);
        image.gameObject.SetActive(false);
    }

    /// <summary>
    /// 交叉淡入淡出
    /// </summary>
    private IEnumerator CrossfadeImages(Image fromImage, Image toImage, float duration)
    {
        if (fromImage == null || toImage == null) yield break;

        toImage.gameObject.SetActive(true);
        SetImageAlpha(toImage, 0f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            SetImageAlpha(fromImage, 1f - smoothT);
            SetImageAlpha(toImage, smoothT);

            yield return null;
        }

        SetImageAlpha(fromImage, 0f);
        SetImageAlpha(toImage, 1f);
        fromImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 渐黑效果
    /// </summary>
    private IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;
        float startAlpha = blackOverlay.color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, 1f, t);
            SetImageAlpha(blackOverlay, alpha);
            yield return null;
        }

        SetImageAlpha(blackOverlay, 1f);
    }

    // ============ 旋涡动画 ============

    private IEnumerator PlayVortexAnimation()
    {
        if (imageE_Vortex == null)
        {
            Debug.LogWarning("[EndingCutsceneController] 未设置旋涡图片");
            yield break;
        }

        // 显示旋涡
        imageE_Vortex.gameObject.SetActive(true);
        SetImageAlpha(imageE_Vortex, 1f);
        imageE_Vortex.rectTransform.localScale = Vector3.one;
        imageE_Vortex.rectTransform.localRotation = Quaternion.identity;

        // 阶段1：纯旋转
        float elapsed = 0f;
        while (elapsed < vortexRotateDuration)
        {
            elapsed += Time.deltaTime;

            // 顺时针旋转（负角度）
            float rotation = -vortexRotateSpeed * elapsed;
            imageE_Vortex.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            yield return null;
        }

        // 阶段2：边旋转边缩小
        float currentRotation = imageE_Vortex.rectTransform.localEulerAngles.z;
        elapsed = 0f;

        while (elapsed < vortexShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vortexShrinkDuration;

            // 继续旋转
            float rotation = currentRotation - vortexRotateSpeed * elapsed;
            imageE_Vortex.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            // 缩小
            float scale = vortexShrinkCurve.Evaluate(t);
            imageE_Vortex.rectTransform.localScale = Vector3.one * scale;

            // 同时淡出
            SetImageAlpha(imageE_Vortex, 1f - t);

            yield return null;
        }

        // 隐藏旋涡
        imageE_Vortex.gameObject.SetActive(false);
        SetImageAlpha(imageE_Vortex, 0f);
    }

    // ============ 电视关机效果 ============

    private IEnumerator PlayTVShutdownEffect()
    {
        // 播放关机音效
        PlaySound(tvShutdownSoundPath);

        // 如果有专门的TVShutdownEffect组件，使用它
        if (tvShutdownEffect != null)
        {
            tvShutdownEffect.Play();
            yield return new WaitForSeconds(tvShutdownDuration);
        }
        // 否则使用简易版本
        else if (tvShutdownImage != null)
        {
            yield return StartCoroutine(PlaySimpleTVShutdown());
        }
        else
        {
            // 没有任何组件，只等待一下
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 简易版电视关机效果（无专门组件时使用）
    /// </summary>
    private IEnumerator PlaySimpleTVShutdown()
    {
        if (tvShutdownImage == null) yield break;

        // 设置为白色横线
        tvShutdownImage.gameObject.SetActive(true);
        tvShutdownImage.color = Color.white;

        RectTransform rt = tvShutdownImage.rectTransform;
        Vector2 originalSize = rt.sizeDelta;

        // 阶段1：收缩成水平线
        float phase1Duration = tvShutdownDuration * 0.4f;
        float elapsed = 0f;

        // 设置初始状态：全屏
        rt.sizeDelta = new Vector2(Screen.width * 2f, Screen.height * 2f);

        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;

            // 高度快速缩小，宽度保持
            float height = Mathf.Lerp(Screen.height * 2f, 4f, Mathf.Pow(t, 0.5f));
            rt.sizeDelta = new Vector2(Screen.width * 2f, height);

            yield return null;
        }

        rt.sizeDelta = new Vector2(Screen.width * 2f, 4f);

        // 阶段2：横线闪烁并消失
        float phase2Duration = tvShutdownDuration * 0.3f;
        elapsed = 0f;

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase2Duration;

            // 宽度缩小
            float width = Mathf.Lerp(Screen.width * 2f, 0f, t);
            rt.sizeDelta = new Vector2(width, 4f);

            // 亮度闪烁
            float brightness = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.3f;
            tvShutdownImage.color = Color.white * brightness;

            yield return null;
        }

        // 阶段3：中心点闪一下
        float phase3Duration = tvShutdownDuration * 0.3f;
        rt.sizeDelta = new Vector2(20f, 20f);
        tvShutdownImage.color = Color.white * 2f; // HDR亮

        yield return new WaitForSeconds(phase3Duration * 0.5f);

        // 快速消失
        elapsed = 0f;
        while (elapsed < phase3Duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (phase3Duration * 0.5f);
            SetImageAlpha(tvShutdownImage, 1f - t);
            yield return null;
        }

        // 完全隐藏
        tvShutdownImage.gameObject.SetActive(false);
        rt.sizeDelta = originalSize;
    }

    // ============ 最终UI ============

    private IEnumerator ShowFinalUICoroutine()
    {
        // 显示图片G
        if (imageG != null)
        {
            yield return StartCoroutine(FadeInImage(imageG, imageFadeInDuration));
        }

        // 显示标题图片
        if (titleImage != null)
        {
            yield return StartCoroutine(FadeInImage(titleImage, imageFadeInDuration * 0.5f));
        }

        // 显示按钮
        if (buttonsContainer != null)
        {
            buttonsContainer.interactable = true;
            buttonsContainer.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < buttonsFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / buttonsFadeInDuration;
                buttonsContainer.alpha = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }

            buttonsContainer.alpha = 1f;
        }
    }

    private void ShowFinalUI()
    {
        currentPhase = CutscenePhase.ImageG;

        // 直接显示所有最终元素
        SetImageAlpha(blackOverlay, 1f);

        if (imageG != null)
        {
            imageG.gameObject.SetActive(true);
            SetImageAlpha(imageG, 1f);
        }

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(true);
            SetImageAlpha(titleImage, 1f);
        }

        if (buttonsContainer != null)
        {
            buttonsContainer.alpha = 1f;
            buttonsContainer.interactable = true;
            buttonsContainer.blocksRaycasts = true;
        }
    }

    // ============ 按钮回调 ============

    private void OnRestartClicked()
    {
        Debug.Log("[EndingCutsceneController] 重新开始游戏");

        OnRestartGame?.Invoke();

        // 删除存档
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
        }

        // 清空背包
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
        }

        // 加载Level1
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("Level1_Room");
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[EndingCutsceneController] 返回主菜单");

        OnReturnToMenu?.Invoke();

        // 加载主菜单
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("LandingPage");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[EndingCutsceneController] 退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============ 辅助方法 ============

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;

        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    private void PlaySound(string soundPath)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundPath))
        {
            AudioManager.Instance.PlaySFX(soundPath);
        }
    }

    // ============ 调试功能 ============

    [ContextMenu("测试演出")]
    private void TestCutscene()
    {
        StartCutscene();
    }
}
