using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Board))]
[DefaultExecutionOrder(-1)]
public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance;

    private bool allowSelf;
    private GamePlayer selfPlayer;
    private TaskCompletionSource<GamePlayer> SelectPlayerTcs; // todo
    public bool IsRequireSelect => SelectPlayerTcs != null;

    private bool IsHovered = false;
    public GamePlayer PlayerHovered { get; private set; }

    void Start()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    public void Update()
    {
        if (!IsHovered) PlayerHovered = null;
        IsHovered = false;
    }

    public void OnClick(GamePlayer player)
    {
        if (SelectPlayerTcs != null && (!allowSelf || player != selfPlayer))
        {
            SelectPlayerTcs.SetResult(player);
            SelectPlayerTcs = null;
            allowSelf = false;
            selfPlayer = null;
        }
    }

    public GamePlayer[] GetPredicatePlayer()
    {
        GamePlayer[] players = Game.Instance.IdxToPlayer;
        return (GamePlayer[])players.Where(p => !allowSelf || p != selfPlayer);
    }

    public Task<GamePlayer> ChoosePlayer(bool allowSelf)
    {
        this.allowSelf = allowSelf;
        if (allowSelf) selfPlayer = (Game.Instance.State as StateTurn).CurrentPlayer;
        Assert.IsNull(SelectPlayerTcs);
        SelectPlayerTcs = new TaskCompletionSource<GamePlayer>();
        return SelectPlayerTcs.Task;

    }

    public void OnHover(GamePlayer player)
    {
        IsHovered = true;
        PlayerHovered = player;
    }
}
