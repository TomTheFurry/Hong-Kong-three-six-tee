using System.Collections;
using System.Collections.Generic;

using Assets.Scripts;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

public class LandOwnershipItem : ItemBase
{
    public override bool IsUsable => false;
    public override UseItemStateBase GetUseItemState(StateTurn.StatePlayerAction parent) => null;

    [SerializeReference]
    [HideInInspector]
    public OwnableTile Tile; // Auto-set by OwneableTile OnValidate()
}