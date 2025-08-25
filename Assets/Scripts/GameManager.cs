using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static TerrainManager terrainManager;

    // Données des joueurs

    private List<Block> Blocks => terrainManager.Blocks;

    public readonly List<int> SelectedBlocks = new();

    public int SelectionLevel => SelectedBlocks.Sum(b => Blocks[b].Level + 1);

    public int CurrentPlayerId = 0;

    // Matériaux / Couleurs

    // Données de l'éditeur

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            terrainManager = GetComponent<TerrainManager>();
        }
        else
            Destroy(gameObject);

        new DataManager();
    }

    private void Start()
    {

        terrainManager.GenerateBlocks();
        NextPlayerTurn();

        UIManager.Instance.Fusion = Fusion;
    }

    #region Block Management Handlers
    
    public void OnBlockEnter(Block block)
    {
        if (!SelectedBlocks.Contains(block.myOwnIndex))
            if (block.OwnerId == CurrentPlayerId)
                block.SetColor(DataManager.GetHoverColors()[CurrentPlayerId]);
            else if (block.OwnerId == -1 && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))
                block.SetColor(DataManager.GetColors()[CurrentPlayerId]);
    }

    public void OnBlockDown(Block block)
    {
        if (block.OwnerId == CurrentPlayerId)
            Select(block);
        else if (block.OwnerId == -1 && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))
            Conquer(block);
    }

    public void OnBlockExit(Block block)
    {
        if (!SelectedBlocks.Contains(block.myOwnIndex) && ((block.OwnerId == CurrentPlayerId) || (block.OwnerId == -1 && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))))
            block.SetColor(DataManager.normalColor);
    }

    #endregion


    #region Player Datas Getters


    #endregion


    #region Game Management And Status Methods

    private void NextPlayerTurn()
    {
        CurrentPlayerId = (CurrentPlayerId + 1) % terrainManager.nb_players;
        var nbTurnToPass = DataManager.GetPassTurns();

        if (nbTurnToPass[CurrentPlayerId] > 0 || !DataManager.GetPositions().Any(i => !Blocks[i].IsCircled()))
        {
            nbTurnToPass[CurrentPlayerId] = Mathf.Max(nbTurnToPass[CurrentPlayerId] - 1, 0);
            NextPlayerTurn();
        }
        else
            UIManager.Instance.ShowPlayerUI(DataManager.GetConquerPoints()[CurrentPlayerId], DataManager.GetColors()[CurrentPlayerId]);
    }

    #endregion


    #region Player Actions Methods

    private void Free(Block block)
    {
        DataManager.GetPositions(block.OwnerId).Remove(block.myOwnIndex);
        block.SetOwnerId(-1);
        block.SetLevel(0);
        block.Content.SetActive(false);
    }

    private void Conquer(Block block)
    {
        UnSelect();
        Conquer(block, block.Level + 1);
        terrainManager.Draw();
        NextPlayerTurn();
    }

    private void Conquer(Block block, int recursive)
    {
        if (block.Level > 0)
            block.SetLevel(0);
        else
        {
            if (block.OwnerId >= 0)
                DataManager.GetPositions(block.OwnerId).Remove(block.myOwnIndex);

            DataManager.GetPositions().Add(block.myOwnIndex);
            block.SetOwnerId(CurrentPlayerId);
        }

        if (recursive > 0)
            foreach (var blockId in block.NeighborsIndexes)
            {
                var otherBlock = Blocks[blockId];
                if (otherBlock.OwnerId >= 0 && otherBlock.OwnerId != CurrentPlayerId)
                {
                    Conquer(otherBlock, recursive - 1);
                    DataManager.GetConquerPoints()[CurrentPlayerId]++;
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
        terrainManager.Draw();
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
        terrainManager.Draw();
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

    
    #endregion
}

public enum PaintMode { onlyBlock, onlyPawn, all }
