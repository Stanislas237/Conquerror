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
    private enum GameState { Normal, Waiting, }

    private GameState gameState = GameState.Normal;

    private HashSet<Block> SpecialSelectionList = null;

    private TaskCompletionSource<Block> _blockSelectionSource;

    public Task<Block> WaitForBlockSelectionAsync(HashSet<Block> validTargets)
    {
        _blockSelectionSource = new();
        SpecialSelectionList = validTargets;
        return _blockSelectionSource.Task;
    }

    public void ResetGameState() => gameState = GameState.Normal;

    public void PauseGameState() => gameState = GameState.Waiting;


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
        new PowerManager(Conquer, Free);
    }

    private void Start() => terrainManager.GenerateBlocks();
    
    public void OnBlockEnter(Block block)
    {
        // Sélection spéciale
        if (SpecialSelectionList != null)
        {
            if (SpecialSelectionList.Contains(block))
                block.SetColor(DataManager.specialSelectionColor);
            return;
        }

        // En attente d'Input
        if (gameState == GameState.Waiting)
            return;

        // Si un bouclier est actif
        if (!block.IsCurrentlyPlayable())
            return;

        // Hover normal
        if (!SelectedBlocks.Contains(block.myOwnIndex))
            if (block.OwnerId == CurrentPlayerId)
                block.SetColor(DataManager.GetHoverColors()[CurrentPlayerId]);
            else if (block.IsEmpty() && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(block.myOwnIndex, Blocks[i].MoveRange)))
                block.SetColor(DataManager.GetColors()[CurrentPlayerId]);
    }

    public void OnBlockDown(Block block)
    {
        // Sélection spéciale
        if (SpecialSelectionList != null)
        {
            if (SpecialSelectionList.Contains(block))
            {
                _blockSelectionSource.SetResult(block);
                SpecialSelectionList = null;
            }
            return;
        }

        // En attente d'Input
        if (gameState == GameState.Waiting)
            return;

        // Si un bouclier est actif
        if (!block.IsCurrentlyPlayable())
            return;

        // Clic normal
        if (block.OwnerId == CurrentPlayerId)
            Select(block);
        else if (block.IsEmpty() && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(block.myOwnIndex, Blocks[i].MoveRange)))
            Conquer(block);
    }

    public void OnBlockExit(Block block)
    {
        // Sélection spéciale
        if (SpecialSelectionList != null)
            block.SetColor(DataManager.normalColor);

        // En attente d'Input
        if (gameState == GameState.Waiting)
            return;

        // Si un bouclier est actif
        if (!block.IsCurrentlyPlayable())
            return;

        // Exit normal
        if (!SelectedBlocks.Contains(block.myOwnIndex) && ((block.OwnerId == CurrentPlayerId) || (block.IsEmpty() && DataManager.GetPositions().Any(i => Blocks[i].HasNeighbor(block.myOwnIndex, Blocks[i].MoveRange)))))
            block.SetColor(DataManager.normalColor);
    }

    public void NextPlayerTurn()
    {
        if (gameState == GameState.Waiting)
            return;

        CurrentPlayerId = (CurrentPlayerId + 1) % terrainManager.nb_players;
        PowerManager.Instance.DecrementAllNbTurns();

        if (DataManager.GetNbPositionsOccuped() >= Blocks.Count)
        {
            GameManager.Instance.EndGame(DataManager.GetTheLongerPostitionsList());
            return;
        }

        var nbTurnToPass = DataManager.GetPassTurns();
        if (nbTurnToPass[CurrentPlayerId] > 0 || !DataManager.GetPositions().Any(i => !Blocks[i].IsCircled()))
        {
            nbTurnToPass[CurrentPlayerId] = Mathf.Max(nbTurnToPass[CurrentPlayerId] - 1, 0);
            NextPlayerTurn();
        }
        else
            UIManager.Instance.ShowPlayerUI();
    }

    public void Free(Block block)
    {
        DataManager.GetPositions(block.OwnerId).Remove(block.myOwnIndex);
        PowerManager.Instance.DisableAllPowers(block);
        block.SetOwnerId(-1);
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
        if (!block.Active || block.Powers.Contains("Gel") || !block.IsCurrentlyPlayable() || block.Powers.Contains("Résistance"))
            return;

        else if (block.Level > 0)
            PowerManager.Instance.DisableAllPowers(block);
        else
        {
            if (block.OwnerId >= 0)
                DataManager.GetPositions(block.OwnerId).Remove(block.myOwnIndex);

            PowerManager.Instance.DisableAllPowers(block);
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

    public async Task Fusion(string powerName)
    {
        DataManager.GetConquerPoints()[CurrentPlayerId] -= UIManager.Instance.GetRequiredPoints(SelectionLevel);
        var LevelTartget = SelectionLevel - 1;
        var tempList = SelectedBlocks.ToList();
        UnSelect();

        for (int i = 0; i < tempList.Count - 1; i++)
            Free(Blocks[tempList[i]]);
        var block = Blocks[tempList.Last()];
        PowerManager.Instance.DisableAllPowers(block);
        block.SetLevel(LevelTartget);
        await PowerManager.Instance.EnablePower(block, powerName);
        DataManager.GetConquerPoints()[CurrentPlayerId] += 2;
        NextPlayerTurn();
    }

    public void EndGame(int winnerId)
    {
        PauseGameState();
        CurrentPlayerId = winnerId;
        UIManager.Instance.AskMessageToPlayer($"Player {winnerId + 1} a gagné !");
    }
}
