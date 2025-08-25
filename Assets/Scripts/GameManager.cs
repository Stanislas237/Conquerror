using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Données des joueurs
    private readonly Dictionary<string, object> PlayerDatas = new()
    {
        { "playersPositions", new HashSet<int>[4] { new(), new(), new(), new() }},
        { "nbTurnToPass", new int[4] },
        { "conquerPoints", new int[4] },
        { "PlayerColors", new Material[4] },
    };

    private readonly List<Block> Blocks = new();

    private readonly List<int> SelectedBlocks = new();

    private int SelectionLevel { get => SelectedBlocks.Sum(b => Blocks[b].Level + 1); }

    private int CurrentPlayerId = 0;

    // private int totalTurns = 0;

    // Matériaux / Couleurs
    private Material hoverColor;
    private Material myHoverColor;
    private Material normalColor;

    // Données de l'éditeur
    [Range(2, 4)]
    public int nb_players = 2;

    public bool alt_mode = false;

    public bool simulateNeighbors = true;

    public Vector2Int terrainSize = new();

    public int terrainRay = 5;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        PlayerDatas["PlayerColors"] = Resources.LoadAll<Material>("Materials/Players");
        hoverColor = Resources.Load<Material>("Materials/Terrain");
        myHoverColor = Resources.Load<Material>("Materials/myHover");
        normalColor = Resources.Load<Material>("Materials/Block");

        GenerateBlocks();
        Draw();
        NextPlayerTurn();

        transform.GetChild(2).localScale = new(terrainSize.x, 1, terrainSize.y);
        UIManager.Instance.Fusion = Fusion;
    }

    #region Block Management Handlers
    
    public void OnBlockEnter(Block block)
    {
        if (!SelectedBlocks.Contains(block.myOwnIndex))
            if (block.OwnerId == CurrentPlayerId)
                block.SetColor(myHoverColor);
            else if (block.OwnerId == -1 && GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))
                block.SetColor(hoverColor);
    }

    public void OnBlockDown(Block block)
    {
        if (block.OwnerId == CurrentPlayerId)
            Select(block);
        else if (block.OwnerId == -1 && GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))
            Conquer(block);
    }

    public void OnBlockExit(Block block)
    {
        if (!SelectedBlocks.Contains(block.myOwnIndex) && ((block.OwnerId == CurrentPlayerId) || (block.OwnerId == -1 && GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))))
            block.SetColor(normalColor);
    }

    #endregion


    #region Player Datas Getters

    private HashSet<int> GetPositions(int i) => ((HashSet<int>[])PlayerDatas["playersPositions"])[i];
    private HashSet<int> GetPositions() => GetPositions(CurrentPlayerId);

    private int[] GetPassTurns() => (int[])PlayerDatas["nbTurnToPass"];

    private int[] GetConquerPoints() => (int[])PlayerDatas["conquerPoints"];

    private Material[] GetColors() => (Material[])PlayerDatas["PlayerColors"];

    #endregion


    #region Game Management And Status Methods

    public bool IsBlockCircled(Block block) => !block.NeighborsIndexes.Any(nId => Blocks[nId].OwnerId == -1);

    private void SetColor(Block block, Material m, PaintMode paintMode)
    {
        if (paintMode != PaintMode.onlyPawn)
            block.SetColor(m);

        if (paintMode != PaintMode.onlyBlock)
            block.SetContentColor(m);
    }

    private void Draw()
    {
        UIManager.Instance.ShowPowers(SelectionLevel, SelectedBlocks.Count, GetConquerPoints()[CurrentPlayerId]);

        var indexesAlreadyModified = new List<int>();
        for (int i = 0; i < nb_players; i++)
            foreach (var index in GetPositions(i))
            {
                var block = Blocks[index];
                bool neighborSelected = SelectedBlocks.Contains(block.myOwnIndex);

                SetColor(block, GetColors()[i], PaintMode.onlyPawn);
                SetColor(block, neighborSelected ? myHoverColor : normalColor, PaintMode.onlyBlock);
                indexesAlreadyModified.Add(block.myOwnIndex);

                if (simulateNeighbors)
                    foreach (var neighborIndex in block.GetExtendedNeighbors(Blocks))
                        if (Blocks[neighborIndex].OwnerId == -1 && !indexesAlreadyModified.Contains(neighborIndex))
                        {
                            if (neighborSelected) indexesAlreadyModified.Add(neighborIndex);
                            Blocks[neighborIndex].SetColor(neighborSelected ? hoverColor : normalColor, true);
                            Blocks[neighborIndex].SetColorState(!neighborSelected);
                        }
            }
    }

    private void NextPlayerTurn()
    {
        CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
        var nbTurnToPass = GetPassTurns();

        if (nbTurnToPass[CurrentPlayerId] > 0 || !GetPositions().Any(i => !IsBlockCircled(Blocks[i])))
        {
            nbTurnToPass[CurrentPlayerId] = Mathf.Max(nbTurnToPass[CurrentPlayerId] - 1, 0);
            NextPlayerTurn();
        }
        else
            UIManager.Instance.ShowPlayerUI(GetConquerPoints()[CurrentPlayerId], GetColors()[CurrentPlayerId]);
    }

    #endregion


    #region Player Actions Methods

    private void Free(Block block, bool removeOwner = false)
    {
        GetPositions(block.OwnerId).Remove(block.myOwnIndex);
        block.SetOwnerId(-1);
        block.SetLevel(0);
        block.Content.SetActive(false);
    }

    private void Conquer(Block block)
    {
        UnSelect();
        Conquer(block, block.Level + 1);
        Draw();
        NextPlayerTurn();
    }

    private void Conquer(Block block, int recursive)
    {
        if (block.Level > 0)
            block.SetLevel(0);
        else
        {
            if (block.OwnerId >= 0)
                GetPositions(block.OwnerId).Remove(block.myOwnIndex);

            GetPositions().Add(block.myOwnIndex);
            block.SetOwnerId(CurrentPlayerId);
        }

        if (recursive > 0)
            foreach (var blockId in block.NeighborsIndexes)
            {
                var otherBlock = Blocks[blockId];
                if (otherBlock.OwnerId >= 0 && otherBlock.OwnerId != CurrentPlayerId)
                {
                    Conquer(otherBlock, recursive - 1);
                    GetConquerPoints()[CurrentPlayerId]++;
                }
            }
    }

    private void UnSelect(Block block)
    {
        SelectedBlocks.Remove(block.myOwnIndex);
        var tempList = SelectedBlocks.ToList();
        SelectedBlocks.Clear();

        for (int i = 0; i < tempList.Count; i++)
            Select(Blocks[tempList[i]]);
    }

    private void UnSelect()
    {
        SelectedBlocks.Clear();
        Draw();
    }

    private void Select(Block block)
    {
        if (SelectedBlocks.Contains(block.myOwnIndex))
            UnSelect(block);
        else
        {
            if (SelectedBlocks.Count != 0 && !Blocks[SelectedBlocks[SelectedBlocks.Count - 1]].NeighborsIndexes.Contains(block.myOwnIndex))
                SelectedBlocks.Clear();
            SelectedBlocks.Add(block.myOwnIndex);
        }
        Draw();
    }

    private void Fusion()
    {
        var LevelTartget = SelectionLevel - 1;
        var tempList = SelectedBlocks.ToList();
        UnSelect();

        for (int i = 0; i < tempList.Count - 1; i++)
            Free(Blocks[tempList[i]]);
        var block = Blocks[tempList[tempList.Count - 1]];
        block.SetLevel(LevelTartget);
        NextPlayerTurn();
    }

    #endregion


    #region Terrain Generation Methods

    private void GenerateBlocks()
    {
        switch (nb_players)
        {
            case 2:
                GenerateRectangle();
                break;
            case 3:
                GenerateHexagon();
                break;
            case 4:
                GeneratePlus();
                break;
        }
    }

    private void GenerateRectangle()
    {
        GameObject PrefabToSpawn = transform.GetChild(0).gameObject;
        int X = 5 * terrainSize.x, Y = 5 * terrainSize.y;
        int maxSize = terrainSize.x * terrainSize.y;

        int GetIndex(int i, int j) => (j + Y + terrainSize.y * (i + X)) / 10;

        for (int i = -X; i < X; i += 10)
            for (int j = -Y; j < Y; j += 10)
            {
                var obj = Instantiate(PrefabToSpawn, new(i + 5, 1, j + 5), Quaternion.identity, transform);
                obj.SetActive(true);
                obj.name = $"({i}, {j})";
                var block = obj.AddComponent<Block>();

                if (Blocks.Count == 0 || Blocks.Count == terrainSize.y - 1 || Blocks.Count == maxSize - terrainSize.y || Blocks.Count == maxSize - 1)
                {
                    GetPositions().Add(Blocks.Count);
                    block.SetOwnerId(CurrentPlayerId);
                    CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
                }

                block.myOwnIndex = Blocks.Count;
                Blocks.Add(block);

                var NeighborsIndexes = new Vector2Int[4] { new(i - 10, j), new(i + 10, j), new(i, j + 10), new(i, j - 10) };
                if (!alt_mode) NeighborsIndexes = NeighborsIndexes.Concat(new Vector2Int[4] { new(i - 10, j - 10), new(i - 10, j + 10), new(i + 10, j + 10), new(i + 10, j - 10) }).ToArray();

                foreach (var coordinates in NeighborsIndexes)
                {
                    if (coordinates.x < -X || coordinates.x >= X || coordinates.y < -Y || coordinates.y >= Y) continue;

                    var index = GetIndex(coordinates.x, coordinates.y);
                    if (index >= 0 && index < maxSize)
                        block.NeighborsIndexes.Add(index);
                }
            }
    }

    private void GenerateHexagon()
    {
        GameObject PrefabToSpawn = transform.GetChild(1).gameObject;
        terrainSize = Vector2Int.one * (2 * terrainRay + 1);
        int R = 10 * terrainRay;
        int maxSize = 1 + 3 * terrainRay * (terrainRay + 1);
        int player_Id_increment_to_set_alt_mode_start_positions = 1;

        int GetIndex(int i, int j)
        {
            int prevBlocks = 0;
            for (int k = -R; k < i; k += 10)
                prevBlocks += terrainRay + 1 + Mathf.Abs(terrainRay - Mathf.Abs(k / 10));
            int thresold = Mathf.FloorToInt((j + R) / 10f) - Mathf.FloorToInt(Mathf.Abs(i) / 20f);

            if (thresold >= 0 && thresold <= (2 * terrainRay) - (Mathf.Abs(i) / 10))
                return prevBlocks + thresold;
            else return -1;
        }

        for (int i = -R; i <= R; i += 10)
        {
            int nbBlocks = R + 2 + Mathf.Abs(R - Mathf.Abs(i));
            for (int j = 1 - nbBlocks / 2; j < nbBlocks / 2; j += 10)
            {
                var obj = Instantiate(PrefabToSpawn, new(i, 1, j), PrefabToSpawn.transform.rotation, transform);
                obj.SetActive(true);
                obj.name = $"({i}, {j})";

                var block = obj.AddComponent<Block>();

                if (Blocks.Count == 0 || Blocks.Count == terrainRay || (i == 0 && Mathf.Abs(j) == R) || Blocks.Count == maxSize - terrainRay - 1 || Blocks.Count == maxSize - 1)
                {
                    GetPositions().Add(Blocks.Count);
                    block.SetOwnerId(CurrentPlayerId);

                    if (CurrentPlayerId == nb_players - 1 && player_Id_increment_to_set_alt_mode_start_positions == 1)
                        player_Id_increment_to_set_alt_mode_start_positions = -1;
                    else
                        CurrentPlayerId = (CurrentPlayerId + player_Id_increment_to_set_alt_mode_start_positions) % nb_players;
                }

                block.myOwnIndex = Blocks.Count;
                Blocks.Add(block);

                var NeighborsIndexes = new Vector2Int[3] { new(i - 10, j + 5), new(i, j - 10), new(i + 10, j + 5) };
                if (!alt_mode) NeighborsIndexes = NeighborsIndexes.Concat(new Vector2Int[3] { new(i, j + 10), new(i + 10, j - 5), new(i - 10, j - 5) }).ToArray();

                foreach (var coordinates in NeighborsIndexes)
                {
                    if (coordinates.x < -R || coordinates.x > R || coordinates.y < -R || coordinates.y > R) continue;

                    var index = GetIndex(coordinates.x, coordinates.y);
                    if (index >= 0 && index < maxSize)
                        block.NeighborsIndexes.Add(index);
                }
            }
        }

        CurrentPlayerId = 0;
    }

    private void GeneratePlus()
    {
        GameObject PrefabToSpawn = transform.GetChild(0).gameObject;
        int X = 5 * terrainSize.x, Y = 5 * terrainSize.y;

        int minBound = Mathf.Min(terrainSize.x, terrainSize.y);
        int thicknessLength = (int)(minBound / 3);
        int thickness = thicknessLength * 10;
        int thresoldX = RoundUpToNearest10(X - thickness / 2), thresoldY = RoundUpToNearest10(Y - thickness / 2);
        int maxSize = (terrainSize.y + terrainSize.x - thicknessLength) * thicknessLength;

        int GetIndex(int i, int j)
        {
            int prevBlocks = 0;
            for (int k = -X; k < i; k += 10)
                prevBlocks += (k + X < thresoldX || k + X >= thresoldX + thickness) ? thicknessLength : terrainSize.y;
            return prevBlocks + (j + Y - (i + X < thresoldX || i + X >= thresoldX + thickness ? thresoldY : 0)) / 10;
        }

        static int RoundUpToNearest10(int value) => (value + 9) / 10 * 10;

        for (int i = -X; i < X; i += 10)
            for (int j = -Y; j < Y; j += 10)
            {
                if ((i + X < thresoldX || i + X >= thresoldX + thickness) && (j + Y < thresoldY || j + Y >= thresoldY + thickness)) continue;

                var obj = Instantiate(PrefabToSpawn, new(i + 5, 1, j + 5), Quaternion.identity, transform);
                obj.SetActive(true);
                obj.name = $"({i}, {j})";
                var block = obj.AddComponent<Block>();

                if (Blocks.Count == 0 || Blocks.Count == thicknessLength - 1 || ((i + X == thresoldX || i + X == thresoldX + thickness - 10) && (j == -Y || j == Y - 10)) || Blocks.Count == maxSize - thicknessLength || Blocks.Count == maxSize - 1)
                {
                    GetPositions().Add(Blocks.Count);
                    block.SetOwnerId(CurrentPlayerId);
                    CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
                }

                block.myOwnIndex = Blocks.Count;
                Blocks.Add(block);

                var NeighborsIndexes = new Vector2Int[4] { new(i - 10, j), new(i + 10, j), new(i, j + 10), new(i, j - 10) };
                if (!alt_mode) NeighborsIndexes = NeighborsIndexes.Concat(new Vector2Int[4] { new(i - 10, j - 10), new(i - 10, j + 10), new(i + 10, j + 10), new(i + 10, j - 10) }).ToArray();

                foreach (var coordinates in NeighborsIndexes)
                {
                    if (coordinates.x < -X || coordinates.x >= X || coordinates.y < -Y || coordinates.y >= Y) continue;
                    if ((coordinates.x + X < thresoldX || coordinates.x + X >= thresoldX + thickness) && (coordinates.y + Y < thresoldY || coordinates.y + Y >= thresoldY + thickness)) continue;

                    var index = GetIndex(coordinates.x, coordinates.y);
                    if (index >= 0 && index < maxSize)
                        block.NeighborsIndexes.Add(index);
                }
            }
    }
    
    #endregion
}

public enum PaintMode { onlyBlock, onlyPawn, all }
