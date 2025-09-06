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
        block.SetLevel(Level);
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

            case "Artefact":
                foreach (var neighborBlock in block.GetExtendedNeighbors(Level + 1))
                    if (neighborBlock.IsOpponent() && neighborBlock.IsCurrentlyPlayable())
                        Free?.Invoke(neighborBlock);

                await Task.Yield();
                break;

            case "Domination":
                foreach (var neighborBlock in block.GetExtendedNeighbors(Level))
                    if (neighborBlock.IsOpponent() && neighborBlock.IsCurrentlyPlayable())
                    {
                        neighborBlock.SetConquerRange(0);
                        Conquer?.Invoke(neighborBlock, false);
                        neighborBlock.SetConquerRange(1);
                    }

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
                TotalTurnsForCombo += nbTours = 3;
                block.PowerDisplay = "BouclierDeZone";

                foreach (var neighborBlock in block.GetExtendedNeighbors(3))
                {
                    neighborBlock.Powers.Add("Bouclier");
                    neighborBlock.Powers.Add("Bouclier" + block.OwnerId);
                }

                await Task.Yield();
                break;

            case "Combo":
                TeleportedAt = null;
                TotalTurnsForCombo = 0;

                LevelTarget = Math.Max(1, Level - 1);
                UIManager.Instance.AskMessageToPlayer($"Sélectionnez un pouvoir de niveau {LevelTarget} pour ce bloc.");

                var levelDownPowerName1 = await UIManager.Instance.WaitForPowerSelectionAsync(LevelTarget);
                await EnablePower(block, levelDownPowerName1, LevelTarget);

                UIManager.Instance.AskMessageToPlayer("Sélectionnez encore un pouvoir pour le bloc transformé.");
                var levelDownPowerName2 = await UIManager.Instance.WaitForPowerSelectionAsync(LevelTarget);

                if (TeleportedAt != null)
                    block = TeleportedAt;
                await EnablePower(block, levelDownPowerName2, LevelTarget);

                if (TeleportedAt != null)
                    block = TeleportedAt;

                if (levelDownPowerName1 == "Bouclier" && levelDownPowerName2 == "Téléportation")
                    await EnablePower(block, levelDownPowerName1, LevelTarget);

                block.PowerDisplay = "Combinaison";
                nbTours = TotalTurnsForCombo;

                await Task.Yield();
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
        block.ClearPowers();
        
        if (!Datas.ContainsKey(block.MyOwnIndex))
            return;

        foreach (var powerName in new HashSet<string>(Datas[block.MyOwnIndex].Keys))
            DisablePower(block, powerName);
        Datas.Remove(block.MyOwnIndex);
    }

    private void DisablePower(Block block, string powerName)
    {
        block.PowerDisplay = string.Empty;
        switch (powerName)
        {
            case "Defense":
                foreach (var neighborBlock in block.GetExtendedNeighbors(block.Level - 1))
                    if (neighborBlock.OwnerId == block.OwnerId)
                        neighborBlock.Powers.Remove("Gel");
                break;

            case "Bouclier":
                foreach (var neighborBlock in block.GetExtendedNeighbors(3))
                {
                    neighborBlock.Powers.Remove("Bouclier");
                    neighborBlock.Powers.Remove("Bouclier" + block.OwnerId);
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
