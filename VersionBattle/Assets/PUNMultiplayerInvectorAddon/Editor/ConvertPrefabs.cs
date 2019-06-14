using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEditor;
using Invector.vShooter;
using System.IO;
using UnityEngine.Events;
using System;
using System.Reflection;

public class ConvertPrefabs : EditorWindow
{
    [MenuItem("Invector/Multiplayer/04. Convert Prefabs To Multiplayer")]
    private static void PUN_ConvertPrefabsWindow()
    {
        GetWindow<ConvertPrefabs>("PUN - Convert Scene To Multiplayer");
    }

    GUISkin skin;
    Vector2 rect = new Vector2(400, 180);
    Vector2 maxrect = new Vector2(400, 400);
    private bool _scanned = false;
    private bool _executed = false;
    Vector2 _scanScrollPos;
    List<string> foundPaths = new List<string>();
    List<string> modified = new List<string>();
    List<bool> buttonOn = new List<bool>();
    Vector2 _modifiedScrollPos;

    private void OnGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        this.minSize = rect;
        this.maxSize = maxrect;
        this.titleContent = new GUIContent("PUN: Multiplayer", null, "Converts Prefabs To Support Multiplayer.");
        GUILayout.BeginVertical("Add Multiplayer Compatiblity To Prefabs", "window");
        GUILayout.Space(35);

        GUILayout.BeginVertical("box");
        if (_scanned == false)
        {
            EditorGUILayout.HelpBox("First scan your project and get a list of prefabs that will be modified. \n\n NOTE: You can edit this list after it's done scanning.", MessageType.Info);
        }
        else if (_executed == false)
        {
            EditorGUILayout.HelpBox("Now select a button to see it in your project. A new button will appear that will allow you to remove it from the modification list.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Completed! \n\n Any exception that is logged will need to be looked at and verified.", MessageType.Info);
        }
        GUILayout.EndVertical();
        if (_scanned == false)
        {
            if (GUILayout.Button("Scan Prefabs"))
            {
                _scanned = true;
                PUN_ScanAllPrefabs();
            }
        }
        else if (_executed == false)
        {
            _scanScrollPos = EditorGUILayout.BeginScrollView(_scanScrollPos, GUILayout.Width(350), GUILayout.Height(250));
            for (int i = 0; i < foundPaths.Count; i++)
            {
                if (buttonOn[i] == true)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(" X "))
                    {
                        foundPaths.RemoveAt(i);
                        buttonOn.RemoveAt(i);
                    }
                    if (GUILayout.Button( Path.GetFileName(foundPaths[i]).Replace(".prefab","") ))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(foundPaths[i]);
                        buttonOn[i] = !buttonOn[i];
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button(Path.GetFileName(foundPaths[i]).Replace(".prefab", "")))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(foundPaths[i]);
                        buttonOn[i] = !buttonOn[i];
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Convert Prefabs To Multiplayer"))
            {
                _executed = true;
                PUN_ConvertPrefabsToMultiplayer();
            }
        }
        else
        {
            _modifiedScrollPos = EditorGUILayout.BeginScrollView(_modifiedScrollPos, GUILayout.Width(350), GUILayout.Height(250), GUILayout.ExpandWidth(true));
            foreach (string targetPath in modified)
            {
                if (GUILayout.Button(Path.GetFileName(targetPath).Replace(".prefab","")))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(targetPath);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    void PUN_ScanAllPrefabs()
    {
        foundPaths.Clear();
        string[] prefabPaths = GetAllPrefabs();
        GameObject target;
        
        foreach (string prefabPath in prefabPaths)
        {
            UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath);
            try
            {
                target = (GameObject)prefab;
                if (target.GetComponent<vShooterWeapon>() && !target.GetComponent<PUN_ShooterWeapon>())
                {
                    foundPaths.Add(prefabPath);
                    buttonOn.Add(false);
                }
            }
            catch
            {
                Debug.LogWarning("Unable to cast: " + prefabPath + " to GameObject, skipping");
            }
        }
    }

    void PUN_ConvertPrefabsToMultiplayer()
    {
        modified.Clear();
        GameObject target;
        foreach(string targetPath in foundPaths)
        {
            UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(targetPath);
            try
            {
                target = (GameObject)prefab;
                Mod_Weapon(target);
                modified.Add(targetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Modifying asset: " + targetPath +", Encountered Exception: "+ex.Message);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    void Mod_Weapon(GameObject target)
    {
        if (target.GetComponent<vShooterWeapon>())
        {
            vShooterWeapon org = target.GetComponent<vShooterWeapon>();
            target.AddComponent<PUN_ShooterWeapon>();
            PUN_Helpers.CopyComponentTo(org, target.GetComponent<PUN_ShooterWeapon>());
            DestroyImmediate(org, true);
            // ------------------------------------------- //
        }
    }

    public string[] GetAllPrefabs()
    {
        string[] temp = AssetDatabase.GetAllAssetPaths();
        List<string> result = new List<string>();
        foreach (string s in temp)
        {
            if (s.Contains(".prefab")) result.Add(s);
        }
        return result.ToArray();
    }
}
