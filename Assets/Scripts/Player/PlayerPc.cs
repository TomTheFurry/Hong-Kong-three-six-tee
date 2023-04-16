using Photon.Pun;

using UnityEngine;

public class PlayerPc : PlayerObjBase
{
    public MeshRenderer[] Meshes;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        gamePlayer.Control = GamePlayer.ControlType.Pc;
    }

    //public override void SetMaterial(Material material)
    //{
    //    foreach (MeshRenderer mesh in Meshes)
    //    {
    //        mesh.material = material;
    //    }
    //}
}
