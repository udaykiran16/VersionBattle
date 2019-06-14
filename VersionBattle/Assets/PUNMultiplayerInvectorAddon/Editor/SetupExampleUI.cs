using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public class SetupExampleUI : EditorWindow
{
    List<string> foundPaths = new List<string>();

    [MenuItem("Invector/Multiplayer/(Optional) Setup Example UI")]
    private static void M_ExampleUI()
    {
        string UIPath = "Assets/PUNMultiplayerInvectorAddon/UI/Example UI.prefab";
        UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(UIPath);
        GameObject target = (GameObject)prefab;
        GameObject UI = PrefabUtility.InstantiatePrefab(target) as GameObject;
        UI.transform.SetParent(FindObjectOfType<PUN_NetworkManager>().transform);

        //Add needed custom action to remove UI
        UnityAction<bool> action = UI.SetActive;
        UnityEventTools.AddBoolPersistentListener(FindObjectOfType<PUN_NetworkManager>()._customNetworkEvents._roomEvents._onJoinedRoom, action, false);
    }
}