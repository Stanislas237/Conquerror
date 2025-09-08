using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField]
    private string[] terrainShapes;
    private int currentTerrainShape = 0;

    [SerializeField]
    private string[] blockShapes;
    private int currentBlockShape = 0;

    [SerializeField]
    private Transform Content1;

    [SerializeField]
    private Transform Content2;

    [SerializeField]
    private Transform Content3;

    [SerializeField]
    private Transform TerrainShapeContent;

    [SerializeField]
    private Transform BlockShapeContent;

    [SerializeField]
    private TextMeshProUGUI NbPlayersText;
    
    private event Action<string> OnShapeChanged;

    private void Start()
    {
        foreach (var dropdown in Content1.GetComponentsInChildren<TMP_Dropdown>())
            if (int.TryParse(dropdown.name, out int index))
            {
                dropdown.value = (int)DataManager.GetPawnTypes()[index - 1];
                dropdown.RefreshShownValue();
                dropdown.onValueChanged.AddListener((value) => DataManager.SetPawnType(index - 1, (PawnType)value));
            }

        Increment_NB_Players(0);

        foreach (var toggle in Content2.GetComponentsInChildren<Toggle>())
            switch (toggle.name)
            {
                case "AltMode":
                    toggle.isOn = TerrainManager.alt_mode;
                    toggle.onValueChanged.AddListener((value) => TerrainManager.alt_mode = value);
                    break;

                case "Simulation":
                    toggle.isOn = TerrainManager.simulateNeighbors;
                    toggle.onValueChanged.AddListener((value) => TerrainManager.simulateNeighbors = value);
                    break;

            }

        currentTerrainShape = Array.IndexOf(terrainShapes, TerrainManager.TerrainShape);
        Increment_Terrain_Shape(0);
        currentBlockShape = Array.IndexOf(blockShapes, TerrainManager.BlockShape);
        Increment_Block_Shape(0);

        foreach (var slider in Content3.GetComponentsInChildren<Slider>())
            switch (slider.name)
            {
                case "Longueur":
                    slider.value = TerrainManager.terrainSize.y;
                    slider.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Longueur : {(int)slider.value}";

                    slider.onValueChanged.AddListener((value) =>
                    {
                        TerrainManager.terrainSize.y = (int)value;
                        slider.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Longueur : {(int)value}";
                    });

                    OnShapeChanged += newShape => slider.transform.parent.gameObject.SetActive(newShape != "Hexa");
                    break;

                case "Largeur":
                    slider.value = TerrainManager.terrainSize.x;
                    slider.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Largeur : {(int)slider.value}";

                    slider.onValueChanged.AddListener((value) =>
                    {
                        TerrainManager.terrainSize.x = (int)value;
                        slider.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Largeur : {(int)value}";
                    });

                    OnShapeChanged += newShape => slider.transform.parent.gameObject.SetActive(newShape != "Hexa");
                    break;

                case "Rayon":
                    slider.value = TerrainManager.terrainRay;
                    slider.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Rayon : {(int)slider.value}";

                    slider.onValueChanged.AddListener((value) =>
                    {
                        TerrainManager.terrainRay = (int)value;
                        slider.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Rayon : {(int)value}";
                    });

                    OnShapeChanged += newShape => slider.transform.parent.gameObject.SetActive(newShape == "Hexa");
                    break;
            }

        OnShapeChanged?.Invoke(TerrainManager.TerrainShape);
    }

    public void Increment_NB_Players(int increment)
    {
        TerrainManager.nb_players = Mathf.Clamp(TerrainManager.nb_players + increment, 2, 4);
        NbPlayersText.text = TerrainManager.nb_players.ToString();
    }

    public void Increment_Terrain_Shape(int increment)
    {
        currentTerrainShape = (currentTerrainShape + increment + terrainShapes.Length) % terrainShapes.Length;
        TerrainManager.TerrainShape = terrainShapes[currentTerrainShape];
        OnShapeChanged?.Invoke(TerrainManager.TerrainShape);

        foreach (Transform shape in TerrainShapeContent)
            shape.gameObject.SetActive(shape.name == TerrainManager.TerrainShape);
    }

    public void Increment_Block_Shape(int increment)
    {
        currentBlockShape = (currentBlockShape + increment + blockShapes.Length) % blockShapes.Length;
        TerrainManager.BlockShape = blockShapes[currentBlockShape];

        foreach (Transform shape in BlockShapeContent)
            shape.gameObject.SetActive(shape.name == TerrainManager.BlockShape);
    }

    public void OpenScene(string name) => SceneManager.LoadScene(name);
}
