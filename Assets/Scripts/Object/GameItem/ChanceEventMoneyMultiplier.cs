using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventMoneyMultiplier", menuName = "ChanceEvent/MoneyMultiplier")]
public class ChanceEventMoneyMultiplier : ChanceEvent
{
    public float MoneyMul;

    public override void OnCreate(ChanceEventState r)
    {
        r.Player.Funds *= MoneyMul;
    }
}
