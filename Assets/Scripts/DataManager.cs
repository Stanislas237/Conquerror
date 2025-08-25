using UnityEngine;
using System.Collections.Generic;

public class DataManager
{
    public static DataManager Instance;

    public static Material normalColor;

    private readonly Dictionary<string, object> PlayerDatas = new()
    {
        { "playersPositions", new HashSet<int>[4] { new(), new(), new(), new() }},
        { "nbTurnToPass", new int[4] },
        { "conquerPoints", new int[4] },
        { "PlayerColors", new Material[4] },
        { "PlayerHoverColors", new Material[4] },
    };

    public DataManager()
    {
        Instance = this;

        PlayerDatas["PlayerColors"] = Resources.LoadAll<Material>("Materials/Players");
        PlayerDatas["PlayerHoverColors"] = Resources.LoadAll<Material>("Materials/PlayerHovers");
        normalColor = Resources.Load<Material>("Materials/Block");
    }

    public static HashSet<int> GetPositions(int i) => ((HashSet<int>[])Instance.PlayerDatas["playersPositions"])[i];
    public static HashSet<int> GetPositions() => GetPositions(GameManager.Instance.CurrentPlayerId);

    public static int[] GetPassTurns() => (int[])Instance.PlayerDatas["nbTurnToPass"];

    public static int[] GetConquerPoints() => (int[])Instance.PlayerDatas["conquerPoints"];

    public static Material[] GetColors() => (Material[])Instance.PlayerDatas["PlayerColors"];

    public static Material[] GetHoverColors() => (Material[])Instance.PlayerDatas["PlayerHoverColors"];

}
