using UnityEngine;
using UnityEditor;
using Invector.vMelee;
using Invector.vShooter;
using Invector.vCharacterController;
using Invector;
using Photon.Pun;
using System.Collections.Generic;
using System.IO;
using System;
using Invector.vItemManager;
using Invector.vCharacterController.vActions;
using System.Reflection;
using UnityEngine.Events;
using UnityEditor.Events;

public class SetupNetworking : EditorWindow
{

    [MenuItem("Invector/Multiplayer/(Optional) Create Network Manager")]
    private static void M_NetworkManager()
    {
        if (!FindObjectOfType<PUN_NetworkManager>())
        {
            GameObject _networkManager = new GameObject("NetworkManager");
            _networkManager.AddComponent<PUN_NetworkManager>();
            _networkManager.AddComponent<PUN_LobbyUI>();
            _networkManager.GetComponent<PUN_NetworkManager>()._spawnPoint = _networkManager.transform;
            _networkManager.AddComponent<PUN_ItemManager>();
        }
        else
        {
            PUN_NetworkManager _networkManager = FindObjectOfType<PUN_NetworkManager>();
            _networkManager._spawnPoint = _networkManager.gameObject.transform;
        }
    }

    //# ------------------------------------------------------------ #

    GameObject _player = null;
    GUISkin skin;
    Vector2 rect = new Vector2(400, 180);
    Vector2 max_rect = new Vector2(400, 500);
    Editor playerPreview;
    bool generated = false;
    [MenuItem("Invector/Multiplayer/02. Make Player Multiplayer Compatible")]
    private static void M_MakePlayerMultiplayer()
    {
        GetWindow<SetupNetworking>("Photon PUN - Make Player Multiplayer Compatible");
    }

