// Assets/Scripts/GamePlay/Lv2/AlcoholLampSaveData.cs
// 酒精灯实验存档数据结构
using System;

/// <summary>
/// 酒精灯实验存档数据
/// </summary>
[Serializable]
public class AlcoholLampSaveData
{
    public string experimentID;     // 实验的唯一标识符
    public int currentStage;        // 当前阶段索引

    public AlcoholLampSaveData() { }

    public AlcoholLampSaveData(string id, int stage)
    {
        experimentID = id;
        currentStage = stage;
    }
}