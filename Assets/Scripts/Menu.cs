using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField]
    private Slider nb_players;
    
    [SerializeField]
    private Slider width;

    [SerializeField]
    private Slider height;

    [SerializeField]
    private Slider ray;

    [SerializeField]
    private Toggle alt_mode;

    [SerializeField]
    private Toggle simulateNeighbors;

    private void Start()
    {
        nb_players.value = TerrainManager.nb_players;
        width.value = TerrainManager.terrainSize.x;
        height.value = TerrainManager.terrainSize.y;
        ray.value = TerrainManager.terrainRay;
        alt_mode.isOn = TerrainManager.alt_mode;
        simulateNeighbors.isOn = TerrainManager.simulateNeighbors;

        RefreshDisplay();

        nb_players.onValueChanged.AddListener((value) =>
            {
                int val = (int)value;
                nb_players.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Nb Joueurs : {val}";
                TerrainManager.nb_players = val;
                RefreshDisplay();
            });
        width.onValueChanged.AddListener((value) =>
            {
                int val = (int)value;
                width.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Largeur : {val}";
                TerrainManager.terrainSize.x = val;
            });
        height.onValueChanged.AddListener((value) =>
            {
                int val = (int)value;
                height.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Longueur : {val}";
                TerrainManager.terrainSize.y = val;
            });
        ray.onValueChanged.AddListener((value) =>
            {
                int val = (int)value;
                ray.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Rayon : {val}";
                TerrainManager.terrainRay = val;
            });
        alt_mode.onValueChanged.AddListener((value) => TerrainManager.alt_mode = value);
        simulateNeighbors.onValueChanged.AddListener((value) => TerrainManager.simulateNeighbors = value);
    }

    private void RefreshDisplay()
    {
        width.transform.parent.gameObject.SetActive(TerrainManager.nb_players != 3);
        height.transform.parent.gameObject.SetActive(TerrainManager.nb_players != 3);
        ray.transform.parent.gameObject.SetActive(TerrainManager.nb_players == 3);
        width.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Largeur : {width.value}";
        height.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Longueur : {height.value}";
        ray.transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Rayon : {ray.value}";
    }

    public void OpenScene(string name) => SceneManager.LoadScene(name);
}
