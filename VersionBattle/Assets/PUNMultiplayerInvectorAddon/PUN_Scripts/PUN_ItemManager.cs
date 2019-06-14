using Invector.vItemManager;
using Invector.vShooter;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class PUN_ItemManager : MonoBehaviour {

    [SerializeField] private vItemListData allItems = null;
    public enum EquipSide { Left, Right }
    public enum WeaponType { shooter, melee, bow, shield }

    private List<string> customHandlers = new List<string>();

    public GameObject createItem(string itemName, EquipSide side, GameObject calledFrom)
    {
        itemName = itemName.Replace("(Clone)", "").Trim();
        if (itemName[0] == 'v')
        {
            itemName = itemName.Remove(0, 1);
        }
        GameObject newitem = null;
        string temp = "";
        foreach (var item in allItems.items)
        {
            temp = item.name;
            if (temp[0] == 'v')
            {
                temp = temp.Remove(0, 1);
            }
            if (temp == itemName)
            {
                newitem = Instantiate(item.originalObject, this.transform.position, this.transform.rotation);
                if (item.customHandler != "")
                {
                    if (!customHandlers.Contains(item.customHandler))
                    {
                        customHandlers.Add(item.customHandler);
                    }
                }
                switch (item.type)
                {
                    case vItemType.Shooter:
                        SetWeapon(newitem, calledFrom, side, WeaponType.shooter, item.customHandler);
                        break;
                    case vItemType.MeleeWeapon:
                        SetWeapon(newitem, calledFrom, side, WeaponType.melee, item.customHandler);
                        break;
                }
            }
        }

        return newitem;
    }

    public void DestroyWeapon(GameObject owner, string weaponName, EquipSide side)
    {
        List<Transform> handlers = GetHandlers(owner,side);
        List<Transform> weapons = new List<Transform>();
        foreach (Transform handler in handlers)
        {
            weapons.Clear();
            weapons = FindAllWithName(handler, weaponName, weapons);
            foreach(Transform weapon in weapons)
            {
                Destroy(weapon.gameObject);
            }
        }
    }

    void SetWeapon(GameObject weapon, GameObject owner, EquipSide side, WeaponType type, string customHandler)
    {
        //vShooterWeapon shooterWeapon = weapon.GetComponent<vShooterWeapon>();
        //PUN_ShooterManager manager = owner.GetComponent<PUN_ShooterManager>();
        Transform handler = GetHandler(owner.transform, side, type, customHandler);
        weapon.transform.position = handler.position;
        weapon.transform.rotation = handler.rotation;
        weapon.transform.SetParent(handler);
    }

    List<Transform> GetHandlers(GameObject owner, EquipSide side)
    {
        List<Transform> handlers = new List<Transform>();
        handlers.Add(GetHandler(owner.transform, side, WeaponType.melee,""));
        handlers.Add(GetHandler(owner.transform, side, WeaponType.shooter,""));
        foreach (string handler in customHandlers)
        {
            Transform meleeHandler = GetHandler(owner.transform, side, WeaponType.melee, handler);
            if (meleeHandler != null)
            {
                handlers.Add(meleeHandler);
            }
            Transform shooterHandler = GetHandler(owner.transform, side, WeaponType.shooter, handler);
            if (shooterHandler != null)
            {
                handlers.Add(shooterHandler);
            }
        }
        return handlers;
    }

    Transform GetHandler(Transform owner, EquipSide side, WeaponType type, string customHandler)
    {
        string foundHandler = "";
        string searchParent = "";
        switch (side)
        {
            case EquipSide.Left:
                searchParent = "LeftHandlers";
                break;
            case EquipSide.Right:
                searchParent = "RightHandlers";
                break;
        }
        if (customHandler == "")
        {
            switch (type)
            {
                case WeaponType.melee:
                    foundHandler = "meleeHandler";
                    break;
                case WeaponType.shooter:
                    foundHandler = "defaultHandler";
                    break;
            }
        }
        else
        {
            foundHandler = customHandler;
        }

        Transform rootHandler = FindWithName(owner, searchParent);
        Transform handler = FindWithName(rootHandler, foundHandler);

        return handler;
    }

    Transform FindWithName(Transform root, string Name)
    {
        Transform retVal = null;
        if (root.name == Name)
        {
            retVal = root;
        }
        foreach (Transform child in root)
        {
            if (child.gameObject.name == Name)
            {
                retVal = child;
            }
            if (retVal == null)
            {
                retVal = FindWithName(child, Name);
                if (retVal != null)
                {
                    break;
                }
            }
        }

        return retVal;
    }

    List<Transform> FindAllWithName(Transform root, string Name, List<Transform> retVal)
    {
        if (root.name == Name)
        {
            retVal.Add(root);
        }
        foreach (Transform child in root)
        {
            if (child.gameObject.name == Name)
            {
                retVal.Add(child);
            }
            retVal = FindAllWithName(child, Name, retVal);
        }

        return retVal;
    }
}
