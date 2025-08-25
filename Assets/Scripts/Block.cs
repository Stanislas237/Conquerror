using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    // Caractéristiques principales
    public HashSet<int> NeighborsIndexes = new();
    public int myOwnIndex = -1;
    private MeshRenderer mr;
    public GameObject Content;


    // Etat du bloc, pouvoirs...
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

    public bool Active
    {
        get;
        private set;
    } = true;
    
    public int MoveRange
    {
        get;
        private set;
    } = 1;
    
    public bool CanColor
    {
        get;
        private set;
    } = true;

    public void SetLevel(int newLevel)
    {
        Level = newLevel;
        Content ??= transform.GetChild(0).gameObject;
        UIManager.Instance.ShowBlockLevel(this);
    }

    public void SetOwnerId(int newOwner) => OwnerId = newOwner;

    public void SetActive(bool active) => Active = active;

    public void SetMoveRange(int newRange) => MoveRange = newRange;

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

    public bool HasNeighbor(List<Block> Blocks, int blockId, int moveRange) => NeighborsIndexes.Contains(blockId) ||
        (moveRange > 1 && NeighborsIndexes.Any(i => Blocks[i].HasNeighbor(Blocks, blockId, moveRange - 1)));

    public HashSet<int> GetExtendedNeighbors(List<Block> Blocks)
    {
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

    private void OnMouseEnter() => GameManager.Instance.OnBlockEnter(this);
    private void OnMouseExit() => GameManager.Instance.OnBlockExit(this);
    private void OnMouseDown() => GameManager.Instance.OnBlockDown(this);
}
