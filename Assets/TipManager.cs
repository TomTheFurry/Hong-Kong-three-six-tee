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

    private string ColorText<T>(T text, string color = "#fff")
    {
        return $"<color=#{color}>{text}</color>";
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
                        tip += $"You rolled {ColorText(rollNum, "0ff")}\n";
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
                        tip += $"{player.Name} rolled {ColorText(rollNum, "0ff")}\n";
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
            tip = "It's currently " + (turnIsYours ? ColorText("your", "0ff") : player.Name + "'s") + " turn.\n";

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
                
                tip += (turnIsYours ? "You" : player.Name) + " now advance for " + ColorText(step, "0ff") + " step" + (step == 1 ? "" : "s") + "!\n";

                if (effect.ChildState is StateTurn.StateTurnEffects.StateStepOnTile onTile)
                {
                    GameTile tile = turn.CurrentPlayer.Tile;
                    tip += (turnIsYours ? "You" : player.Name) + " are now on " + ColorText(tile.Name, "0ff") + ".\n";

                    if (tile is OwnableTile ot) 
                    {
                        if (turnIsYours) {
                            if (ot.Owner == null) {
                                tip += "You can buy this tile for " + ColorText(ot.Price, "0ff") + " dollars.\n";
                            }
                            else if (ot.Owner == player && ot.Level < 5) {
                                tip += "You can level up this tile to level " + (ot.Level + 1) + " for " + ColorText(ot.LevelUpPrice, "0ff") + " dollars.\n";
                            }
                            else if (ot.Owner != player && ot.Owner != null) {
                                tip += "'You paid " + (ot.StepOnPrice) + " dollars to " + ot.Owner.Name + " for rent.\n";
                            }

                            if (ot is ShopTile st) {
                                tip += "You can buy items from the shop.\n";
                            }
                            tip += "Press 'Enter' to continue...\n";
                        }
                        else {
                            if (ot.Owner != player && ot.Owner != null) {
                                bool isOwnedBySelf = ot.Owner == self;
                                tip += player.Name + " paid " + (ot.StepOnPrice) + " dollars to " +(isOwnedBySelf ? "you" : ot.Owner.Name) + " for rent.\n";
                            }
                            tip += "Waiting for " + player.Name + "'s actions...\n";
                        }
                    }
                    else if (tile is ChanceTile ct) {
                        if (turnIsYours) {
                            if (onTile.Child is ChanceEventState ce)
                            {
                                tip += "You drawed the card: " + ce.Card.Ev.Name + "\n";
                            }
                            else {
                                tip += "You can pick up a chance card.\n";
                                tip += "Press 'P' to draw cards. (TEMP)\n";
                            }
                        }
                        else {
                            tip += "Waiting for " + player.Name + "'s actions...\n";
                        }
                    }
                    else if (tile is SpecialTile st) {
                        if (st.TileType is SpecialTile.Type.RollStaw) {
                            if (st.DrawStawTask == null) {
                                tip += "Drawing a staw....\n";
                            }
                            else {
                                bool lucky = st.DrawStawTask.Task.Result;
                                tip += "You drawed a " + (lucky ? ColorText("lucky", "0ff") : ColorText("unlucky", "f00")) + " staw!\n";
                            }
                        }
                        else {
                            tip += "TODO\n";
                        }
                    }
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
