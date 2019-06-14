using Invector.vShooter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PUN_ControlAimCanvas : vControlAimCanvas
{
    public override void SetWordPosition(Vector3 wordPosition, bool validPoint = true)
    {
        if (cc == null) return;
        base.SetWordPosition(wordPosition, validPoint);
    }
}
