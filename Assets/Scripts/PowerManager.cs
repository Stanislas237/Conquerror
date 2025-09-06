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

    public async Task EnablePower(Block block, string powerName, int Level)
    {
        int nbTours = 0;
        switch (powerName)
        {
            case "Defense":
                nbTours = Level switch
                {
                    1 or 2 => 2,
                    _ => Level
                };

                block.PowerDisplay = "Defense";
                block.Powers.Add("Gel");

                if (Level == 1)
                    break;

                foreach (var neighborBlock in block.GetExtendedNeighbors(Level - 1))
                    if (neighborBlock.OwnerId == GameManager.Instance.CurrentPlayerId)
                        neighborBlock.Powers.Add("Gel");

                await Task.Yield();
                break;

            case "Mouvement":
                UIManager.Instance.AskMessageToPlayer($"Déplacement étendu de rayon {Level + 1} activé.");

                block.PowerDisplay = "Mouvement";
                var targetBlock = await GameManager.Instance.WaitForBlockSelectionAsync(block.GetExtendedNeighbors(Level + 1));
                Conquer?.Invoke(targetBlock, true);

                await Task.Yield();
                break;

            case "Attaque":
                foreach (var neighborBlock in block.GetExtendedNeighbors(Level))
                    if (neighborBlock.IsOpponent() && neighborBlock.IsCurrentlyPlayable())
                        // neighborBlock.SetConquerRange(0);
                        Conquer?.Invoke(neighborBlock, false);
                        // neighborBlock.SetConquerRange(1);

                await Task.Yield();
                break;

            case "Contagion":
                UIManager.Instance.AskMessageToPlayer($"Sélectionnez un bloc ennemi dans un rayon de {Level} pour le contaminer.");

                targetBlock = await GameManager.Instance.WaitForBlockSelectionAsync(block.GetExtendedNeighbors(Level).Where(b => b.IsOpponent() && b.IsCurrentlyPlayable()));

                // Contagion
                DisableAllPowers(targetBlock);
                targetBlock.SetLevel(0);
                targetBlock.SetConquerRange(0);
                Conquer?.Invoke(targetBlock, false);
                targetBlock.SetConquerRange(1);

                var LevelTarget = Math.Max(1, Level - 1);
                UIManager.Instance.AskMessageToPlayer($"Sélectionnez un combo de niveau {LevelTarget} pour le bloc contaminé.");
                var levelDownPowerName = await UIManager.Instance.WaitForPowerSelectionAsync(LevelTarget);

                // Pouvoir
                targetBlock.SetLevel(LevelTarget);
                await EnablePower(targetBlock, levelDownPowerName, LevelTarget);
                break;

            case "Téléportation":
                UIManager.Instance.AskMessageToPlayer("Sélectionnez un bloc vide où se téléporter.");
                targetBlock = await GameManager.Instance.WaitForBlockSelectionAsync(Blocks.Where(block => block.IsEmpty() && block.IsCurrentlyPlayable()));
                TeleportedAt = targetBlock;

                // Téléportation
                DisableAllPowers(targetBlock);
                Conquer?.Invoke(targetBlock, false);
                Free?.Invoke(block);
                break;
                
            case "Bouclier":
                nbTours = TotalTurnsForCombo = 3;
                block.PowerDisplay = "BouclierDeZone";
                
                block.SetMoveRange(3);
                var ExtendedNeighbors = block.GetExtendedNeighbors();
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

        UIManager.Instance.ClearMessage();
        UIManager.Instance.ShowPowers(false);
        GameManager.Instance.ResetGameState();

        if (!Datas.ContainsKey(block.MyOwnIndex))
            Datas[block.MyOwnIndex] = new();
        Datas[block.MyOwnIndex][powerName] = nbTours * TerrainManager.nb_players;

    }

    public void DisableAllPowers(Block block)
    {
        // block.SetLevel(0);
        block.ClearPowers();
        
        if (!Datas.ContainsKey(block.MyOwnIndex))
            return;

        foreach (var powerName in new List<string>(Datas[block.MyOwnIndex].Keys))
            DisablePower(block, powerName);
        Datas.Remove(block.MyOwnIndex);
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
        }
    }
}
