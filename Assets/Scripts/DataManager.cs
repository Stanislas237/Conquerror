using UnityEngine;
using System.Collections.Generic;

public class DataManager
{
    public const int MaxNbUseOfContagion = 1;
    public const int MaxNbUseOfCombo = 1;
    public static DataManager Instance;

    public static Material normalColor;
    public static Material specialSelectionColor;

    private readonly Dictionary<string, object> PlayerDatas = new()
    {
        { "playersPositions", new HashSet<int>[4] { new(), new(), new(), new() }},
        { "conquerPoints", new int[4] {50, 50, 50, 50} },
        { "PlayerColors", new Material[4] },
        { "PlayerHoverColors", new Material[4] },
        { "nbTurnToPass", new int[4] },
        { "nbUseOfContagion", new int[4] },
        { "nbUseOfCombo", new int[4] },
    };

    public DataManager()
    {
        Instance = this;

        PlayerDatas["PlayerColors"] = Resources.LoadAll<Material>("Materials/Players");
        PlayerDatas["PlayerHoverColors"] = Resources.LoadAll<Material>("Materials/PlayerHovers");
        normalColor = Resources.Load<Material>("Materials/Block");
        specialSelectionColor = Resources.Load<Material>("Materials/Terrain");
    }

    public static HashSet<int> GetPositions(int i) => ((HashSet<int>[])Instance.PlayerDatas["playersPositions"])[i];
    public static HashSet<int> GetPositions() => GetPositions(GameManager.Instance.CurrentPlayerId);

    public static int[] GetPassTurns() => (int[])Instance.PlayerDatas["nbTurnToPass"];

    public static int[] GetNbContagions() => (int[])Instance.PlayerDatas["nbUseOfContagion"];

    public static int[] GetNbCombos() => (int[])Instance.PlayerDatas["nbUseOfCombo"];

    public static int[] GetConquerPoints() => (int[])Instance.PlayerDatas["conquerPoints"];

    public static Material[] GetColors() => (Material[])Instance.PlayerDatas["PlayerColors"];

    public static Material[] GetHoverColors() => (Material[])Instance.PlayerDatas["PlayerHoverColors"];

}
