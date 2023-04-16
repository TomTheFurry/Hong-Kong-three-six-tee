using UnityEngine;

public class LandOwnershipItem : ItemBase
{
    public override bool IsUsable => false;
    public override UseItemStateBase GetUseItemState(StateTurn.StatePlayerAction parent) => null;

    [SerializeReference]
    [HideInInspector]
    public OwnableTile Tile; // Auto-set by OwneableTile OnValidate()
    private bool updated = false;

    void Update()
    {
        if (!updated && Tile != null && TryGetComponent<TileDataSetter>(out var setter))
        {
            updated = true;
            setter.Set(Tile);
        }
    }
}