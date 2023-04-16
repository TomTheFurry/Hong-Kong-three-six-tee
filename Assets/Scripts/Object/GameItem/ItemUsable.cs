using System.Collections;

using UnityEngine;

public class ItemUsable : ItemBase
{
    public override bool IsUsable => true;

    public enum Event
    {
        
    }




    public override UseItemStateBase GetUseItemState(StateTurn.StatePlayerAction parent) => null;



}