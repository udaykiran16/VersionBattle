 using UnityEngine;
using UnityEditor;
using System.IO;                                    //for modifying files (reading and writing)
using System.Collections.Generic;                   //For Lists

public class ModifyScripts : EditorWindow
{
    #region Helper Classes
    public class M_FileData
    {
        public bool exists = false;
        public string path = "";
    }
    public class M_Additions
    {
        public string target = "";
        public string add = "";
        public string nextline = "";
        public M_FileAddtionType type = M_FileAddtionType.NewLine;
    }
    #endregion

    #region Editor Variables
    GUISkin skin;
    Vector2 rect = new Vector2(400, 180);
    Vector2 maxrect = new Vector2(400, 180);
    private bool _executed = false;
    public enum M_FileAddtionType { Replace, NewLine, InsertLine }
    private int _index = 0;
    #endregion

    #region List Of Available Files 
    M_FileData _meleeCombatInput;
    M_FileData _monoBehavior;
    M_FileData _damageReceiver;
    M_FileData _imeleeFighter;
    M_FileData _itemManager;
    M_FileData _ladderAction;
    M_FileData _throwObject;
    M_FileData _weaponHolder;
    //M_FileData _shooterWeapon;
    #endregion

    [MenuItem("Invector/Multiplayer/01. Add Multiplayer To Invector Scripts")]
    private static void M_ChangeScripts()
    {
        GetWindow<ModifyScripts>("UNet - Modify Scripts");
    }
    private void OnGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        this.minSize = rect;
        this.maxSize = maxrect;
        this.titleContent = new GUIContent("UNet: Multiplayer", null, "Adds multiplayer support to Invector scripts.");
        GUILayout.BeginVertical("Add Multiplayer Compatiblity To Scripts", "window");
        GUILayout.Space(35);

