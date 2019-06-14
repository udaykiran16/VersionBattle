using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public static class PUN_Helpers
{
    public static void CopyComponentTo(Component orgComp, Component newComp)
    {
        Type type = orgComp.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);

        foreach (var pinfo in pinfos)
        {
            if (pinfo.CanWrite)
            {
                try
                {
                    pinfo.SetValue(newComp, pinfo.GetValue(orgComp, null), null);
                }
                catch
                {
                    Debug.LogWarning("Failed To Set property value of "+pinfo.Name+" for "+newComp+" component");
                }
            }
        }

        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos)
        {
            try
            {
                finfo.SetValue(newComp, finfo.GetValue(orgComp));
            }
            catch
            {
                Debug.LogWarning("Failed to set field value: " + finfo.Name + " for " + newComp + " component.");
            }
        }
    }

    public static GameObject FindExampleUIPrefab()
    {
        string[] prefabPaths = GetAllPrefabs();
        GameObject target;

        foreach (string prefabPath in prefabPaths)
        {
            UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath);
            try
            {
                target = (GameObject)prefab;
                if (target.GetComponent<PUN_ExampleUI>())
                {
                    return target;
                }
            }
            catch
            {
                Debug.LogWarning("Unable to cast: " + prefabPath + " to GameObject, skipping");
            }
        }

        return null;
    }


    public static string[] GetAllPrefabs()
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


