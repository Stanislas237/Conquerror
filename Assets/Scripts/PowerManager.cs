using System;
using System.Collections.Generic;

public class PowerManager
{
    private Dictionary<int, Dictionary<string, int>> Datas;

    public static PowerManager Instance;

    public PowerManager()
    {
        Instance = this;
        Datas = new();
    }

    public void EnablePower(Block block, string powerName)
    {
        int nbTours = 0;
        switch (powerName)
        {
            case "Résistance":
                nbTours = 2;
                block.SetActive(false);
                break;
            case "Déplacement":
                nbTours = 1;
                block.SetMoveRange(2);
                break;
        }

        if (!Datas.ContainsKey(block.myOwnIndex))
            Datas[block.myOwnIndex] = new();
        Datas[block.myOwnIndex][powerName] = nbTours;
    }

    private void DisablePower(Block block, string powerName)
    {
        switch (powerName)
        {
            case "Résistance":
                block.SetActive(true);
                break;
            case "Déplacement":
                block.SetMoveRange(1);
                break;
        }

        block.SetLevel(0);
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

            DisablePower(GameManager.terrainManager.Blocks[couple.id], couple.nom);
        }
    }
}
