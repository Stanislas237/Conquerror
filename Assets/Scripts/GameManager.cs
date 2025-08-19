using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private readonly List<Block>[] playersPositions = new List<Block>[4] { new(), new(), new(), new() };
    private Material[] PlayerColors;
    private int CurrentPlayerId = 0;

    [HideInInspector]
    public Material hoverColor;
    [HideInInspector]
    public Material normalColor;
    [HideInInspector]
    public List<Block> Blocks = new();

    [Range(2, 4)]
    public int nb_players = 2;

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
        PlayerColors = Resources.LoadAll<Material>("Materials/Players");
        hoverColor = Resources.Load<Material>("Materials/Terrain");
        normalColor = Resources.Load<Material>("Materials/Block");

        GenerateBlocks();
        Draw();

        transform.GetChild(2).localScale = new(terrainSize.x, 1, terrainSize.y);
    }

    public List<Block> GetPositions() => playersPositions[CurrentPlayerId];

    private void Draw()
    {
        for (int i = 0; i < nb_players; i++)
            foreach (var block in playersPositions[i])
                block.SetColor(PlayerColors[i]);
    }

    public void Conquer(Block block)
    {
        Conquer(block, true);
        Draw();

        CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
    }

    private void Conquer(Block block, bool recursive)
    {
        block.ownerId = CurrentPlayerId;
        playersPositions[CurrentPlayerId].Add(block);

        if (recursive)
            foreach (var blockId in block.NeighborsIndexes)
            {
                var otherBlock = Blocks[blockId];
                if (otherBlock.ownerId >= 0 && otherBlock.ownerId != CurrentPlayerId)
                {
                    playersPositions[otherBlock.ownerId].Remove(otherBlock);
                    Conquer(otherBlock, false);
                }
            }
    }

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
                    playersPositions[CurrentPlayerId].Add(block);
                    block.ownerId = CurrentPlayerId;
                    CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
                }

                block.myOwnIndex = Blocks.Count;
                Blocks.Add(block);

                foreach (var coordinates in new Vector2Int[8] { new(i - 10, j - 10), new(i - 10, j + 10), new(i - 10, j), new(i + 10, j + 10), new(i + 10, j - 10), new(i + 10, j), new(i, j + 10), new(i, j - 10) })
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
                    playersPositions[CurrentPlayerId].Add(block);
                    block.ownerId = CurrentPlayerId;
                    CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
                }

                block.myOwnIndex = Blocks.Count;
                Blocks.Add(block);

                foreach (var coordinates in new Vector2Int[6] { new(i - 10, j + 5), new(i - 10, j - 5), new(i, j - 10), new(i, j + 10), new(i + 10, j - 5), new(i + 10, j + 5) })
                {
                    if (coordinates.x < -R || coordinates.x > R || coordinates.y < -R || coordinates.y > R) continue;

                    var index = GetIndex(coordinates.x, coordinates.y);
                    if (index >= 0 && index < maxSize)
                        block.NeighborsIndexes.Add(index);
                }
            }
        }

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
                    playersPositions[CurrentPlayerId].Add(block);
                    block.ownerId = CurrentPlayerId;
                    CurrentPlayerId = (CurrentPlayerId + 1) % nb_players;
                }

                block.myOwnIndex = Blocks.Count;
                Blocks.Add(block);

                foreach (var coordinates in new Vector2Int[8] { new(i - 10, j - 10), new(i - 10, j + 10), new(i - 10, j), new(i + 10, j + 10), new(i + 10, j - 10), new(i + 10, j), new(i, j + 10), new(i, j - 10) })
                {
                    if (coordinates.x < -X || coordinates.x >= X || coordinates.y < -Y || coordinates.y >= Y) continue;
                    if ((coordinates.x + X < thresoldX || coordinates.x + X >= thresoldX + thickness) && (coordinates.y + Y < thresoldY || coordinates.y + Y >= thresoldY + thickness)) continue;

                    var index = GetIndex(coordinates.x, coordinates.y);
                    if (index >= 0 && index < maxSize)
                        block.NeighborsIndexes.Add(index);
                }
            }
    }
}
