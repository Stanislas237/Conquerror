using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    public List<int> NeighborsIndexes = new();

    public int myOwnIndex = -1;

    public int level = 0;

    public int ownerId = -1;

    public GameObject Content;

    public bool canColor = true;

    private MeshRenderer mr;

    public void SetColor(Material m, bool important = false)
    {
        mr ??= GetComponent<MeshRenderer>();
        if (canColor || important) mr.material = m;
    }

    private void OnMouseEnter() => GameManager.Instance.OnBlockEnter(this);
    private void OnMouseExit() => GameManager.Instance.OnBlockExit(this);
    private void OnMouseDown() => GameManager.Instance.OnBlockDown(this);
}