        GUILayout.BeginVertical("box");
        if (_executed == false)
        {
            EditorGUILayout.HelpBox("This will modify all relevant Invector scripts by adding needed lines to their scripts. This will make these script send their needed data across the network. Note: There is no automated process to undo these changes. If you wish to undo look at the \"InvectorScriptChanges.txt\" file that will contain a list of changes.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Done! The invector scripts will now be synced across the network. If you would like to undo these changes look at the InvectorScriptChanges.txt file to see what lines were added to what files.", MessageType.Info);
        }
        GUILayout.EndVertical();
        if (_executed == false)
        {
            if (GUILayout.Button("Add Multiplayer Lines"))
            {
                _executed = true;
                M_AddMultiplayerToScripts();
            }
        }
    }

    void M_AddMultiplayerToScripts()
    {
        M_ControlAimCanvas();
        M_ThrowUI();
        M_DamageReceiver_Shooter();
        M_IMeleeFighter();
        M_ItemManager();
        M_LadderAction();
        M_ThrowObject();
        M_WeaponHolder();
        //M_ShooterWeapon();
    }

    #region Individual Files Modification Instructions
    void M_ThrowUI()
    {
        _monoBehavior = FileExists("vThrowUI.cs", Application.dataPath);
        if (_monoBehavior.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "private void Start()";
            adding[0].add = "protected virtual void Start()";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "void UpdateCount()";
            adding[1].add = "protected void UpdateCount()";
            adding[1].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_monoBehavior.path, newlines);
        }
    }
    void M_ControlAimCanvas()
    {
        _meleeCombatInput = FileExists("vControlAimCanvas.cs", Application.dataPath);
        if (_meleeCombatInput.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[1];
            adding[0] = new M_Additions();
            adding[0].target = "public void SetWordPosition(Vector3 wordPosition, bool validPoint = true)";
            adding[0].add = "public virtual void SetWordPosition(Vector3 wordPosition, bool validPoint = true)";
            adding[0].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_meleeCombatInput.path, newlines);
        }
    }
    void M_DamageReceiver_Shooter()
    {
        _damageReceiver = FileExists("vDamageReceiver.cs", Application.dataPath+"/Invector-3rdPersonController/Shooter");
        if (_damageReceiver.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "public void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)";
            adding[0].add = "public virtual void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "private vIHealthController healthController;";
            adding[1].add = "protected vIHealthController healthController;";
            adding[1].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_damageReceiver.path, newlines);
        }
    }
    void M_IMeleeFighter()
    {
        _imeleeFighter = FileExists("vIMeleeFighter.cs", Application.dataPath);
        if (_imeleeFighter.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "using System.Collections;";
            adding[0].add = "using Photon.Pun;";
            adding[0].type = M_FileAddtionType.NewLine;
            adding[1] = new M_Additions();
            adding[1].target = "else receiver.ApplyDamage(damage);";
            adding[1].add = "if (receiver.transform.root.gameObject.GetComponent<PhotonView>() && receiver.transform.root.gameObject.GetComponent<PhotonView>().IsMine == false) { receiver.transform.root.gameObject.GetComponent<PhotonView>().RPC(\"ApplyDamage\", RpcTarget.Others, JsonUtility.ToJson(damage)); }";
            adding[1].nextline = "}";
            adding[1].type = M_FileAddtionType.InsertLine;

            newlines.AddRange(adding);
            ModifyFile(_imeleeFighter.path, newlines);
        }
    }
    void M_ItemManager()
    {
        _itemManager = FileExists("vItemManager.cs", Application.dataPath);
        if (_itemManager.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[3];
            adding[0] = new M_Additions();
            adding[0].target = "private float equipTimer;";
            adding[0].add = "protected float equipTimer;";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "IEnumerator EquipItemRoutine(EquipPoint equipPoint, vItem item)";
            adding[1].add = "protected virtual IEnumerator EquipItemRoutine(EquipPoint equipPoint, vItem item)";
            adding[1].type = M_FileAddtionType.Replace;
            adding[2] = new M_Additions();
            adding[2].target = "IEnumerator UnequipItemRoutine(EquipPoint equipPoint, vItem item)";
            adding[2].add = "protected virtual IEnumerator UnequipItemRoutine(EquipPoint equipPoint, vItem item)";
            adding[2].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_itemManager.path, newlines);
        }
    }
    void M_LadderAction()
    {
        _ladderAction = FileExists("vLadderAction.cs", Application.dataPath);
        if (_ladderAction.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[3];
            adding[0] = new M_Additions();
            adding[0].target = "void TriggerEnterLadder()";
            adding[0].add = "protected virtual void TriggerEnterLadder()";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "void ExitLadderInput()";
            adding[1].add = "protected virtual void ExitLadderInput()";
            adding[1].type = M_FileAddtionType.Replace;
            adding[2] = new M_Additions();
            adding[2].target = "void ResetPlayerSettings()";
            adding[2].add = "protected virtual void ResetPlayerSettings()";
            adding[2].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_ladderAction.path, newlines);
        }
    }
    void M_ThrowObject()
    {
        _throwObject = FileExists("vThrowObject.cs", Application.dataPath);
        if (_throwObject.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[9];
            adding[0] = new M_Additions();
            adding[0].target = "void UpdateInput()";
            adding[0].add = "protected virtual void UpdateInput()";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "private bool isAiming;";
            adding[1].add = "protected bool isAiming;";
            adding[1].type = M_FileAddtionType.Replace;
            adding[2] = new M_Additions();
            adding[2].target = "private bool inThrow;";
            adding[2].add = "protected bool inThrow;";
            adding[2].type = M_FileAddtionType.Replace;
            adding[3] = new M_Additions();
            adding[3].target = "vThirdPersonInput tpInput;";
            adding[3].add = "protected vThirdPersonInput tpInput;";
            adding[3].type = M_FileAddtionType.Replace;
            adding[4] = new M_Additions();
            adding[4].target = "private bool isThrowInput;";
            adding[4].add = "protected bool isThrowInput;";
            adding[4].type = M_FileAddtionType.Replace;
            adding[5] = new M_Additions();
            adding[5].target = "void UpdateThrow()";
            adding[5].add = "protected virtual void UpdateThrow()";
            adding[5].type = M_FileAddtionType.Replace;
            adding[6] = new M_Additions();
            adding[6].target = "private LineRenderer lineRenderer;";
            adding[6].add = "protected LineRenderer lineRenderer;";
            adding[6].type = M_FileAddtionType.Replace;
            adding[7] = new M_Additions();
            adding[7].target = "void DrawTrajectory()";
            adding[7].add = "protected void DrawTrajectory()";
            adding[7].type = M_FileAddtionType.Replace;
            adding[8] = new M_Additions();
            adding[8].target = "IEnumerator Launch()";
            adding[8].add = "protected IEnumerator Launch()";
            adding[8].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_throwObject.path, newlines);
        }
    }
    void M_WeaponHolder()
    {
        _weaponHolder = FileExists("vWeaponHolder.cs", Application.dataPath);
        if (_weaponHolder.exists == true)
        {
            List<M_Additions> newlines = new List<M_Additions>();
            M_Additions[] adding = new M_Additions[2];
            adding[0] = new M_Additions();
            adding[0].target = "public void SetActiveHolder(bool active)";
            adding[0].add = "public virtual void SetActiveHolder(bool active)";
            adding[0].type = M_FileAddtionType.Replace;
            adding[1] = new M_Additions();
            adding[1].target = "public void SetActiveWeapon(bool active)";
            adding[1].add = "public virtual void SetActiveWeapon(bool active)";
            adding[1].type = M_FileAddtionType.Replace;

            newlines.AddRange(adding);
            ModifyFile(_weaponHolder.path, newlines);
        }
    }
    //void M_ShooterWeapon()
    //{
    //    _shooterWeapon = FileExists("vShooterWeapon.cs", Application.dataPath);
    //    if (_shooterWeapon.exists == true)
    //    {
    //        List<M_Additions> newlines = new List<M_Additions>();
    //        M_Additions[] adding = new M_Additions[1];
    //        adding[0] = new M_Additions();
    //        adding[0].target = "public void StartEmitters()";
    //        adding[0].add = "public virtual void StartEmitters()";
    //        adding[0].type = M_FileAddtionType.Replace;

    //        newlines.AddRange(adding);
    //        ModifyFile(_shooterWeapon.path, newlines);
    //    }
    //}
    #endregion

    #region Modification Logic
    M_FileData FileExists(string filename, string directory)
    {
        M_FileData data = new M_FileData();
        DirectoryInfo dir = new DirectoryInfo(directory);
        foreach (FileInfo file in dir.GetFiles("*.cs"))
        {
            if (file.Name == filename)
            {
                data.exists = true;
                data.path = file.ToString();
                return data;
            }
        }
        foreach(string subDir in Directory.GetDirectories(directory))
        {
            data = FileExists(filename, subDir);
            if (data.exists == true)
            {
                return data;
            }
        }
        data.exists = false;
        data.path = "";
        return data;
    }
    void ModifyFile(string filepath, List<M_Additions> additions)
    {
        List<string> lines = new List<string>(System.IO.File.ReadAllLines(filepath));
        List<string> modified = new List<string>();
        bool added = false;

        for (int i=0; i < lines.Count; i++)
        {
            foreach (M_Additions item in additions)
            {
                if (lines[i].Trim().Equals(item.target))
                {
                    string space;
                    switch (item.type)
                    {
                        case M_FileAddtionType.NewLine:
                            added = true;
                            modified.Add(lines[i]);
                            if (!lines[i + 1].Trim().Equals(item.add))              //prevent this code from adding the same line in twice
                            {
                                space = lines[i].Split(item.target[0])[0];          //Get spaces
                                modified.Add(space + item.add);
                            }
                            break;
                        case M_FileAddtionType.Replace:
                            added = true;
                            if (!lines[i].Trim().Equals("//"+item.target))         //prevent this code from adding the same line in twice
                            {
                                space = lines[i].Split(item.target[0])[0];         //Get spaces
                                modified.Add(space+"//"+lines[i].Trim());          //Comment out the target line
                            }
                            else
                            {
                                modified.Add(lines[i]);
                            }
                            if (!lines[i+1].Trim().Equals(item.add))               //prevent this code from adding the same line in twice
                            {
                                space = lines[i].Split(item.target[0])[0];         //Get spaces
                                modified.Add(space + item.add);                    //Add new line
                            }
                            break;
                        case M_FileAddtionType.InsertLine:
                            _index = i + 1;
                            if (lines[_index].Trim() == "")
                                _index += 1;
                            if (lines[_index].Trim().Equals(item.nextline))
                            {
                                added = true;
                                space = lines[i].Split(item.target[0])[0];         //Get spaces
                                modified.Add(lines[i]);
                                modified.Add(space+item.add);
                            }
                            break;
                    }
                }
            }
            if (added == false)
            {
                modified.Add(lines[i]);
            }
            else
            {
                added = false;
            }
            
        }
        
        using (StreamWriter writer = new StreamWriter(filepath, false))
        {
            foreach (string line in modified)
            {
                writer.WriteLine(line);
            }
        }
    }
    #endregion
}
