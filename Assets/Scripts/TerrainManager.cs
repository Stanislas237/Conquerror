using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    public readonly List<Block> Blocks = new();

    public static int nb_players = 2;

    public static bool alt_mode = false;

    public static bool simulateNeighbors = true;

    public static Vector2Int terrainSize = new();

    public static int terrainRay = 5;

    public void GenerateBlocks()
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

        Draw();
        transform.GetChild(2).localScale = new(terrainSize.x, 1, terrainSize.y);
    }

    public void Draw()
    {
        UIManager.Instance.ShowPowers(GameManager.Instance.SelectedBlocks.Any(i => !Blocks[i].IsCircled()));

        var indexesAlreadyModified = new HashSet<int>();
        for (int i = 0; i < nb_players; i++)
            foreach (var index in DataManager.GetPositions(i))
            {
                var block = Blocks[index];
                bool neighborSelected = GameManager.Instance.SelectedBlocks.Contains(block.MyOwnIndex);

                SetColor(block, DataManager.GetColors()[i], PaintMode.onlyPawn);
                SetColor(block, neighborSelected ? DataManager.GetHoverColors()[i] : DataManager.normalColor, PaintMode.onlyBlock);
                indexesAlreadyModified.Add(block.MyOwnIndex);

                if (simulateNeighbors)
                    foreach (var neighborIndex in block.GetExtendedNeighbors())
                        if (Blocks[neighborIndex].IsEmpty() && !indexesAlreadyModified.Contains(neighborIndex))
                        {
                            if (neighborSelected) indexesAlreadyModified.Add(neighborIndex);
                            Blocks[neighborIndex].SetColor(neighborSelected ? DataManager.GetColors()[i] : DataManager.normalColor, true);
                            Blocks[neighborIndex].SetColorState(!neighborSelected);
                        }
            }
    }

    
    private void SetColor(Block block, Material m, PaintMode paintMode)
    {
        if (paintMode != PaintMode.onlyPawn)
            block.SetColor(m);

        if (paintMode != PaintMode.onlyBlock)
            block.SetContentColor(m);
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
                    DataManager.GetPositions().Add(Blocks.Count);
                    block.SetOwnerId(GameManager.Instance.CurrentPlayerId);
                    GameManager.Instance.CurrentPlayerId = (GameManager.Instance.CurrentPlayerId + 1) % nb_players;
                }

                block.MyOwnIndex = Blocks.Count;
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
                    DataManager.GetPositions().Add(Blocks.Count);
                    block.SetOwnerId(GameManager.Instance.CurrentPlayerId);

                    if (GameManager.Instance.CurrentPlayerId == nb_players - 1 && player_Id_increment_to_set_alt_mode_start_positions == 1)
                        player_Id_increment_to_set_alt_mode_start_positions = -1;
                    else
                        GameManager.Instance.CurrentPlayerId = (GameManager.Instance.CurrentPlayerId + player_Id_increment_to_set_alt_mode_start_positions) % nb_players;
                }

                block.MyOwnIndex = Blocks.Count;
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

        GameManager.Instance.CurrentPlayerId = 0;
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
                    DataManager.GetPositions().Add(Blocks.Count);
                    block.SetOwnerId(GameManager.Instance.CurrentPlayerId);
                    GameManager.Instance.CurrentPlayerId = (GameManager.Instance.CurrentPlayerId + 1) % nb_players;
                }

                block.MyOwnIndex = Blocks.Count;
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
}

public enum PaintMode { onlyBlock, onlyPawn, all }
