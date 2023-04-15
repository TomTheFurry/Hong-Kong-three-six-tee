using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventMoney", menuName = "ChanceEvent/Money")]
public class ChanceEventMoney : ChanceEvent
{
    public float Money;
    public override void OnCreate(ChanceEventState r)
    {
        r.Player.Funds += Money;
    }
}
