// DebugActiveState.cs
using UnityEngine;

public class DebugActiveState : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log($"[{name}] Awake - activeSelf: {gameObject.activeSelf}");
    }

    private void OnEnable()
    {
        Debug.Log($"[{name}] OnEnable 被调用");
    }

    private void OnDisable()
    {
        Debug.Log($"[{name}] OnDisable 被调用");
        Debug.Log(System.Environment.StackTrace); // 打印调用堆栈，找出是谁禁用的
    }
}