    void PlayerPreview()
    {
        GUILayout.FlexibleSpace();

        if (_player != null)
        {
            playerPreview.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(360, 300), "window");
        }
    }

    private void OnGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        this.minSize = rect;
        this.maxSize = max_rect;
        this.titleContent = new GUIContent("Photon PUN: Multiplayer", null, "Adds multiplayer support to your player.");
        GUILayout.BeginVertical("Add Multiplayer Compatiblity", "window");
        GUILayout.Space(35);

        GUILayout.BeginVertical("box");

        if (!_player)
        {
            generated = false;
            EditorGUILayout.HelpBox("Input your player gameobject you want to make multiplayer compatible. Will copy, modify, and save that gameobject as a resource (in the \"Assets/Resources\" folder).", MessageType.Info);
        }

        _player = EditorGUILayout.ObjectField("Player Prefab", _player, typeof(GameObject), true, GUILayout.ExpandWidth(true)) as GameObject;

        if (GUI.changed && _player != null)
        {
            playerPreview = Editor.CreateEditor(_player);
        }
        if (_player != null && generated == true)
        {
            EditorGUILayout.HelpBox("Last manual steps! \n\n 1. Any components with custom events will need to be verified. If the event wasn't able to be easily copied it will say \"Missing Component\" on the UnityEvent or not be present. Look at the original object and re-add any of the missing events. \n\n 2. If you make any manual changes be sure to apply your changes to the prefab! \n\nNOTE: You can find the prefab in the \"Assets/Resources\" folder\n\n 3. Find the \"Network Manager\" gameobject and add the appropriate \"V Item List Data\" object to the \"PUN_ItemManager\" component that you're using. This will sync weapons across the network.", MessageType.Info);
        }
        GUILayout.EndVertical();

        if (_player != null)
        {
            GUILayout.BeginHorizontal("box");
            PlayerPreview();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Add Multiplayer Support"))
            {
                generated = true;
                M_SetupMultiplayer();
            }
        }
    }

    void M_SetupMultiplayer()
    {
        GameObject prefab = GameObject.Instantiate(_player, _player.transform.position+Vector3.forward, Quaternion.identity) as GameObject;
        prefab.name = "PUN_" + prefab.name.Replace("(Clone)", "");
        Selection.activeGameObject = prefab;

        if (prefab == null)
            return;
        foreach (MonoBehaviour script in prefab.GetComponents(typeof(MonoBehaviour)))
        {
            script.enabled = false;
        }
        if (prefab.GetComponent<PUN_SyncPlayer>() == null)
        {
            prefab.AddComponent<PUN_SyncPlayer>();
        }
        ModifyComponents(prefab);
        AssignDamageReceivers(prefab);
        MakeAndAssignPrefab(prefab);
    }

    void ModifyComponents(GameObject prefab)
    {
        //Set Syncronization
        prefab.GetComponent<PhotonView>().Synchronization = ViewSynchronization.ReliableDeltaCompressed;

        //Sync Rigidbody
        prefab.GetComponent<PhotonRigidbodyView>().m_SynchronizeVelocity = true;
        prefab.GetComponent<PhotonRigidbodyView>().m_SynchronizeAngularVelocity = true;
        prefab.GetComponent<PhotonRigidbodyView>().m_TeleportEnabled = false;
        
        ////Add Photon Components To Photon View To Sync Them over network
        prefab.GetComponent<PhotonView>().ObservedComponents = null;
        List<Component> observables = new List<Component>();
        observables.Add(prefab.GetComponent<PhotonRigidbodyView>());
        observables.Add(prefab.GetComponent<PUN_SyncPlayer>());
        prefab.GetComponent<PhotonView>().ObservedComponents = observables;
        //(Observe Options) https://doc.photonengine.com/en-us/pun/current/getting-started/feature-overview

        //Enable multiplayer compatiable components
        Setup_GenericAction(prefab);
        Setup_GenericAnimation(prefab);
        Setup_ThirdPersonController(prefab);
        Setup_ShooterMeleeInput(prefab);
        Setup_MeleeCombatInput(prefab);
        //Setup_ThrowObject(prefab);
        Setup_LadderAction(prefab);
        Setup_CameraVerify(prefab);
        Setup_WeaponHolders(prefab);
        Setup_ShooterManager(prefab);
        Setup_MeleeManager(prefab);

        //Destroy Non Multiplayer Compatible Components
        DestroyComponents(prefab);

        EnableComponents(prefab);

        //Enable Ragdoll colliders to shooter weapons can hit you
        if (prefab.GetComponent<vRagdoll>()) prefab.GetComponent<vRagdoll>().disableColliders = false;
    }
    void MakeAndAssignPrefab(GameObject prefab)
    {
        try
        {
            //Create the Prefab 
            if (!Directory.Exists("Assets/Resources"))
            {
                //if it doesn't, create it
                Directory.CreateDirectory("Assets/Resources");
            }

            if (AssetDatabase.LoadAssetAtPath("Assets/Resources/" + prefab.name + ".prefab", typeof(GameObject)))
            {
                if (EditorUtility.DisplayDialog("Are you sure?",
                            "The prefab already exists. Do you want to overwrite it?",
                            "Yes",
                            "No"))
                //If the user presses the yes button, create the Prefab
                {
                    CreatePrefab(prefab, "Assets/Resources/" + prefab.name + ".prefab");
                }
                //If the name doesn't exist, create the new Prefab
                else
                {
                    Debug.Log("The prefab for this gameobject \"" + prefab.name + "\" was not made. Make one manually and assign that prefab to the \"_playerPrefab\" on the NetworkManager gameobject.");
                }
            }
            else
            {
                CreatePrefab(prefab, "Assets/Resources/" + prefab.name + ".prefab");
            }
            //Application.dataPath
            M_NetworkManager();
            PUN_NetworkManager nm = FindObjectOfType<PUN_NetworkManager>();
            nm._playerPrefab = (GameObject)Resources.Load(prefab.name);
        }
        catch (Exception e)
        {
            Debug.Log("An error occured. Make sure the prefab was made and that prefab gets assigned to the \"NetworkManager\"");
            Debug.LogError(e);
        }
    }
    void CreatePrefab(GameObject obj, string location)
    {
        Debug.Log("Saving prefab to: " + location);
        UnityEngine.Object newPrefab = PrefabUtility.CreatePrefab(location, obj);
        PrefabUtility.ReplacePrefab(obj, newPrefab, ReplacePrefabOptions.ConnectToPrefab);
        Debug.Log("Prefab successfully made and saved!");
    }
    void AssignDamageReceivers(GameObject prefab)
    {
        TraverseChildren(prefab.transform.root);
        foreach (Transform child in prefab.transform.root)
        {
            if (child.GetComponent<vDamageReceiver>())
            {
                vDamageReceiver original = child.GetComponent<vDamageReceiver>();

                PUN_DamageReceiver pun = child.gameObject.AddComponent<PUN_DamageReceiver>();
                pun.damageMultiplier = child.GetComponent<vDamageReceiver>().damageMultiplier;
                pun.overrideReactionID = child.GetComponent<vDamageReceiver>().overrideReactionID;
                
                DestroyImmediate(original);
            }
        }
    }
    void TraverseChildren(Transform target)
    {
        if (target.GetComponent<vDamageReceiver>())
        {
            vDamageReceiver original = target.GetComponent<vDamageReceiver>();

            PUN_DamageReceiver pun = target.gameObject.AddComponent<PUN_DamageReceiver>();
            pun.damageMultiplier = target.GetComponent<vDamageReceiver>().damageMultiplier;
            pun.overrideReactionID = target.GetComponent<vDamageReceiver>().overrideReactionID;

            DestroyImmediate(original);
        }
        foreach (Transform child in target)
        {
            TraverseChildren(child);
        }
    }

    #region Overrides Component Setups
    void Setup_LadderAction(GameObject prefab)
    {
        if (prefab.GetComponent<vLadderAction>() || prefab.GetComponent<PUN_LadderAction>())
        {
            vLadderAction org = prefab.GetComponent<vLadderAction>();
            if (!prefab.GetComponent<PUN_LadderAction>())
            {
                prefab.AddComponent<PUN_LadderAction>();
                PUN_LadderAction newComp = prefab.GetComponent<PUN_LadderAction>();
                PUN_Helpers.CopyComponentTo(org, newComp);
            }
            prefab.GetComponent<PUN_LadderAction>().enabled = false;
        }
    }
    //void Setup_ThrowObject(GameObject prefab)
    //{
    //    if (prefab.GetComponent<vThrowObject>() || prefab.GetComponent<PUN_ThrowObject>())
    //    {
    //        vThrowObject org = prefab.GetComponent<vThrowObject>();
    //        if (!prefab.GetComponent<PUN_ThrowObject>())
    //        {
    //            prefab.AddComponent<PUN_ThrowObject>();
    //            PUN_ThrowObject newComp = prefab.GetComponent<PUN_ThrowObject>();
    //            PUN_Helpers.CopyComponentTo(org, newComp);
    //        }
    //        prefab.GetComponent<PUN_ThrowObject>().enabled = false;
    //    }
    //}
    void Setup_CameraVerify(GameObject prefab)
    {
        if (!prefab.GetComponent<PUN_ThirdPersonCameraVerify>()) prefab.AddComponent<PUN_ThirdPersonCameraVerify>();
    }
    void Setup_MeleeCombatInput(GameObject prefab)
    {
        if (!prefab.GetComponent<vShooterMeleeInput>() && (prefab.GetComponent<vMeleeCombatInput>() || prefab.GetComponent<PUN_MeleeCombatInput>()))
        {
            vShooterMeleeInput org = prefab.GetComponent<vShooterMeleeInput>();
            if (!prefab.GetComponent<PUN_MeleeCombatInput>())
            {
                prefab.AddComponent<PUN_MeleeCombatInput>();
                PUN_MeleeCombatInput newComp = prefab.GetComponent<PUN_MeleeCombatInput>();
                PUN_Helpers.CopyComponentTo(org, newComp);
            }
            prefab.GetComponent<PUN_MeleeCombatInput>().enabled = false;
        }
    }
    void Setup_ShooterMeleeInput(GameObject prefab)
    {
        if (prefab.GetComponent<vShooterMeleeInput>() || prefab.GetComponent<PUN_ShooterMeleeInput>())
        {
            vShooterMeleeInput org = prefab.GetComponent<vShooterMeleeInput>();
            if (!prefab.GetComponent<PUN_ShooterMeleeInput>())
            {
                prefab.AddComponent<PUN_ShooterMeleeInput>();
                PUN_ShooterMeleeInput newComp = prefab.GetComponent<PUN_ShooterMeleeInput>();
                PUN_Helpers.CopyComponentTo(org, newComp);
            }
            prefab.GetComponent<PUN_ShooterMeleeInput>().enabled = false;
        }
    }
    void Setup_ThirdPersonController(GameObject prefab)
    {
        if (prefab.GetComponent<vThirdPersonController>() || prefab.GetComponent<PUN_ThirdPersonController>())
        {
            vThirdPersonController org = prefab.GetComponent<vThirdPersonController>();
            if (!prefab.GetComponent<PUN_ThirdPersonController>())
            {
                prefab.AddComponent<PUN_ThirdPersonController>();
                PUN_ThirdPersonController newComp = prefab.GetComponent<PUN_ThirdPersonController>();
                PUN_Helpers.CopyComponentTo(org, newComp);
            }
            prefab.GetComponent<PUN_ThirdPersonController>().enabled = true;
            prefab.GetComponent<PUN_ThirdPersonController>().useInstance = false;
        }
    }
    void Setup_GenericAnimation(GameObject prefab)
    {
        if (prefab.GetComponent<vGenericAnimation>() || prefab.GetComponent<PUN_GenericAnimation>())
        {
            vGenericAnimation org = prefab.GetComponent<vGenericAnimation>();
            if (!prefab.GetComponent<PUN_GenericAnimation>())
            {
                prefab.AddComponent<PUN_GenericAnimation>();
                PUN_GenericAnimation newComp = prefab.GetComponent<PUN_GenericAnimation>();
                PUN_Helpers.CopyComponentTo(org, newComp);
            }
            prefab.GetComponent<PUN_GenericAnimation>().enabled = false;
        }
    }
    void Setup_GenericAction(GameObject prefab)
    {
        if (prefab.GetComponent<vGenericAction>() || prefab.GetComponent<PUN_GenericAction>())
        {
            vGenericAction org = prefab.GetComponent<vGenericAction>();
            if (!prefab.GetComponent<PUN_GenericAction>())
            {
                prefab.AddComponent<PUN_GenericAction>();
                PUN_GenericAction newComp = prefab.GetComponent<PUN_GenericAction>();
                PUN_Helpers.CopyComponentTo(org, newComp);
            }
            prefab.GetComponent<PUN_GenericAction>().enabled = false;
        }
    }
    void Setup_WeaponHolders(GameObject prefab)
    {
        vWeaponHolder[] holders = prefab.GetComponentsInChildren<vWeaponHolder>();
        List<vWeaponHolder> PUN_holders = new List<vWeaponHolder>();
        foreach (vWeaponHolder holder in holders)
        {
            GameObject holderGO = holder.gameObject;
            holderGO.AddComponent<PUN_WeaponHolder>();
            PUN_WeaponHolder newComp = holderGO.GetComponent<PUN_WeaponHolder>();
            PUN_Helpers.CopyComponentTo(holder, newComp);
            DestroyImmediate(holder);

            PUN_holders.Add(newComp);
        }
        if (prefab.GetComponent<vWeaponHolderManager>())
        {
            prefab.GetComponent<vWeaponHolderManager>().holders = PUN_holders.ToArray();
        }
    }
    void Setup_ShooterManager(GameObject prefab)
    {
        if (prefab.GetComponent<vShooterManager>())
        {
            vShooterManager org = prefab.GetComponent<vShooterManager>();
            if (!prefab.GetComponent<PUN_ShooterManager>())
            {
                prefab.AddComponent<PUN_ShooterManager>();
                PUN_ShooterManager newComp = prefab.GetComponent<PUN_ShooterManager>();
                PUN_Helpers.CopyComponentTo(org, newComp);
                DestroyImmediate(prefab.GetComponent<vShooterManager>());
            }
        }
    }
    void Setup_MeleeManager(GameObject prefab)
    {
        if (prefab.GetComponent<vMeleeManager>())
        {
            vMeleeManager org = prefab.GetComponent<vMeleeManager>();
            if (!prefab.GetComponent<PUN_MeleeManager>())
            {
                prefab.AddComponent<PUN_MeleeManager>();
                PUN_MeleeManager newComp = prefab.GetComponent<PUN_MeleeManager>();
                PUN_Helpers.CopyComponentTo(org, newComp);
                DestroyImmediate(prefab.GetComponent<vMeleeManager>());
            }
        }
    }
    void EnableComponents(GameObject prefab)
    {
        if (prefab.GetComponent<vRagdoll>()) prefab.GetComponent<vRagdoll>().enabled = true;
        if (prefab.GetComponent<vFootStep>()) prefab.GetComponent<vFootStep>().enabled = true;
        prefab.GetComponent<PUN_ThirdPersonCameraVerify>().enabled = true;
        prefab.GetComponent<PUN_SyncPlayer>().enabled = true;
        prefab.GetComponent<PhotonRigidbodyView>().enabled = true;
        if (prefab.GetComponent<vFootStep>()) prefab.GetComponent<vFootStep>().enabled = true;
    }
    #endregion

    #region Network Helper Components
    void Setup_NetworkDestroy()
    {

    }
    #endregion

    #region Destroy Components
    void DestroyComponents(GameObject prefab)
    {
        if (prefab.GetComponent<vThirdPersonController>()) DestroyImmediate(prefab.GetComponent<vThirdPersonController>());
        if (prefab.GetComponent<vShooterMeleeInput>()) DestroyImmediate(prefab.GetComponent<vShooterMeleeInput>());
        if (!prefab.GetComponent<vShooterMeleeInput>() && prefab.GetComponent<vMeleeCombatInput>()) DestroyImmediate(prefab.GetComponent<vMeleeCombatInput>());
        if (prefab.GetComponent<vGenericAction>()) DestroyImmediate(prefab.GetComponent<vGenericAction>());
        if (prefab.GetComponent<vGenericAnimation>()) DestroyImmediate(prefab.GetComponent<vGenericAnimation>());
        //if (prefab.GetComponent<vThrowObject>()) DestroyImmediate(prefab.GetComponent<vThrowObject>());
        if (prefab.GetComponent<vLadderAction>()) DestroyImmediate(prefab.GetComponent<vLadderAction>());
    }
    #endregion
    
    #region Copying UnityEvents
    //void RebuildMissingComponents(GameObject prefab, UnityEvent prefabsEvent, UnityEvent originalEvent)
    //{
    //    for (int i = 0; i < prefabsEvent.GetPersistentEventCount(); i++)
    //    {
    //        if (prefabsEvent.GetPersistentTarget(i) == null)
    //        {
    //            if (prefabsEvent.GetPersistentTarget(i).GetType() == typeof(Component))
    //            {
    //                Component val = GetValidComponent(prefab.transform, prefabsEvent.GetPersistentTarget(i).name);
    //                MethodInfo info = UnityEventBase.GetValidMethodInfo(prefabsEvent.GetPersistentTarget(i), prefabsEvent.GetPersistentMethodName(i), new Type[] { typeof(Component) });
    //                UnityAction<Component> execute = (Component obj) => info.Invoke(val, new UnityEngine.Component[] { val });
    //                UnityEventTools.AddPersistentListener(execute);
    //                UnityEventTools.AddObjectPersistentListener<GameObject>(dest, execute, arg.gameObject);
    //            }
    //        }
    //    }
    //}
    //Component GetValidComponent(Transform target, string componentName)
    //{
    //    Component val = null;
    //    foreach (Component comp in target)
    //    {
    //        if (comp.name == componentName)
    //        {
    //            return comp;
    //        }
    //    }
    //    foreach (Transform child in target)
    //    {
    //        foreach(Component comp in child)
    //        {
    //            if (comp.name == componentName)
    //            {
    //                return comp;
    //            }
    //        }
    //        val = GetValidComponent(child, componentName);
    //        if (val != null)
    //        {
    //            return val;
    //        }
    //    }
    //    return null;
    //}
    //void CopyUnityEvent(GameObject prefab, UnityEvent source, UnityEvent dest)
    //{
    //    dest = source;
    //    for (int i = 0; i < source.GetPersistentEventCount(); i++)
    //    {
    //        if (source.GetPersistentTarget(i).GetType() == typeof(GameObject))
    //        {
    //            Transform arg = FindValidTarget(_player, prefab, source, i);
    //            MethodInfo info = UnityEventBase.GetValidMethodInfo(source.GetPersistentTarget(i), source.GetPersistentMethodName(i), new Type[] { typeof(GameObject) });
    //            UnityAction<GameObject> execute = (GameObject obj) => info.Invoke(arg, new UnityEngine.Object[] { arg });
    //            UnityEventTools.AddObjectPersistentListener<GameObject>(dest, execute, arg.gameObject);
    //        }
    //    }
    //    //for (int i = 0; i < source.GetPersistentEventCount(); i++)
    //    //{
    //    //    Type inputType = source.GetPersistentTarget(i).GetType();
    //    //    MethodInfo info;
    //    //    if (inputType == typeof(bool))
    //    //    {
    //    //        info = UnityEventBase.GetValidMethodInfo(source.GetPersistentTarget(i), source.GetPersistentMethodName(i), new Type[] { typeof(bool) });
    //    //        //execute = () => info.Invoke(source.GetPersistentTarget(i), new object[] { source.GetPersistentTarget(i) });
    //    //    }
    //    //    else if (inputType == typeof(float))
    //    //    {
    //    //        info = UnityEventBase.GetValidMethodInfo(source.GetPersistentTarget(i), source.GetPersistentMethodName(i), new Type[] { typeof(float) });
    //    //        //execute = () => info.Invoke(source.GetPersistentTarget(i), new object[] { source.GetPersistentTarget(i) });
    //    //    }
    //    //    else if (inputType == typeof(int))
    //    //    {
    //    //        info = UnityEventBase.GetValidMethodInfo(source.GetPersistentTarget(i), source.GetPersistentMethodName(i), new Type[] { typeof(int) });
    //    //        //execute = () => info.Invoke(source.GetPersistentTarget(i), new object[] { source.GetPersistentTarget(i) });
    //    //    }
    //    //    else
    //    //    {
    //    //        UnityEngine.Object arg = (UnityEngine.Object)FindValidTarget(_player, prefab, source, i);
    //    //        //MethodInfo function = GetFunction(prefab, source.GetPersistentMethodName(i));
    //    //        //Debug.Log(source.GetPersistentMethodName(i));
    //    //        //Debug.Log("FUNCTION:" + function);

    //        //        UnityAction<UnityEngine.Object> action = System.Delegate.CreateDelegate(typeof(UnityAction), source.GetPersistentMethodName(i), eventName) as UnityAction<UnityEngine.Object>;
    //        //        UnityEventTools.AddObjectPersistentListener(dest, action, arg);
    //        //        //action 
    //        //        //UnityAction<UnityEngine.Object> execute = (UnityEngine.Object obj) => info.Invoke(arg, new UnityEngine.Object[] { arg });
    //        //        //UnityEventTools.AddObjectPersistentListener(dest, execute, arg);
    //        //    }
    //        //}
    //}
    //Transform FindValidTarget(GameObject owner, GameObject prefab, UnityEvent target, int index)
    //{
    //    Transform retVal = null;
    //    var argument = target.GetPersistentTarget(index);
    //    if (argument.GetType() == typeof(GameObject) || argument.GetType() == typeof(Transform))
    //    {
    //        retVal = GetObjectWithName(owner.transform, argument.name);
    //        if (retVal != null)
    //        {
    //            retVal = GetObjectWithName(prefab.transform, argument.name);
    //        }
    //    }

    //    return retVal;
    //}
    //Transform GetObjectWithName(Transform parent, string nameToSearchFor)
    //{
    //    Transform retVal = null;
    //    foreach(Transform child in parent)
    //    {
    //        if (child.name == nameToSearchFor)
    //        {
    //            retVal = child;
    //            break;
    //        }
    //        else
    //        {
    //            retVal = GetObjectWithName(child, nameToSearchFor);
    //            if (retVal != null)
    //            {
    //                break;
    //            }
    //        }
    //    }
    //    return retVal;
    //}
    //MethodInfo GetFunction(GameObject target, string functionName)
    //{
    //    MethodInfo retVal = null;

    //    foreach (var component in target.GetComponents<Component>())
    //    {
    //        foreach (var method in component.GetType().GetMethods())
    //        {
    //            if (method.Name == functionName)
    //            {
    //                retVal = component.GetType().GetMethod(functionName);
    //                break;
    //            }
    //        }
    //        if (retVal != null)
    //        {
    //            break;
    //        }
    //    }

    //    return retVal;
    //}
    #endregion
}
