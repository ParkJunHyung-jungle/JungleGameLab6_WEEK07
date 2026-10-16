using UnityEngine;

public sealed class KeyboardCustomizationTestUI : MonoBehaviour
{
    [Header("Storage")]
    [SerializeField] private KeyboardCustomizationStorage _storage;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 420f, 180f), GUI.skin.box);
        GUILayout.Label("Keyboard customization save / load");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(36f))) _storage.SaveCustomization();
        if (GUILayout.Button("Change sample", GUILayout.Height(36f))) _storage.ApplySampleChange();
        if (GUILayout.Button("Load", GUILayout.Height(36f))) _storage.LoadCustomization();
        GUILayout.EndHorizontal();
        GUILayout.Label(_storage.LastMessage);
        GUILayout.EndArea();
    }
}
