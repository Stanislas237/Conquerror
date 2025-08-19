using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Material[] PlayerColors;
    public Material hoverColor;
    public Material normalColor;
    protected readonly List<Block>[] playersPositions = new List<Block>[4] { new(), new(), new(), new() };

    [SerializeField]
    private int nb_players = 3;

    [SerializeField]
    private Transform Plane;

    public Vector2Int terrainSize = new();

    public int terrainRay = 5;

    [HideInInspector]
    public List<Block> Blocks = new();

    private int CurrentPlayerId = 0;

    private GameObject PrefabToSpawn;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        GenerateBlocks();
        Draw();

        Plane.localScale = new(terrainSize.x, 1, terrainSize.y);
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
                GenerateHexagonNeighbors();
                break;
            case 4:
                GeneratePlusNeighbors();
                break;
        }
    }

    private void GenerateRectangle()
    {
        PrefabToSpawn = transform.GetChild(0).gameObject;
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
                    CurrentPlayerId = (CurrentPlayerId + 1) % 2;
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

    private void GenerateHexagonNeighbors()
    {
        PrefabToSpawn = transform.GetChild(1).gameObject;
        terrainSize = Vector2Int.one * (2 * terrainRay + 1);
        int R = 10 * terrainRay;
        int maxSize = 1 + 3 * terrainRay * (terrainRay + 1);

        int GetIndex(int i, int j)
        {
            int prevBlocks = 0;
            for (int k = -R; k < i; k += 10)
                prevBlocks += terrainRay + 1 + Mathf.Abs(terrainRay - Mathf.Abs(k / 10));

            return prevBlocks + Mathf.FloorToInt((j + R) / 10f) - Mathf.FloorToInt(Mathf.Abs(i) / 20f);
        }

        for (int i = -R; i <= R; i += 10)
        {
            int nbBlocks = R + 2 + Mathf.Abs(R - Mathf.Abs(i));
            for (int j = -nbBlocks / 2; j < nbBlocks / 2; j += 10)
            {
                var obj = Instantiate(PrefabToSpawn, new(i, 1, j + 1), PrefabToSpawn.transform.rotation, transform);
                obj.SetActive(true);
                obj.name = $"({i}, {j + 1})";

                var block = obj.AddComponent<Block>();
                block.myOwnIndex = GetIndex(i, j+1);
            }
        }

    }

    private void GeneratePlusNeighbors()
    {
        
    }


    // protected virtual void Start() => Draw();

    // private bool Playing = true;

    // private void Trigger(T pos, string trigger = "hover")
    // {
    //     if (playersPositions[CurrentPlayerIndex].Any(c => Neighbors(c, pos, false)))
    //         if (trigger == "hover")
    //             SetColor(pos, hoverColor);
    //         else if (trigger == "click" && Playing)
    //         {
    //             Conquer(pos);
    //             Draw();

    //             if (playersPositions.Count(list => list.Count != 0) <= 1)
    //             {
    //                 Debug.Log($"Fin de la partie ! Joueur {CurrentPlayerIndex + 1} a gagné");
    //                 Playing = false;
    //                 return;
    //             }
    //             else do
    //                 CurrentPlayerIndex = (CurrentPlayerIndex + 1) % nb_players;
    //             while (playersPositions[CurrentPlayerIndex].Count == 0);
    //         }
    // }

    // private void Draw()
    // {
    //     for (int i = 0; i < nb_players; i++)
    //         foreach (var item in playersPositions[i])
    //             SetColor(item, colors[i]);
    // }

    // protected abstract void SetColor(T pos, Color c);

    // private void UnsetColor(T pos)
    // {
    //     SetColor(pos, Color.white);
    //     Draw();
    // }

    // protected abstract bool Neighbors(T first, T second, bool onlyIgnoreMyOwnList);

    // protected abstract T SetUpButton(int index);

    // private void Conquer(T pos)
    // {
    //     for (int i = 0; i < nb_players; i++)
    //         if (i == CurrentPlayerIndex) continue;
    //         else
    //             foreach (var item in playersPositions[i].Where(c => Neighbors(c, pos, true)).ToList())
    //             {
    //                 playersPositions[i].Remove(item);
    //                 Conquer(item);
    //             }

    //     if (!playersPositions[CurrentPlayerIndex].Contains(pos))
    //         playersPositions[CurrentPlayerIndex].Add(pos);
    // }

    // protected void AddEventTriggers(GameObject obj, T pos)
    // {
    //     if (!obj.TryGetComponent(out EventTrigger trigger))
    //         trigger = obj.AddComponent<EventTrigger>();

    //     // Pointer Down
    //     EventTrigger.Entry pointerDownEntry = new() { eventID = EventTriggerType.PointerDown };
    //     pointerDownEntry.callback.AddListener(_ => Trigger(pos, "click"));
    //     trigger.triggers.Add(pointerDownEntry);

    //     // Pointer Enter
    //     EventTrigger.Entry pointerEnterEntry = new() { eventID = EventTriggerType.PointerEnter };
    //     pointerEnterEntry.callback.AddListener(_ => Trigger(pos));
    //     trigger.triggers.Add(pointerEnterEntry);

    //     // Pointer Exit
    //     EventTrigger.Entry pointerExitEntry = new() { eventID = EventTriggerType.PointerExit };
    //     pointerExitEntry.callback.AddListener(_ => UnsetColor(pos));
    //     trigger.triggers.Add(pointerExitEntry);
    // }
}
