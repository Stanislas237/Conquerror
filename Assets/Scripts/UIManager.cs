using TMPro;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour
{
    private List<Block> Blocks => GameManager.terrainManager.Blocks;

    public static UIManager Instance;
    private Camera mainCamera;
    private List<Block> blocksToShowLevels = new();

    [SerializeField]
    private Transform LevelTextPrefab;
    [SerializeField]
    private Transform PowersParent;
    [SerializeField]
    private Transform PlayerUIsParent;
    [SerializeField]
    private Image NextButtonImage;
    [SerializeField]
    private TextMeshProUGUI TextMeshMessage;
    [SerializeField]
    private Transform EndPanel;

    // Sélection d'un pouvoir pour la contagion
    private int specialSelectionLevel = 0;
    private TaskCompletionSource<string> _powerSelectionSource;

    public Task<string> WaitForPowerSelectionAsync(int powerLevel)
    {
        specialSelectionLevel = powerLevel;
        _powerSelectionSource = new();
        ShowPowers(true);
        return _powerSelectionSource.Task;
    }



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
        for (int i = 0; i < TerrainManager.nb_players; i++)
        {
            var ui = PlayerUIsParent.GetChild(i);
            var color = DataManager.GetColors()[i].color;
            var text = ui.GetChild(3).GetComponent<TextMeshProUGUI>();
            var p_text = ui.GetChild(4).GetComponent<TextMeshProUGUI>();
            ui.gameObject.SetActive(true);
            ui.GetChild(0).GetComponent<Image>().color = color;
            ui.GetChild(1).GetChild(1).GetComponent<Image>().color = color;
            text.color = p_text.color = color;
            text.text = $"Player {i + 1}";
        }
        ShowPlayerUI();

        foreach (Transform t in PowersParent)
            t.GetComponent<Button>().onClick.AddListener(async () =>
            {
                if (specialSelectionLevel != 0)
                {
                    _powerSelectionSource.SetResult(t.name);
                    specialSelectionLevel = 0;
                }
                else
                    await GameManager.Instance.Fusion(t.name);
            });
    }

    private void UpdateEnergyUI(int index)
    {
        var Energybar = PlayerUIsParent.GetChild(index).GetChild(1).GetChild(1);
        var currScale = Energybar.localScale;
        currScale.y = Mathf.Clamp(DataManager.GetEnergy()[index] / 50f, 0, 1);
        Energybar.localScale = currScale;
    }

    private void UpdateConquestPointsUI(int index)
    {
        var ConquerPointsBar = PlayerUIsParent.GetChild(index).GetChild(2).GetChild(1);
        var currScale = ConquerPointsBar.localScale;
        currScale.y = Mathf.Clamp((float)DataManager.GetConquerPoints()[index] / DataManager.requiredConquerPointsForSpecial, 0, 1);
        ConquerPointsBar.localScale = currScale;
    }

    private void UpdatePercentageText(int index)
    {
        var ConquestPercentage = DataManager.GetPositions(index).Count * 100f / Blocks.Count;

        // Condition de victoire
        if (ConquestPercentage >= 60)
            GameManager.Instance.EndGame(index);

        PlayerUIsParent.GetChild(index).GetChild(4).GetComponent<TextMeshProUGUI>().text = $"{ConquestPercentage:F1} %";
    }

    public void ShowPlayerUI()
    {
        var CurrPlayer = GameManager.Instance.CurrentPlayerId;
        NextButtonImage.color = DataManager.GetColors()[CurrPlayer].color;

        for (int i = 0; i < TerrainManager.nb_players; i++)
        {
            var ui = PlayerUIsParent.GetChild(i).gameObject;
            if (ui.activeSelf)
            {
                UpdateEnergyUI(i);
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
        GameManager.Instance.PauseGameState();
        ShowPowers(false);
        // Afficher le message à l'utilisateur
        Debug.Log($"Message to Player: {message}");
        TextMeshMessage.text = message;
        TextMeshMessage.color = DataManager.GetColors()[GameManager.Instance.CurrentPlayerId].color;
    }

    public void ClearMessage() => TextMeshMessage.text = string.Empty;

    public void ShowBlockLevel(Block block)
    {
        var prefab = LevelTextPrefab.parent.Find($"LevelPrefabForId {block.MyOwnIndex}") ?? Instantiate(LevelTextPrefab, LevelTextPrefab.parent);
        prefab.SetLocalPositionAndRotation(ScreenToCanvasPosition(mainCamera.WorldToScreenPoint(block.Content.transform.position + Vector3.up * 4f)), Quaternion.identity);
        prefab.name = $"LevelPrefabForId {block.MyOwnIndex}";
        prefab.GetComponent<TextMeshProUGUI>().text = block.Level + block.PowerDisplay;
        StartCoroutine(ResetBlockDisplayText(prefab.GetComponent<TextMeshProUGUI>(), block.Level.ToString()));
        prefab.gameObject.SetActive(block.Level > 0);

        if (!blocksToShowLevels.Contains(block))
            blocksToShowLevels.Add(block);
    }

    private IEnumerator ResetBlockDisplayText(TextMeshProUGUI textMesh, string text)
    {
        yield return new WaitForSeconds(0.5f);
        textMesh.text = text;
    }

    public int GetRequiredEnergy(int Level) => Level = (Level >= 5) ? 50 : (Level - 1) * 10;

    public void ShowPowers(bool hasAtLeastOneNotCircled)
    {
        int selectLevelCount = GameManager.Instance.SelectionLevel, currPlayer = GameManager.Instance.CurrentPlayerId, energy = DataManager.GetEnergy()[currPlayer],
        nbBlocks = GameManager.Instance.SelectedBlocks.Count, requiredPoints = GetRequiredEnergy(selectLevelCount);

        if (specialSelectionLevel != 0)
        {
            selectLevelCount = specialSelectionLevel + 1;
            requiredPoints = 0;
            nbBlocks = 2;
        }

        foreach (Transform t in PowersParent)
        {
            var shouldShow = hasAtLeastOneNotCircled && nbBlocks > 1 && energy >= requiredPoints;

            if (!shouldShow && nbBlocks == 1 /*&& DataManager.GetConquerPoints()[currPlayer] > DataManager.requiredConquerPointsForSpecial*/)
            {
                if (t.name == "Contagion")
                    shouldShow = //DataManager.GetPawnTypes()[currPlayer] == PawnType.Conquerant &&
                    Blocks[GameManager.Instance.SelectedBlocks.Last()].NeighborsIndexes.Any(blockId => Blocks[blockId].IsOpponent() && Blocks[blockId].IsCurrentlyPlayable());

                if (t.name == "Téléportation")
                    shouldShow = //DataManager.GetPawnTypes()[currPlayer] == PawnType.Voyageur &&
                    Blocks.Any(block => block.IsEmpty() && block.IsCurrentlyPlayable());

                if (t.name == "Bouclier")
                    shouldShow = //DataManager.GetPawnTypes()[currPlayer] == PawnType.Gardien &&
                    true;

                if (t.name == "Combo")
                    shouldShow = //DataManager.GetPawnTypes()[currPlayer] == PawnType.Archiviste &&
                    true;
            }

            t.gameObject.SetActive(shouldShow);
        }
    }

    public void ShowEndPanel(int winnerId)
    {
        EndPanel.gameObject.SetActive(true);
        var color = DataManager.GetColors()[winnerId].color;
        foreach (var img in EndPanel.GetChild(0).GetComponentsInChildren<Image>())
            if (img.name != "Panel_FG")
                img.color = color;
        var txt = EndPanel.GetComponentInChildren<TextMeshProUGUI>();
        txt.color = color;
        txt.text = $"Player {winnerId + 1} a gagné !";
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
