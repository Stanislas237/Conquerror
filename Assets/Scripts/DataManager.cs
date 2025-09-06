using UnityEngine;
using System.Collections.Generic;

public class DataManager
{
    // public const int MaxNbUseOfContagion = 1;
    // public const int MaxNbUseOfCombo = 1;
    public const int requiredConquerPointsForSpecial = 30;
    public static DataManager Instance;

    public static Material normalColor;
    public static Material specialSelectionColor;

    private static readonly Dictionary<string, object> PlayerDatas = new();

    public DataManager()
    {
        PlayerDatas.TryAdd("PawnTypes", new PawnType[4] { PawnType.Conquerant, PawnType.Conquerant, PawnType.Conquerant, PawnType.Conquerant });
        PlayerDatas["playersPositions"] = new HashSet<int>[4] { new(), new(), new(), new() };
        PlayerDatas["conquerPoints"] = new int[4];
        PlayerDatas["Energy"] = new int[4];
        PlayerDatas["nbTurnToPass"] = new int[4];
        PlayerDatas["nbUseOfContagion"] = new int[4];
        PlayerDatas["nbUseOfCombo"] = new int[4];
        
        if (Instance == null)
        {
            PlayerDatas["PlayerColors"] = Resources.LoadAll<Material>("Materials/Players");
            PlayerDatas["PlayerHoverColors"] = Resources.LoadAll<Material>("Materials/PlayerHovers");
            normalColor = Resources.Load<Material>("Materials/Block");
            specialSelectionColor = Resources.Load<Material>("Materials/Terrain");
        }

        Instance = this;
    }

    public static HashSet<int> GetPositions(int i) => ((HashSet<int>[])PlayerDatas["playersPositions"])[i];
    public static HashSet<int> GetPositions() => GetPositions(GameManager.Instance.CurrentPlayerId);

    public static int GetNbPositionsOccuped()
    {
        var Positions = (HashSet<int>[])PlayerDatas["playersPositions"];
        int totalNbPositions = 0;
        foreach (var p in Positions)
            totalNbPositions += p.Count;
        return totalNbPositions;
    }

    public static int GetTheLongerPostitionsList()
    {
        int maxIndex = -1, maxCount = -1;
        var Positions = (HashSet<int>[])PlayerDatas["playersPositions"];
        for (int i = 0; i < Positions.Length; i++)
            if (Positions[i].Count > maxCount)
            {
                maxCount = Positions[i].Count;
                maxIndex = i;
            }
        
        return maxIndex;
    }

    // Getters

    public static PawnType[] GetPawnTypes() => (PawnType[])PlayerDatas["PawnTypes"];

    public static int[] GetPassTurns() => (int[])PlayerDatas["nbTurnToPass"];

    public static int[] GetNbContagions() => (int[])PlayerDatas["nbUseOfContagion"];

    public static int[] GetNbCombos() => (int[])PlayerDatas["nbUseOfCombo"];

    public static int[] GetConquerPoints() => (int[])PlayerDatas["conquerPoints"];

    public static int[] GetEnergy() => (int[])PlayerDatas["Energy"];

    public static Material[] GetColors() => (Material[])PlayerDatas["PlayerColors"];

    public static Material[] GetHoverColors() => (Material[])PlayerDatas["PlayerHoverColors"];


    // Setters

    public static void SetPawnType(int i, PawnType pawnType)
    {
        PlayerDatas["PawnTypes"] ??= new PawnType[4] { PawnType.Conquerant, PawnType.Conquerant, PawnType.Conquerant, PawnType.Conquerant };
        GetPawnTypes()[i] = pawnType;
    }
}

public enum PawnType { Conquerant, Voyageur, Gardien, Archiviste }
