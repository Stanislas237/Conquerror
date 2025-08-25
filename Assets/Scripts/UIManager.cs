using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private Camera mainCamera;
    private List<Block> blocksToShowLevels = new();
    public Action Fusion;

    [SerializeField]
    private Transform ConquerPointsBar;
    [SerializeField]
    private Transform LevelTextPrefab;
    [SerializeField]
    private Transform PowersParent;
    [SerializeField]
    private MeshRenderer PlayerIndicator;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = Camera.main;
    }

    private void Start()
    {
        foreach (Transform t in PowersParent)
            t.GetComponent<Button>().onClick.AddListener(() => Fusion?.Invoke());
    }

    public void ShowPlayerUI(int conquerPoints, Material m)
    {
        var currScale = ConquerPointsBar.localScale;
        currScale.x = conquerPoints / 50f;
        ConquerPointsBar.localScale = currScale;

        PlayerIndicator.material = m;
    }

    public void ShowBlockLevel(Block block)
    {
        var prefab = LevelTextPrefab.parent.Find($"LevelPrefabForId {block.myOwnIndex}") ?? Instantiate(LevelTextPrefab, LevelTextPrefab.parent);
        prefab.SetLocalPositionAndRotation(ScreenToCanvasPosition(mainCamera.WorldToScreenPoint(block.Content.transform.position + Vector3.up * 4f)), Quaternion.identity);
        prefab.name = $"LevelPrefabForId {block.myOwnIndex}";
        prefab.GetComponent<TextMeshProUGUI>().text = block.Level.ToString();
        prefab.gameObject.SetActive(block.Level > 0);

        if (!blocksToShowLevels.Contains(block))
            blocksToShowLevels.Add(block);
    }

    public void ShowPowers(int selectLevelCount, int nbBlocks, int conquerPoints)
    {
        int requiredPoints = selectLevelCount >= 5 ? 50 : (selectLevelCount - 1) * 10;
        foreach (Transform t in PowersParent)
            t.gameObject.SetActive(nbBlocks > 1 && conquerPoints >= requiredPoints && t.name.Contains((selectLevelCount - 1).ToString()));
    }

    public void RefreshLevels()
    {
        foreach (var block in blocksToShowLevels)
            ShowBlockLevel(block);
    }

    private Vector3 ScreenToCanvasPosition(Vector2 screenPos)
    {
        // Conversion en coordonnées locales du Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            screenPos,
            mainCamera,
            out Vector2 localPos
        );
        return new(localPos.x, localPos.y, 0);
    }
}
