using UnityEngine;

public class HexGridGenerator : MonoBehaviour
{
    public GameObject hexPrefab;
    public int radius = 3;

    private float hexWidth;
    private float hexHeight;

    void Start()
    {
        var rt = hexPrefab.GetComponent<RectTransform>();
        hexWidth = rt.rect.width;
        hexHeight = rt.rect.height;

        GenerateHexagonGrid(radius);
        // gameObject.AddComponent<GameManagerImpair>();
    }

    void GenerateHexagonGrid(int R)
    {
        for (int q = -R; q <= R; q++)
        {
            int r1 = Mathf.Max(-R, -q - R);
            int r2 = Mathf.Min(R, -q + R);
            for (int r = r1; r <= r2; r++)
                CreateHexTile(q, r);
        }
    }

    void CreateHexTile(int q, int r)
    {
        Vector2 pos = HexToPixel(q, r);
        GameObject hexGO = Instantiate(hexPrefab, transform);
        hexGO.SetActive(true);
        hexGO.GetComponent<RectTransform>().anchoredPosition = pos;
        hexGO.name = $"Hex_{q}_{r}";
    }

    Vector2 HexToPixel(int q, int r)
    {
        float x = hexWidth * 0.75f * q;
        float y = hexHeight * (r + q / 2f);
        return new Vector2(x, y);
    }
}
