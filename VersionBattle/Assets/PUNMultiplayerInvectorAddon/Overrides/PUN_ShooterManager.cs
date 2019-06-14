using Invector.vShooter;
using Photon.Pun;
using System.Collections;
using UnityEngine;

public class PUN_ShooterManager : vShooterManager
{
    public override void ReloadWeapon()
    {
        var weapon = rWeapon ? rWeapon : lWeapon;

        if (!weapon || !weapon.gameObject.activeInHierarchy) return;
        UpdateTotalAmmo();
        bool primaryWeaponAnim = false;

        if (weapon.ammoCount < weapon.clipSize && (weapon.isInfinityAmmo || WeaponHasAmmo()) && !weapon.autoReload)
        {
            onStartReloadWeapon.Invoke(weapon);

            if (GetComponent<Animator>())
            {
                GetComponent<Animator>().SetInteger("ReloadID", GetReloadID());
                GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "Reload");
            }
            if (CurrentWeapon && CurrentWeapon.gameObject.activeInHierarchy) StartCoroutine(AddAmmoToWeapon(CurrentWeapon, CurrentWeapon.reloadTime));
            primaryWeaponAnim = true;
        }
        if (weapon.secundaryWeapon && weapon.secundaryWeapon.ammoCount >= weapon.secundaryWeapon.clipSize && (weapon.secundaryWeapon.isInfinityAmmo || WeaponHasAmmo(true)) && !weapon.secundaryWeapon.autoReload)
        {
            if (!primaryWeaponAnim)
            {
                if (GetComponent<Animator>())
                {
                    primaryWeaponAnim = true;
                    GetComponent<Animator>().SetInteger("ReloadID", weapon.secundaryWeapon.reloadID);
                    GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "Reload");
                }
            }
            StartCoroutine(AddAmmoToWeapon(CurrentWeapon.secundaryWeapon, primaryWeaponAnim ? CurrentWeapon.reloadTime : CurrentWeapon.secundaryWeapon.reloadTime, !primaryWeaponAnim));
        }

    }

    protected override IEnumerator Recoil(float horizontal, float up)
    {
        yield return new WaitForSeconds(0.02f);
        if (GetComponent<Animator>() && GetComponent<PhotonView>().IsMine == true)
        {
            GetComponent<PhotonView>().RPC("SetTrigger", RpcTarget.All, "Shoot");
        }
        if (tpCamera != null) tpCamera.RotateCamera(horizontal, up);
    }

    public override void SetLeftWeapon(GameObject weapon)
    {
        if (weapon != null)
        {
            base.SetLeftWeapon(weapon);
            if (gameObject.GetComponent<PhotonView>().IsMine == true)
            {
                gameObject.GetComponent<PhotonView>().RPC("SetLeftWeapon", RpcTarget.OthersBuffered, weapon.name);
            }
        }
    }
    public override void SetRightWeapon(GameObject weapon)
    {
        if (weapon != null)
        {
            base.SetRightWeapon(weapon);
            if (gameObject.GetComponent<PhotonView>().IsMine == true)
            {
                gameObject.GetComponent<PhotonView>().RPC("SetRightWeapon", RpcTarget.OthersBuffered, weapon.name);
            }
        }
    }

    public override void OnDestroyWeapon(GameObject otherGameObject)
    {
        base.OnDestroyWeapon(otherGameObject);
        if (otherGameObject != null)
        {
            if (gameObject.GetComponent<PhotonView>().IsMine == true)
            {
                GetComponent<PhotonView>().RPC("OnDestroyWeapon", RpcTarget.OthersBuffered, otherGameObject.name, PUN_ItemManager.EquipSide.Right);
            }
        }
    }
}
