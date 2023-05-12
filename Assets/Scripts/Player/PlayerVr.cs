using Photon.Pun;

using UnityEngine;

public class PlayerVr : PlayerObjBase
{
    public MeshRenderer[] Meshes;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        gamePlayer.Control = GamePlayer.ControlType.Vr;
    }

    //public override void SetMaterial(Material material)
    //{
    //    foreach (MeshRenderer mesh in Meshes)
    //    {
    //        mesh.material = material;
    //    }
    //}
}
