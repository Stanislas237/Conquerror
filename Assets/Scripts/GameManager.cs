using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static TerrainManager terrainManager;

    private List<Block> Blocks => terrainManager.Blocks;

    public readonly List<int> SelectedBlocks = new();

    public int SelectionLevel => SelectedBlocks.Sum(b => Blocks[b].Level + 1);

    public int CurrentPlayerId = 0;


    // Sélection spéciale
    private HashSet<Block> SpecialSelectionList = null;

    private TaskCompletionSource<Block> _blockSelectionSource;

    public Task<Block> WaitForBlockSelectionAsync(HashSet<Block> validTargets)
    {
        _blockSelectionSource = new();
        SpecialSelectionList = validTargets;
        return _blockSelectionSource.Task;
    }



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
        new PowerManager(Conquer);
    }

    private void Start()
    {
        terrainManager.GenerateBlocks();
        // UIManager.Instance.Fusion = Fusion;
    }
    
    public void OnBlockEnter(Block block)
    {
        if (SpecialSelectionList != null)
        {
            if (SpecialSelectionList.Contains(block))
                block.SetColor(DataManager.specialSelectionColor);
            return;
        }

        if (!SelectedBlocks.Contains(block.myOwnIndex))
            if (block.OwnerId == CurrentPlayerId)
                block.SetColor(DataManager.GetHoverColors()[CurrentPlayerId]);
            else if (block.OwnerId == -1 && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(Blocks, block.myOwnIndex, Blocks[i].MoveRange)))
                block.SetColor(DataManager.GetColors()[CurrentPlayerId]);
    }

    public void OnBlockDown(Block block)
    {
        if (SpecialSelectionList != null)
        {
            if (SpecialSelectionList.Contains(block))
            {
                _blockSelectionSource.SetResult(block);
                SpecialSelectionList = null;
            }
            return;
        }

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

    private void NextPlayerTurn()
    {
        CurrentPlayerId = (CurrentPlayerId + 1) % terrainManager.nb_players;
        PowerManager.Instance.DecrementAllNbTurns();

        var nbTurnToPass = DataManager.GetPassTurns();
        if (nbTurnToPass[CurrentPlayerId] > 0 || !DataManager.GetPositions().Any(i => !Blocks[i].IsCircled()))
        {
            nbTurnToPass[CurrentPlayerId] = Mathf.Max(nbTurnToPass[CurrentPlayerId] - 1, 0);
            NextPlayerTurn();
        }
        else
            UIManager.Instance.ShowPlayerUI();
    }

    private void Free(Block block)
    {
        DataManager.GetPositions(block.OwnerId).Remove(block.myOwnIndex);
        block.SetOwnerId(-1);
        PowerManager.Instance.DisableAllPowers(block);
        block.Content.SetActive(false);
    }

    private void Conquer(Block block, bool GoToNextPlayerAfterConquer = true)
    {
        UnSelect();
        Conquer(block, block.ConquerRange);
        terrainManager.Draw();

        if (GoToNextPlayerAfterConquer)
            NextPlayerTurn();
    }

    private void Conquer(Block block, int recursive)
    {
        if (!block.Active || block.Powers.Contains("Gel") || block.Powers.Contains("Bouclier") || block.Powers.Contains("Résistance"))
            return;

        else if (block.Level > 0)
            PowerManager.Instance.DisableAllPowers(block);
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
            if (SelectedBlocks.Count != 0 && !Blocks[SelectedBlocks.Last()].NeighborsIndexes.Contains(block.myOwnIndex))
                SelectedBlocks.Clear();
            SelectedBlocks.Add(block.myOwnIndex);
        }
        terrainManager.Draw();
    }

    private async Task Fusion(string powerName)
    {
        var LevelTartget = SelectionLevel - 1;
        var tempList = SelectedBlocks.ToList();
        UnSelect();

        for (int i = 0; i < tempList.Count - 1; i++)
            Free(Blocks[tempList[i]]);
        var block = Blocks[tempList.Last()];
        block.SetLevel(LevelTartget);

        await PowerManager.Instance.EnablePower(block, powerName);
        NextPlayerTurn();
    }
}
