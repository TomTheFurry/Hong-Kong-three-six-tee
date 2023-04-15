using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using UnityEditor;

using UnityEngine;
using UnityEngine.InputSystem;

public abstract class GameTile : MonoBehaviourPun
{
    public string Name;

    public int TileId = -1;
    public TileType Type;

    // Config this
    [SerializeReference]
    public GameTile NextTile = null;

    // Auto config
    [HideInInspector]
    [SerializeReference]
    public GameTile PrevTile = null;

    public virtual IEnumerator<GameTile> GetNextTiles()
    {
        yield return NextTile;
    }

    private bool maybeDup = true;

    public abstract bool NeedActionOnEnterTile(GamePlayer player);
    public abstract bool NeedActionOnExitTile(GamePlayer player);
    public abstract bool ActionsOnStop(GamePlayer player, StateTurn.StateTurnEffects.StateStepOnTile self, [NotNullWhen(true)] [CanBeNull] out Task t, [NotNullWhen(false)] [CanBeNull] out Task<GameState> state);

    #region UNITY_EDITOR
#if UNITY_EDITOR

    private void UpdateTileType()
    {
        if (Type == null)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                // set default color (use unity default color)
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            }
            return;
        }
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = Type.Color;
        }
    }

    public void OnValidate()
    {
        if (NextTile != null)
        {
            NextTile.PrevTile = this;
            EditorUtility.SetDirty(this);
        }
        // Check if is dup
        UpdateTileType();
    }

    private static void GizmoDraw(GameTile from, GameTile to)
    {
        Vector3 o = new Vector3(0, 0.5f, 0);
        // draw arrow
        Gizmos.DrawLine(from.transform.position + o, to.transform.position + o);
        // draw arrow head
        Vector3 dir = to.transform.position - from.transform.position;
        Vector3 left = Quaternion.Euler(0, 30, 0) * dir * 0.2f;
        Vector3 right = Quaternion.Euler(0, -30, 0) * dir * 0.2f;
        Gizmos.DrawLine(to.transform.position + o, to.transform.position + o - left);
        Gizmos.DrawLine(to.transform.position + o, to.transform.position + o - right);
    }

    public void OnDrawGizmos()
    {
        if (NextTile != null && !Selection.Contains(NextTile.gameObject) && !Selection.Contains(gameObject))
        {
            Gizmos.color = Color.white;
            GizmoDraw(this, NextTile);
        }

        if (NextTile == null || PrevTile == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.25f);
            
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (NextTile != null)
        {
            Gizmos.color = Color.green;
            GizmoDraw(this, NextTile);
        }
        /*if (PrevTile != null)
        {
            Gizmos.color = Color.red;
            GizmoDraw(PrevTile, this);
        }*/
    }

#endif
    #endregion
}