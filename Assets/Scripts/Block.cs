using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    public List<int> NeighborsIndexes = new();

    public int myOwnIndex = -1;

    public int ownerId = -1;

    public GameObject Content;

    private MeshRenderer mr;

    public void SetColor(Material m)
    {
        mr ??= GetComponent<MeshRenderer>();
        mr.material = m;
    }

    private void OnMouseEnter()
    {
        if (ownerId == -1 && GameManager.Instance.GetPositions().Any(p => NeighborsIndexes.Contains(p.myOwnIndex)))
            SetColor(GameManager.Instance.hoverColor);
    }

    private void OnMouseExit()
    {
        if (ownerId == -1 && GameManager.Instance.GetPositions().Any(p => NeighborsIndexes.Contains(p.myOwnIndex)))
            SetColor(GameManager.Instance.normalColor);
    }

    private void OnMouseDown()
    {
        if (ownerId == -1 && GameManager.Instance.GetPositions().Any(p => NeighborsIndexes.Contains(p.myOwnIndex)))
            GameManager.Instance.Conquer(this);
    }
}
