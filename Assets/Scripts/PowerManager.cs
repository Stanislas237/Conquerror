using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PowerManager
{
    private List<Block> Blocks => GameManager.terrainManager.Blocks;

    private Dictionary<int, Dictionary<string, int>> Datas;
    public Action<Block, bool> Conquer;
    public Action<Block> Free;

    public static PowerManager Instance;

    private Block TeleportedAt = null;
    private int TotalTurnsForCombo = 0;

    public PowerManager(Action<Block, bool> conquer = null, Action<Block> free = null)
    {
        Instance = this;
        Datas = new();
        Conquer = conquer;
        Free = free;
    }

    public async Task EnablePower(Block block, string powerName)
    {
        int nbTours = 0;
        switch (powerName)
        {
            case "Résistance":
                nbTours = 2;
                block.PowerDisplay = "Résistance";
                block.Powers.Add("Résistance");
                break;
            case "Déplacement":
                nbTours = 1;
                block.PowerDisplay = "DéplacementEtendu";
                block.SetMoveRange(2);
                break;
            case "Gel":
                nbTours = 2;
                block.PowerDisplay = "Gel";
                block.Powers.Add("Gel");

                foreach (var blockId in block.NeighborsIndexes)
                {
                    var neighborBlock = Blocks[blockId];
                    if (neighborBlock.OwnerId == GameManager.Instance.CurrentPlayerId)
                        neighborBlock.Powers.Add("Gel");
                }
                break;
            case "Capture":
                block.SetMoveRange(2);
                var ExtendedNeighbors = block.GetExtendedNeighbors();
                block.SetMoveRange(1);

                foreach (var blockId in ExtendedNeighbors)
                {
                    var neighborBlock = Blocks[blockId];
                    if (neighborBlock.IsOpponent() && neighborBlock.IsCurrentlyPlayable())
                    {
                        neighborBlock.SetConquerRange(0);
                        Conquer?.Invoke(neighborBlock, false);
                        neighborBlock.SetConquerRange(1);
                    }
                }
                break;
            case "Contagion":
                DataManager.GetNbContagions()[GameManager.Instance.CurrentPlayerId]++;
                GameManager.Instance.PauseGameState();

                UIManager.Instance.AskMessageToPlayer("Sélectionnez un bloc ennemi adjacent à ce bloc pour le contaminer.");
                var targetBlock = await GameManager.Instance.WaitForBlockSelectionAsync(new HashSet<Block>(block.NeighborsIndexes.Where(BlockId => Blocks[BlockId].IsOpponent() && Blocks[BlockId].IsCurrentlyPlayable())
                .Select(id => Blocks[id])));

                // Contagion
                DisableAllPowers(targetBlock);
                targetBlock.SetConquerRange(0);
                Conquer?.Invoke(targetBlock, false);
                targetBlock.SetConquerRange(1);

                UIManager.Instance.AskMessageToPlayer("Sélectionnez un pouvoir pour le bloc contaminé.");
                var level1PowerName = await UIManager.Instance.WaitForPowerSelectionAsync(1);

                UIManager.Instance.ClearMessage();

                targetBlock.SetLevel(1);
                await EnablePower(targetBlock, level1PowerName);

                GameManager.Instance.ResetGameState();
                break;
            case "Téléportation":
                GameManager.Instance.PauseGameState();

                UIManager.Instance.AskMessageToPlayer("Sélectionnez un bloc vide où se téléporter.");
                targetBlock = await GameManager.Instance.WaitForBlockSelectionAsync(new HashSet<Block>(Blocks.Where(block => block.IsEmpty() && block.IsCurrentlyPlayable())));
                TeleportedAt = targetBlock;
                UIManager.Instance.ClearMessage();

                // Téléportation
                DisableAllPowers(targetBlock);
                targetBlock.SetConquerRange(1);
                Conquer?.Invoke(targetBlock, false);
                Free?.Invoke(block);

                GameManager.Instance.ResetGameState();
                break;
            case "Bouclier":
                nbTours = TotalTurnsForCombo = 3;
                block.PowerDisplay = "BouclierDeZone";
                
                block.SetMoveRange(3);
                ExtendedNeighbors = block.GetExtendedNeighbors();
                block.SetMoveRange(1);

                foreach (var blockId in ExtendedNeighbors)
                {
                    Blocks[blockId].Powers.Add("Bouclier");
                    Blocks[blockId].Powers.Add("Bouclier" + block.OwnerId);
                }
                await Task.Yield();
                break;
            case "Explosion":
                foreach (var blockId in block.NeighborsIndexes)
                {
                    var neighborBlock = Blocks[blockId];
                    if (neighborBlock.IsOpponent() && neighborBlock.IsCurrentlyPlayable())
                        Free?.Invoke(neighborBlock);
                }
                await Task.Yield();
                break;
            case "Combo":
                DataManager.GetNbCombos()[GameManager.Instance.CurrentPlayerId]++;
                DataManager.GetPassTurns()[GameManager.Instance.CurrentPlayerId]++;
                GameManager.Instance.PauseGameState();
                TeleportedAt = null;
                TotalTurnsForCombo = 0;
                
                UIManager.Instance.AskMessageToPlayer("Sélectionnez un 1er pouvoir pour le bloc transformé.");
                var level3PowerName1 = await UIManager.Instance.WaitForPowerSelectionAsync(3);
                UIManager.Instance.ClearMessage();

                await EnablePower(block, level3PowerName1);
                
                UIManager.Instance.AskMessageToPlayer("Sélectionnez un 2e pouvoir pour le bloc transformé.");
                var level3PowerName2 = await UIManager.Instance.WaitForPowerSelectionAsync(3);
                UIManager.Instance.ClearMessage();

                if (TeleportedAt != null)
                    block = TeleportedAt;
                
                await EnablePower(block, level3PowerName2);

                if (TeleportedAt != null)
                    block = TeleportedAt;

                if (level3PowerName1 == "Bouclier" && level3PowerName2 == "Téléportation")
                    await EnablePower(block, level3PowerName1);

                block.SetLevel(4);
                block.PowerDisplay = "Combo";
                nbTours = TotalTurnsForCombo;

                GameManager.Instance.ResetGameState();                
                break;
            case "Domination":
                block.SetMoveRange(3);
                ExtendedNeighbors = block.GetExtendedNeighbors();
                block.SetMoveRange(1);

                foreach (var blockId in ExtendedNeighbors)
                {
                    var neighborBlock = Blocks[blockId];
                    if (neighborBlock.IsOpponent() && neighborBlock.IsCurrentlyPlayable())
                    {
                        neighborBlock.SetConquerRange(0);
                        Conquer?.Invoke(neighborBlock, false);
                        neighborBlock.SetConquerRange(1);
                    }
                }
                break;
        }
        UIManager.Instance.ShowPowers(false);

        if (!Datas.ContainsKey(block.myOwnIndex))
            Datas[block.myOwnIndex] = new();
        Datas[block.myOwnIndex][powerName] = nbTours * GameManager.terrainManager.nb_players;

    }

    public void DisableAllPowers(Block block)
    {
        block.SetLevel(0);
        block.ClearPowers();
        
        if (!Datas.ContainsKey(block.myOwnIndex))
            return;

        foreach (var powerName in new List<string>(Datas[block.myOwnIndex].Keys))
            DisablePower(block, powerName);
        Datas.Remove(block.myOwnIndex);
    }

    private void DisablePower(Block block, string powerName)
    {
        block.PowerDisplay = string.Empty;
        switch (powerName)
        {
            case "Résistance":
                block.Powers.Remove("Résistance");
                break;
            case "Déplacement":
                block.SetMoveRange(1);
                break;
            case "Gel":
                block.Powers.Remove("Gel");
                foreach (var blockId in block.NeighborsIndexes)
                {
                    var neighborBlock = Blocks[blockId];
                    if (neighborBlock.OwnerId == block.OwnerId)
                        neighborBlock.Powers.Remove("Gel");
                }
                break;
            case "Bouclier":
                block.SetMoveRange(3);
                var ExtendedNeighbors = block.GetExtendedNeighbors();
                block.SetMoveRange(1);

                foreach (var blockId in ExtendedNeighbors)
                {
                    Blocks[blockId].Powers.Remove("Bouclier");
                    Blocks[blockId].Powers.Remove("Bouclier" + block.OwnerId);
                }
                break;
        }
    }

    public void DecrementAllNbTurns()
    {
        var inactiveBlocksPowers = new List<(int id, string nom)>();

        foreach (var BlockId in new List<int>(Datas.Keys))
            foreach (var powerName in new List<string>(Datas[BlockId].Keys))
            {
                Datas[BlockId][powerName]--;

                if (Datas[BlockId][powerName] < 0)
                    inactiveBlocksPowers.Add((BlockId, powerName));
            }

        // Désactiver et supprimer les pouvoirs qui ont expiré
        foreach (var couple in inactiveBlocksPowers)
        {
            Datas[couple.id].Remove(couple.nom);

            if (Datas[couple.id].Count == 0)
                Datas.Remove(couple.id);

            var block = Blocks[couple.id];
            DisablePower(block, couple.nom);
            block.SetLevel(0);
        }
    }
}
