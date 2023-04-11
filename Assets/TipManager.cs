using System.Collections;
using System.Collections.Generic;

using Photon.Pun;

using TMPro;

using UnityEngine;

public class TipManager : MonoBehaviour
{
    public static TipManager Instance;
    public TextMeshProUGUI tipText;

    private void Awake()
    {
        Instance = this;
    }

    private void SetTip(string tip)
    {
        if (tip.Length == 0)
        {
            tipText.gameObject.SetActive(false);
        }
        else
        {
            tipText.gameObject.SetActive(true);
            tipText.text = tip;
        }
    }

    public void Update()
    {
        Game g = Game.Instance;
        if (g.State is not StateRollOrder and not StateTurn)
        {
            SetTip("");
            return;
        }

        GamePlayer self = PhotonNetwork.LocalPlayer;
        string tip = "";

        if (g.State is StateRollOrder rollOrder)
        {
            tip = "Rolling for deciding starting orders.\n";
            List<string> waitFor = new();
            for (int idx = 0; idx < rollOrder.RollNumByIdx.Length; idx++)
            {
                int rollNum = rollOrder.RollNumByIdx[idx];
                var player = g.IdxToPlayer[idx];
                if (player == self)
                {
                    if (rollNum != 0)
                    {
                        tip += $"You rolled {rollNum}\n";
                    }
                }
                else
                {
                    if (rollNum == 0)
                    {
                        waitFor.Add(player.Name);
                    }
                    else
                    {
                        tip += $"{player.Name} rolled {rollNum}\n";
                    }
                }
            }

            if (rollOrder.RollNumByIdx[self.Idx] == 0)
            {
                tip += "\nPick up a dice and roll it!";
            }
            else if (waitFor.Count != 0)
            {
                tip += "\nWaiting for " + string.Join(", ", waitFor) + "...";
            }
        }
        else if (g.State is StateTurn turn)
        {
            GamePlayer player = turn.CurrentPlayer;
            bool turnIsYours = player == self;
            tip = "It's currently " + (turnIsYours ? "your" : player.Name + "'s") + " turn.\n";

            if (turn.ChildState is StateTurn.StatePlayerAction action)
            {
                if (turnIsYours)
                {
                    tip += "You can either:\n";
                    tip += "- Throw a dice to roll for your turn\n";
                    tip += "- Throw an Item to the table to use it";
                }
                else
                {
                    tip += "Waiting for " + player.Name + " to take their turn...";
                }
            }
            else if (turn.ChildState is StateTurn.StateTurnEffects effect)
            {
                int step = effect.TotalSteps;
                
                tip += (turnIsYours ? "You" : player.Name) + " now advance for " + step + " step" + (step == 1 ? "" : "s") + "!\n";

                if (effect.ChildState is StateTurn.StateTurnEffects.StateStepOnTile onTile)
                {
                    GameTile tile = turn.CurrentPlayer.Tile;
                    tip += (turnIsYours ? "You" : player.Name) + " are now on " + tile.Name + ".\n";
                }
            }
            else if (g.State is StateTurn.StateEndTurn endTurn)
            {
                tip = turnIsYours ? "Your turn ended" : "Turn ended for " + player.Name;
                if (endTurn.IsEndRound)
                {
                    tip += "Round " + (g.roundData.RoundIdx+1) + " ended";
                }
                
                if (g.roundData.PeekNextPlayer() == self)
                {
                    tip += "\nIt's now your turn!";
                }
                else
                {
                    tip += "\nNext is " + g.roundData.PeekNextPlayer().Name + "'s turn";
                }

            }
        }
        
        SetTip(tip);
    }
}
