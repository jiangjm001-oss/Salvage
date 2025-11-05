// Assets/Scripts/Animation/AnimationManager.cs
using UnityEngine;
using UnityEngine.Events;

public class AnimationManager : MonoBehaviour
{
    // 定义静态事件，供 GameManager 订阅
    public static UnityEvent OnEndingAnimationFinished = new UnityEvent();

    // 这是一个示例函数，当结局动画播放完毕时（例如，在 Animation Clip 的末尾事件中调用）
    public void EndGameAnimationFinished()
    {
        Debug.Log("AnimationManager: Ending animation finished. Triggering event.");
        OnEndingAnimationFinished.Invoke();
    }
}