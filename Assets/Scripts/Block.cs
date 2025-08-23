using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    public List<int> NeighborsIndexes = new();

    public int myOwnIndex = -1;

    public int Level
    {
        get;
        private set;
    } = 0;

    public int ownerId = -1;

    public GameObject Content;

    public bool canColor = true;

    public bool canConquer = true;

    private MeshRenderer mr;

    public void SetColor(Material m, bool important = false)
    {
        mr ??= GetComponent<MeshRenderer>();
        if (canColor || important) mr.material = m;
    }

    public void SetContentColor(Material m)
    {
        Content ??= transform.GetChild(0).gameObject;
        Content.SetActive(m != null);
        Content.GetComponent<MeshRenderer>().material = m;
    }

    public void SetLevel(int newLevel)
    {
        Level = newLevel;
        Content ??= transform.GetChild(0).gameObject;
        UIManager.Instance.ShowBlockLevel(this);
    }

    public void ConquerBy(int player, List<Block> playerList, Material m)
    {
        ownerId = player;
        playerList.Add(this);
    }

    private void OnMouseEnter() => GameManager.Instance.OnBlockEnter(this);
    private void OnMouseExit() => GameManager.Instance.OnBlockExit(this);
    private void OnMouseDown() => GameManager.Instance.OnBlockDown(this);
}
