// using UnityEngine;
// using System.Linq;
// using UnityEngine.UI;
// using System.Collections.Generic;

// public class GameManagerPair : GameManager<Vector2>
// {
//     private int terrainWidth;

//     protected override void Start()
//     {
//         switch (nb_players)
//         {
//             case 2:
//                 terrainWidth = 8;
//                 for (int i = 1; i <= 48; i++)
//                 {
//                     var obj = Instantiate(transform.GetChild(0), transform).gameObject;
//                     obj.SetActive(true);
//                     obj.name = (i - 1).ToString();
//                     AddEventTriggers(obj, SetUpButton(i - 1));
//                 }
//                 playersPositions[0] = new() { new(0, 5), new(7, 5) };
//                 playersPositions[1] = new() { new(0, 0), new(7, 0) };
//                 break;

//             case 4:
//                 terrainWidth = 9;
//                 for (int i = 1; i <= 81; i++)
//                 {
//                     var obj = Instantiate(transform.GetChild(0), transform).gameObject;
//                     obj.SetActive(true);
//                     obj.name = (i - 1).ToString();

//                     var coordinates = SetUpButton(i - 1);
//                     if (new[] { coordinates.x, coordinates.y }.Any(v => v > 2 && v < 6))
//                         AddEventTriggers(obj, coordinates);
//                     else
//                         Destroy(obj.GetComponentInChildren<Image>());
//                 }

//                 playersPositions[0] = new() { new(3, 0), new(4, 0), new(5, 0) };
//                 playersPositions[1] = new() { new(0, 3), new(0, 4), new(0, 5) };
//                 playersPositions[2] = new() { new(3, 8), new(4, 8), new(5, 8) };
//                 playersPositions[3] = new() { new(8, 3), new(8, 4), new(8, 5) };
//                 break;
//         }

//         base.Start();
//     }
    
//     protected override Vector2 SetUpButton(int index)
//     {
//         var x = index % terrainWidth;
//         return new() { x = x, y = (index - x) / terrainWidth };
//     }

//     protected override void SetColor(Vector2 pos, Color c)
//     {
//         var obj = GameObject.Find((terrainWidth * pos.y + pos.x).ToString());
//         if (obj && obj.transform.GetChild(0).TryGetComponent(out Image img)) img.color = c;
//         else Debug.LogError("Objet non trouvé : " + (terrainWidth * pos.y + pos.x).ToString());
//     }

//     protected override bool Neighbors(Vector2 first, Vector2 second, bool onlyIgnoreMyOwnList)
//     {
//         var dx = Mathf.Abs(first.x - second.x);
//         var dy = Mathf.Abs(first.y - second.y);

//         bool closestPositions = false;
//         if (nb_voisins == 4)
//             closestPositions = ((dx == 1) ^ (dy == 1)) && (dx + dy == 1);
//         else if (nb_voisins == 8)
//             closestPositions = ((dx == 1) || (dy == 1)) && (dx + dy == 1 || dx + dy == 2);

//         if (onlyIgnoreMyOwnList)
//             return closestPositions && !playersPositions[CurrentPlayerIndex].Contains(second);
//         else
//             return closestPositions && !playersPositions.Any(list => list.Contains(second));
//     }
// }
