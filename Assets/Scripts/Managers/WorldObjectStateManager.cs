// Assets/Scripts/Managers/WorldObjectStateManager.cs
// 世界物品状态管理器 - 解决全景视图与放大视图物品不同步问题
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 世界物品状态管理器
/// 追踪所有可交互物品的状态，确保不同视图间状态同步
/// </summary>
public class WorldObjectStateManager : MonoBehaviour
{
    public static WorldObjectStateManager Instance { get; private set; }

    // ============ 事件 ============

    /// <summary>
    /// 当物品状态改变时触发
    /// 参数: (objectID, isActive) - isActive为false表示物品被拾取/隐藏
    /// </summary>
    public static event Action<string, bool> OnObjectStateChanged;

    /// <summary>
    /// 当物品被拾取时触发（专门用于拾取事件）
    /// 参数: objectID
    /// </summary>
    public static event Action<string> OnObjectPickedUp;

    // ============ 状态存储 ============

    // 存储所有物品的激活状态 (objectID -> isActive)
    private Dictionary<string, bool> objectStates = new Dictionary<string, bool>();

    // ============ 生命周期 ============

    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[WorldObjectStateManager] 检测到重复实例，销毁当前组件");
            Destroy(this);
            return;
        }
        Instance = this;

        Debug.Log("[WorldObjectStateManager] ✓ 实例初始化成功");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 注册物品状态（在物品初始化时调用）
    /// </summary>
    /// <param name="objectID">物品唯一ID</param>
    /// <param name="isActive">初始激活状态</param>
    public void RegisterObject(string objectID, bool isActive)
    {
        if (string.IsNullOrEmpty(objectID))
        {
            Debug.LogWarning("[WorldObjectStateManager] 尝试注册空ID的物品");
            return;
        }

        if (!objectStates.ContainsKey(objectID))
        {
            objectStates[objectID] = isActive;
            Debug.Log($"[WorldObjectStateManager] 注册物品: {objectID}, 状态: {(isActive ? "激活" : "隐藏")}");
        }
    }

    /// <summary>
    /// 设置物品状态并通知所有监听者
    /// </summary>
    /// <param name="objectID">物品唯一ID</param>
    /// <param name="isActive">新的激活状态</param>
    public void SetObjectState(string objectID, bool isActive)
    {
        if (string.IsNullOrEmpty(objectID))
        {
            Debug.LogWarning("[WorldObjectStateManager] 尝试设置空ID物品的状态");
            return;
        }

        bool wasChanged = false;

        // 检查状态是否真的改变了
        if (objectStates.TryGetValue(objectID, out bool currentState))
        {
            wasChanged = currentState != isActive;
        }
        else
        {
            wasChanged = true; // 新物品
        }

        // 更新状态
        objectStates[objectID] = isActive;

        if (wasChanged)
        {
            Debug.Log($"[WorldObjectStateManager] 物品状态改变: {objectID} → {(isActive ? "激活" : "隐藏")}");

            // 触发状态改变事件
            OnObjectStateChanged?.Invoke(objectID, isActive);
        }
    }

    /// <summary>
    /// 标记物品为已拾取（会触发专门的拾取事件）
    /// </summary>
    /// <param name="objectID">物品唯一ID</param>
    public void MarkAsPickedUp(string objectID)
    {
        if (string.IsNullOrEmpty(objectID))
        {
            Debug.LogWarning("[WorldObjectStateManager] 尝试标记空ID物品为已拾取");
            return;
        }

        Debug.Log($"[WorldObjectStateManager] ★ 物品被拾取: {objectID}");

        // 设置状态为隐藏
        SetObjectState(objectID, false);

        // 触发拾取事件
        OnObjectPickedUp?.Invoke(objectID);
    }

    /// <summary>
    /// 获取物品当前状态
    /// </summary>
    /// <param name="objectID">物品唯一ID</param>
    /// <returns>物品是否激活，如果未注册返回true</returns>
    public bool GetObjectState(string objectID)
    {
        if (string.IsNullOrEmpty(objectID))
        {
            return true;
        }

        if (objectStates.TryGetValue(objectID, out bool isActive))
        {
            return isActive;
        }

        // 未注册的物品默认为激活状态
        return true;
    }

    /// <summary>
    /// 检查物品是否已被拾取
    /// </summary>
    /// <param name="objectID">物品唯一ID</param>
    /// <returns>是否已被拾取</returns>
    public bool IsObjectPickedUp(string objectID)
    {
        return !GetObjectState(objectID);
    }

    /// <summary>
    /// 批量设置物品状态（用于读取存档）
    /// </summary>
    /// <param name="pickedUpIDs">已拾取物品的ID列表</param>
    public void ApplyPickedUpStates(List<string> pickedUpIDs)
    {
        if (pickedUpIDs == null) return;

        Debug.Log($"[WorldObjectStateManager] 应用存档状态，已拾取物品数: {pickedUpIDs.Count}");

        foreach (string objectID in pickedUpIDs)
        {
            if (!string.IsNullOrEmpty(objectID))
            {
                // 直接设置状态，不触发存档保存
                objectStates[objectID] = false;

                // 但仍然触发事件，让相关物体同步状态
                OnObjectStateChanged?.Invoke(objectID, false);
            }
        }
    }

    /// <summary>
    /// 获取所有已拾取物品的ID列表（用于存档）
    /// </summary>
    /// <returns>已拾取物品ID列表</returns>
    public List<string> GetPickedUpObjectIDs()
    {
        List<string> pickedUpIDs = new List<string>();

        foreach (var kvp in objectStates)
        {
            if (!kvp.Value) // isActive == false 表示已拾取
            {
                pickedUpIDs.Add(kvp.Key);
            }
        }

        return pickedUpIDs;
    }

    /// <summary>
    /// 重置所有状态（用于新游戏）
    /// </summary>
    public void ResetAllStates()
    {
        Debug.Log("[WorldObjectStateManager] 重置所有物品状态");
        objectStates.Clear();
    }

    /// <summary>
    /// 获取当前追踪的物品数量（调试用）
    /// </summary>
    public int GetTrackedObjectCount()
    {
        return objectStates.Count;
    }

    /// <summary>
    /// 打印所有物品状态（调试用）
    /// </summary>
    [ContextMenu("打印所有物品状态")]
    public void DebugPrintAllStates()
    {
        Debug.Log("========== 世界物品状态 ==========");
        foreach (var kvp in objectStates)
        {
            Debug.Log($"  [{kvp.Key}] = {(kvp.Value ? "激活" : "隐藏")}");
        }
        Debug.Log($"========== 共 {objectStates.Count} 个物品 ==========");
    }
}