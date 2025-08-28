using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PowerManager
{
    private Dictionary<int, Dictionary<string, int>> Datas;
    public Action<Block, bool> Conquer;

    public static PowerManager Instance;

    public PowerManager(Action<Block, bool> conquer = null)
    {
        Instance = this;
        Datas = new();
        Conquer = conquer;
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
                block.PowerDisplay = "Déplacement étendu";
                block.SetMoveRange(2);
                break;
            case "Gel":
                nbTours = 2;
                block.PowerDisplay = "Gel";
                block.Powers.Add("Gel");

                foreach (var blockId in block.NeighborsIndexes)
                {
                    var neighborBlock = GameManager.terrainManager.Blocks[blockId];
                    if (neighborBlock.OwnerId == GameManager.Instance.CurrentPlayerId)
                        neighborBlock.Powers.Add("Gel");
                }
                break;
            case "Capture":
                block.SetConquerRange(2);
                Conquer?.Invoke(block, false);
                block.SetConquerRange(1);
                break;
            case "Contagion":
                UIManager.Instance.AskMessageToPlayer("Sélectionnez un bloc ennemi adjacent à ce bloc pour le contaminer.");
                var targetBlock = await GameManager.Instance.WaitForBlockSelectionAsync(new HashSet<Block>(block.NeighborsIndexes.Where(BlockId =>
                !new int[2] { -1, GameManager.Instance.CurrentPlayerId }.Contains(GameManager.terrainManager.Blocks[BlockId].OwnerId)).Select(id => GameManager.terrainManager.Blocks[id])));

                // Contagion
                DisableAllPowers(targetBlock);
                Conquer?.Invoke(targetBlock, false);

                UIManager.Instance.AskMessageToPlayer("Sélectionnez un povoir pour le bloc contaminé.");

                block.SetConquerRange(2);
                Conquer?.Invoke(block, false);
                block.SetConquerRange(1);
                break;
        }

        if (!Datas.ContainsKey(block.myOwnIndex))
            Datas[block.myOwnIndex] = new();
        Datas[block.myOwnIndex][powerName] = nbTours * GameManager.terrainManager.nb_players;

    }

    public void DisableAllPowers(Block block)
    {
        block.SetLevel(0);
        block.Powers = new();
        block.PowerDisplay = string.Empty;
        
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
                block.SetActive(true);
                break;
            case "Déplacement":
                block.SetMoveRange(1);
                break;
            case "Gel":
                block.Powers.Remove("Gel");
                foreach (var blockId in block.NeighborsIndexes)
                {
                    var neighborBlock = GameManager.terrainManager.Blocks[blockId];
                    if (neighborBlock.OwnerId == GameManager.Instance.CurrentPlayerId)
                        neighborBlock.Powers.Remove("Gel");
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

            var block = GameManager.terrainManager.Blocks[couple.id];
            DisablePower(block, couple.nom);
            block.SetLevel(0);
        }
    }
}
