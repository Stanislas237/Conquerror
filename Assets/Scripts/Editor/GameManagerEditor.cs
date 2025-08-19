using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Récupération du GameManager
        GameManager gm = (GameManager)target;

        // Affiche le champ nb_players
        gm.nb_players = EditorGUILayout.IntSlider("Nombre de joueurs", gm.nb_players, 2, 4);

        EditorGUILayout.Space();

        // Affiche les champs en fonction de nb_players
        if (gm.nb_players == 2 || gm.nb_players == 4)
        {
            EditorGUILayout.LabelField("Configuration 2 et 4 joueurs", EditorStyles.boldLabel);
            gm.terrainSize = EditorGUILayout.Vector2IntField("Dimensions", gm.terrainSize);
        }
        else if (gm.nb_players == 3)
        {
            EditorGUILayout.LabelField("Configuration 3 joueurs", EditorStyles.boldLabel);
            gm.terrainRay = EditorGUILayout.IntField("Rayon", gm.terrainRay);
        }
        else
            EditorGUILayout.HelpBox("Veuillez choisir un nombre de joueurs entre 2 et 4.", MessageType.Warning);

        // Marquer la scène comme modifiée si quelque chose a changé
        if (GUI.changed)
            EditorUtility.SetDirty(gm);
    }
}
