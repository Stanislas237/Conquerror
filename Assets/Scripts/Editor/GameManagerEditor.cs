using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Récupération du GameManager
        TerrainManager tm = (TerrainManager)target;

        // Affiche le champ nb_players
        tm.nb_players = EditorGUILayout.IntSlider("Nombre de joueurs", tm.nb_players, 2, 4);

        // Affiche le champ mode alternatif
        tm.alt_mode = EditorGUILayout.Toggle("Mode Alternatif", tm.alt_mode);

        // Affiche le champ Simulation de voisins
        tm.simulateNeighbors = EditorGUILayout.Toggle("Simulations de voisins", tm.simulateNeighbors);

        EditorGUILayout.Space();

        // Affiche les champs en fonction de nb_players
        if (tm.nb_players == 2 || tm.nb_players == 4)
        {
            EditorGUILayout.LabelField("Configuration 2 et 4 joueurs", EditorStyles.boldLabel);
            tm.terrainSize = EditorGUILayout.Vector2IntField("Dimensions", tm.terrainSize);
        }
        else if (tm.nb_players == 3)
        {
            EditorGUILayout.LabelField("Configuration 3 joueurs", EditorStyles.boldLabel);
            tm.terrainRay = EditorGUILayout.IntField("Rayon", tm.terrainRay);
        }
        else
            EditorGUILayout.HelpBox("Veuillez choisir un nombre de joueurs entre 2 et 4.", MessageType.Warning);

        // Marquer la scène comme modifiée si quelque chose a changé
        if (GUI.changed)
            EditorUtility.SetDirty(tm);
    }
}
