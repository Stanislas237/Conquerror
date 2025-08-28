using TMPro;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private Camera mainCamera;
    private List<Block> blocksToShowLevels = new();
    public Action<string> Fusion;
    public bool JustAssignPower = false;

    [SerializeField]
    private Transform LevelTextPrefab;
    [SerializeField]
    private Transform PowersParent;
    [SerializeField]
    private Transform PlayerUIsParent;

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
        for (int i = 0; i < GameManager.terrainManager.nb_players; i++)
        {
            var ui = PlayerUIsParent.GetChild(i);
            var color = DataManager.GetColors()[i].color;
            var text = ui.GetChild(2).GetComponent<TextMeshProUGUI>();
            var p_text = ui.GetChild(3).GetComponent<TextMeshProUGUI>();
            ui.gameObject.SetActive(true);
            ui.GetChild(0).GetComponent<Image>().color = color;
            ui.GetChild(1).GetChild(1).GetComponent<Image>().color = color;
            text.color = p_text.color = color;
            text.text = $"Player {i + 1}";
        }
        ShowPlayerUI();

        foreach (Transform t in PowersParent)
            t.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (JustAssignPower)
                Fusion?.Invoke(t.name[1..]);
            });
    }

    private void UpdateConquestPointsUI(int index)
    {
        var ConquerPointsBar = PlayerUIsParent.GetChild(index).GetChild(1).GetChild(1);
        var currScale = ConquerPointsBar.localScale;
        currScale.y = DataManager.GetConquerPoints()[index] / 50f;
        ConquerPointsBar.localScale = currScale;
    }

    private void UpdatePercentageText(int index)
    {
        var ConquestPercentage = DataManager.GetPositions(index).Count * 100f / GameManager.terrainManager.Blocks.Count;
        PlayerUIsParent.GetChild(index).GetChild(3).GetComponent<TextMeshProUGUI>().text = $"{ConquestPercentage:F1} %";
    }

    public void ShowPlayerUI()
    {
        var CurrPlayer = GameManager.Instance.CurrentPlayerId;

        for (int i = 0; i < GameManager.terrainManager.nb_players; i++)
        {
            var ui = PlayerUIsParent.GetChild(i).gameObject;
            if (ui.activeSelf)
            {
                UpdateConquestPointsUI(i);
                UpdatePercentageText(i);

                foreach (var img in ui.GetComponentsInChildren<Image>())
                {
                    var currColor = img.color;
                    currColor.a = i == CurrPlayer ? 1 : 0.3f;
                    img.color = currColor;
                }
            }
        }
    }

    public void AskMessageToPlayer(string message)
    {
        ShowPowers(false);
        // Afficher le message à l'utilisateur
        Debug.Log($"Message to Player: {message}");
    }

    public void ShowBlockLevel(Block block)
    {
        var prefab = LevelTextPrefab.parent.Find($"LevelPrefabForId {block.myOwnIndex}") ?? Instantiate(LevelTextPrefab, LevelTextPrefab.parent);
        prefab.SetLocalPositionAndRotation(ScreenToCanvasPosition(mainCamera.WorldToScreenPoint(block.Content.transform.position + Vector3.up * 4f)), Quaternion.identity);
        prefab.name = $"LevelPrefabForId {block.myOwnIndex}";
        prefab.GetComponent<TextMeshProUGUI>().text = block.Level + block.PowerDisplay;
        prefab.gameObject.SetActive(block.Level > 0);

        if (!blocksToShowLevels.Contains(block))
            blocksToShowLevels.Add(block);
    }

    public void ShowPowers(bool hasAtLeastOneNotCircled)
    {
        int selectLevelCount = GameManager.Instance.SelectionLevel, conquerPoints = DataManager.GetConquerPoints()[GameManager.Instance.CurrentPlayerId],
        nbBlocks = GameManager.Instance.SelectedBlocks.Count, requiredPoints = selectLevelCount >= 5 ? 50 : (selectLevelCount - 1) * 10;

        foreach (Transform t in PowersParent)
        {
            var shouldShow = hasAtLeastOneNotCircled && nbBlocks > 1 && conquerPoints >= requiredPoints && t.name[..1] == (selectLevelCount - 1).ToString();
            if (t.name == "2Contagion")
                shouldShow &= DataManager.GetNbContagions()[GameManager.Instance.CurrentPlayerId] < DataManager.MaxNbUseOfContagion
                && GameManager.terrainManager.Blocks[GameManager.Instance.SelectedBlocks.Last()].NeighborsIndexes.Any(blockId => !new int[2] { -1, GameManager.Instance.CurrentPlayerId }.Contains(GameManager.terrainManager.Blocks[blockId].OwnerId));

            t.gameObject.SetActive(shouldShow);
        }
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
