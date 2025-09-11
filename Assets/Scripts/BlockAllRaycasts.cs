using UnityEngine;
using UnityEngine.EventSystems;

public class BlockAllRaycasts : MonoBehaviour, ICanvasRaycastFilter
{
    public bool IsRaycastLocationValid(Vector2 _, Camera __) => true;
}
