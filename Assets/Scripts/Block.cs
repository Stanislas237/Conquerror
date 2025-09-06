using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    private List<Block> Blocks => GameManager.terrainManager.Blocks;

    // Données propres au bloc
    public HashSet<int> NeighborsIndexes = new();

    private int _index = -1;

    public int MyOwnIndex
    {
        get => _index;
        set
        {
            if (_index != -1)
                throw new InvalidOperationException("My own index ne peut être modifié qu'une seule fois !");
            _index = value;
        }
    }

    private MeshRenderer mr;

    [HideInInspector]
    public GameObject Content;

    public List<string> Powers = new();

    [HideInInspector]
    public string PowerDisplay = string.Empty;


    // Caractéristiques principales
    public bool Active
    {
        get;
        private set;
    } = true;

    public int Level
    {
        get;
        private set;
    } = 0;

    public int OwnerId
    {
        get;
        private set;
    } = -1;

    public int MoveRange
    {
        get;
        private set;
    } = 1;

    public int ConquerRange
    {
        get;
        private set;
    } = 1;

    public bool CanColor
    {
        get;
        private set;
    } = true;




    // Setters
    public void SetLevel(int newLevel)
    {
        Level = newLevel;
        Content ??= transform.GetChild(0).gameObject;
        UIManager.Instance.ShowBlockLevel(this);
    }

    public void SetOwnerId(int newOwner) => OwnerId = newOwner;

    public void SetActive(bool active) => Active = active;

    public void SetMoveRange(int newRange) => MoveRange = newRange;

    public void SetConquerRange(int newRange) => ConquerRange = newRange;

    public void SetColorState(bool canColor) => CanColor = canColor;

    public void SetColor(Material m, bool important = false)
    {
        mr ??= GetComponent<MeshRenderer>();
        if (CanColor || important) mr.material = m;
    }

    public void SetContentColor(Material m)
    {
        Content ??= transform.GetChild(0).gameObject;
        Content.SetActive(m != null);
        Content.GetComponent<MeshRenderer>().material = m;
    }

    public void ClearPowers()
    {
        foreach (var p in Powers.ToList())
            if (!p.Contains("Bouclier"))
                Powers.Remove(p);
        PowerDisplay = string.Empty;
    }



    // Getters
    public HashSet<int> GetExtendedNeighbors()
    {
        if (MoveRange == 0)
            return new HashSet<int>(){ MyOwnIndex };

        var extendedNeighbors = new HashSet<int>(NeighborsIndexes);
        var currentNeighbors = new HashSet<int>(NeighborsIndexes);
        var moveRange = MoveRange;

        while (moveRange > 1)
        {
            var newNeighbors = new HashSet<int>();

            foreach (var index in currentNeighbors)
                newNeighbors.UnionWith(Blocks[index].NeighborsIndexes);

            extendedNeighbors.UnionWith(newNeighbors);

            currentNeighbors = new HashSet<int>(newNeighbors);

            moveRange--;
        }

        return extendedNeighbors;
    }

    public HashSet<Block> GetExtendedNeighbors(int moveRange)
    {
        int previousValue = MoveRange;
        MoveRange = moveRange;
        var extendedNeighbors = GetExtendedNeighbors();
        MoveRange = previousValue;
        return new HashSet<Block>(extendedNeighbors.Select(id => Blocks[id]));
    }



    // States
    public bool IsEmpty() => OwnerId == -1;

    public bool IsCircled() => !NeighborsIndexes.Any(nId => GameManager.terrainManager.Blocks[nId].IsEmpty());

    public bool HasNeighbor(int blockId, int moveRange) => NeighborsIndexes.Contains(blockId) ||
        (moveRange > 1 && NeighborsIndexes.Any(i => Blocks[i].HasNeighbor(blockId, moveRange - 1)));

    public bool IsCurrentlyPlayable() => !Powers.Contains("Bouclier") || Powers.Contains("Bouclier" + GameManager.Instance.CurrentPlayerId);

    public bool IsOpponent() => OwnerId != -1 && OwnerId != GameManager.Instance.CurrentPlayerId;

    private void OnMouseEnter()
    {
        if (Active)
            GameManager.Instance.OnBlockEnter(this);
    }

    private void OnMouseExit()
    {
        if (Active)
            GameManager.Instance.OnBlockExit(this);
    }

    private void OnMouseDown()
    {
        if (Active)
            GameManager.Instance.OnBlockDown(this);
    }

    // Overrides

    public override bool Equals(object obj) => obj is Block block && MyOwnIndex == block.MyOwnIndex;

    public override int GetHashCode() => MyOwnIndex.GetHashCode();
}
