using System.Collections;
using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

public class PlayerPc : PlayerObjBase
{
    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        gamePlayer.Control = GamePlayer.ControlType.Pc;
    }
}
