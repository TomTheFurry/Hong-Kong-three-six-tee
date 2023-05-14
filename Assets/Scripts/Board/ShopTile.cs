using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Photon.Pun;
using TMPro;
using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;

public class ShopTile : OwnableTile
{
    private int[] itemsList;
    private TaskCompletionSource<int[]> shopItems;
    private TaskCompletionSource<int> buyItem;

    public double BuyItemCost => (Level == 0 ? Price * 0.1 * feeMultiplier : StepOnPrice) * 0.5;

    [PunRPC]
    public void ServerDrawedShopItem(int[] items)
    {
        Debug.Log("ServerDrawedShopItem: " + items.IntToString());
        Assert.IsTrue(items.Length == 3);
        Assert.IsNotNull(shopItems);
        shopItems.SetResult(items);
    }

    [PunRPC]
    public void BoughtItem(int choice, PhotonMessageInfo info) {
        if (choice == -1) {
            buyItem.SetResult(-1);
        }
        else {
            Assert.IsTrue(itemsList[choice] != -1);
            Debug.Log("BoughtItem: " + ItemTemplateDefiner.Instance.ItemTemplate[itemsList[choice]].Name);
            int id = itemsList[choice];
            itemsList[choice] = -1;
            ItemTemplateDefiner.Instance.ServerInstantiateItem(id, info.Sender);
        }
    }

    private async Task OnStepBuyItems(GamePlayer player)
    {
        buyItem = new TaskCompletionSource<int>();
        if (player.PunConnection.IsLocal) {
            int itemRemaining = 3;

            while (itemRemaining > 0) {
                // I am the active player
                List<(GameItem, double, int)> options = new List<(GameItem, double, int)>();
                for (int i = 0; i < 3; i++)
                {
                    if (itemsList[i] == -1) continue;
                    options.Add((ItemTemplateDefiner.Instance.ItemTemplate[itemsList[i]], BuyItemCost, i));
                }

                PlayerUiShop playerUiShop = ((StateTurn)Game.Instance.State).CurrentPlayer.PcManager.GetShopUi();
                int option = await playerUiShop.ShowShop(options);
                if (option == -1) break;

                itemRemaining--;
                int choice = option;
                Debug.Log($"Shop choice: {choice}");
                Assert.IsFalse(choice < 0 || choice > 2);
                Assert.IsTrue(itemsList[choice] != -1);

                photonView.RPC(nameof(BoughtItem), RpcTarget.All, choice);
                // TODO: Show UI
                //List<(KeyCode, string)> opts = new List<(KeyCode, string)>();
                //opts.Add((KeyCode.Return, "Continue"));

                //for (int i = 0; i < 3; i++) {
                //    if (itemsList[i] == -1) continue;
                //    opts.Add(((KeyCode) ((int) KeyCode.Alpha1 + i), "Buy " + ItemTemplateDefiner.Instance.ItemTemplate[itemsList[i]].Name + " for " + BuyItemCost));
                //}

                //KeyCode option = await DebugKeybind.Instance.ChooseActionTemp(opts);
                //if (option is KeyCode.Return) break;

                //itemRemaining--;
                //int choice = (int) option - (int) KeyCode.Alpha1;
                //Assert.IsFalse(choice < 0 || choice > 2);
                //Assert.IsTrue(itemsList[choice] != -1);

                //photonView.RPC(nameof(BoughtItem), RpcTarget.All, choice);
            }
            photonView.RPC(nameof(BoughtItem), RpcTarget.All, -1);
            ((StateTurn)Game.Instance.State).CurrentPlayer.PcManager.HideUi();
        }
        await buyItem.Task;
        buyItem = null;
    }

    protected override async Task OnStep(GamePlayer player)
    {
        Debug.Log("OnStep!");
        itemsList = null;
        if (!PhotonNetwork.IsMasterClient) {
            shopItems = new TaskCompletionSource<int[]>();
            itemsList = await shopItems.Task;
        }
        else {
            itemsList = ItemTemplateDefiner.Instance.ServerDrawShopItems();
            photonView.RPC(nameof(ServerDrawedShopItem), RpcTarget.Others, itemsList);
        }
        
        Debug.Log("Drawn shop item " + itemsList.IntToString());
        // sub funds
        if (Owner != null && Owner != player)
        {
            Debug.Log("Pay fees");
            await OnStepPayFees(player);
        }
        else
        {
            Debug.Log("Await for player tile action...");
            await OnStepCanBuyOrUpgrade(player);
        }
        
        Debug.Log("Await for player shop action...");
        await OnStepBuyItems(player);
        itemsList = null;
    }
}
