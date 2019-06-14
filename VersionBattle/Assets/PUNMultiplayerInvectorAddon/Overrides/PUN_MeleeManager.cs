using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector.vMelee;
using Photon.Pun;
using Invector.vItemManager;

public class PUN_MeleeManager : vMeleeManager
{
    bool send = true;

    public override void SetRightWeapon(GameObject weaponObject)
    {
        if (weaponObject)
        {
            base.SetRightWeapon(weaponObject);
            if (GetComponent<PhotonView>().IsMine == true)
            {
                send = true;
                foreach (Component comp in weaponObject.GetComponents<Component>())
                {
                    if (comp.ToString().Contains("vShooterEquipment"))
                    {
                        send = false;
                    }
                }
                if (send == true)
                {
                    gameObject.GetComponent<PhotonView>().RPC("SetRightWeapon", RpcTarget.OthersBuffered, weaponObject.name);
                }
            }
        }
    }
    public override void SetRightWeapon(vMeleeWeapon weapon)
    {
        if (weapon)
        {
            base.SetRightWeapon(weapon);
            if (GetComponent<PhotonView>().IsMine == true)
            {
                send = true;
                foreach (Component comp in weapon.GetComponents<Component>())
                {
                    if (comp.ToString().Contains("vShooterEquipment"))
                    {
                        send = false;
                    }
                }
                if (send == true)
                {
                    gameObject.GetComponent<PhotonView>().RPC("SetRightWeapon", RpcTarget.OthersBuffered, weapon.gameObject.name);
                }
            }
        }
    }

    public override void SetLeftWeapon(vMeleeWeapon weapon)
    {
        if (weapon)
        {
            base.SetLeftWeapon(weapon);
            if (GetComponent<PhotonView>().IsMine == true)
            {
                send = true;
                foreach (Component comp in weapon.GetComponents<Component>())
                {
                    if (comp.ToString().Contains("vShooterEquipment"))
                    {
                        send = false;
                    }
                }
                if (send == true)
                {
                    gameObject.GetComponent<PhotonView>().RPC("SetLeftWeapon", RpcTarget.OthersBuffered, weapon.gameObject.name);
                }
            }
        }
    }
    public override void SetLeftWeapon(GameObject weaponObject)
    {
        if (weaponObject)
        {
            base.SetLeftWeapon(weaponObject);
            if (GetComponent<PhotonView>().IsMine == true)
            {
                send = true;
                foreach (Component comp in weaponObject.GetComponents<Component>())
                {
                    if (comp.ToString().Contains("vShooterEquipment"))
                    {
                        send = false;
                    }
                }
                if (send == true)
                {
                    gameObject.GetComponent<PhotonView>().RPC("SetLeftWeapon", RpcTarget.OthersBuffered, weaponObject.name);
                }
            }
        }
    }
}
