using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class HeldItem : GameItem
{
    public override bool IsUsable => false;

    public enum Type
    {
        ChildOctopus,
        OldOctopus,
        MakeYouGetMyChance,
        PoorsFunding,
        NoSickness,
        HalfCostLevelUp,
        NoPassbyFee
    }
    public Type HeldType;

    public override bool IsIllegal => HeldType == Type.PoorsFunding ? CurrentOwner!.Funds > 2000 : Illegal;

}
