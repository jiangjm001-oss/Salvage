// Assets/Scripts/Gameplay/PuzzleManager.cs
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    // 定义静态事件，供 GameManager 订阅
    public static UnityEvent OnLevel1PuzzleCompleted = new UnityEvent();
    public static UnityEvent OnLevel2PuzzleCompleted = new UnityEvent();

    // 这是一个示例函数，当关卡1的谜题条件满足时（例如，玩家使用了身份卡）
    // 你需要在游戏逻辑的某个地方调用它
    public void CompleteLevel1Puzzle()
    {
        Debug.Log("PuzzleManager: Level 1 puzzle conditions met. Triggering event.");
        OnLevel1PuzzleCompleted.Invoke();
    }

    // 关卡2的谜题完成函数
    public void CompleteLevel2Puzzle()
    {
        Debug.Log("PuzzleManager: Level 2 puzzle conditions met. Triggering event.");
        OnLevel2PuzzleCompleted.Invoke();
    }
}