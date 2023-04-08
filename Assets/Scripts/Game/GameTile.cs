using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEditor;

using UnityEngine;

public abstract class GameTile : MonoBehaviour
{
    // Config this
    [SerializeReference]
    public GameTile NextTile = null;

    // Auto config
    //[HideInInspector]
    [SerializeReference]
    public GameTile PrevTile = null;

    private bool maybeDup = true;

    private void Awake()
    {
        
    }
    
    public abstract bool NeedActionOnEnterTile(GamePlayer player);
    public abstract bool NeedActionOnExitTile(GamePlayer player);
    public abstract Task ActionsOnStop(GamePlayer player);

    #region UNITY_EDITOR
#if UNITY_EDITOR

    public void RecheckLinks(bool overrideOthers = false)
    {
        if (NextTile != null && NextTile.PrevTile != this)
        {
            var old = NextTile.PrevTile;
            if (old != null && old.NextTile == NextTile) // Both 'old' and me try to have the same NextTile
            {
                if (overrideOthers)
                {
                    old.NextTile = null;
                    NextTile.PrevTile = this;
                }
                else
                {
                    NextTile = null;
                }
            }
            else
            {
                NextTile.PrevTile = this;
            }
            
        }
    }
    
    public void SetNextTile(GameTile nextTile)
    {
        if (NextTile != null && NextTile.PrevTile == this)
        {
            NextTile.PrevTile = null;
        }
        NextTile = nextTile;
        RecheckLinks(true);
    }

    private bool CheckDupAction()
    {
        if (!maybeDup) return false;
        maybeDup = false;

        // Check for dup at in between: Insert after
        if (NextTile != null && PrevTile != null)
        {
            var sideGuy = PrevTile.NextTile;
            if (sideGuy == this) return false;
            if (sideGuy != NextTile.PrevTile) return true;
            if (sideGuy.NextTile != NextTile || sideGuy.PrevTile != PrevTile) return true;
            sideGuy.NextTile = this;
            PrevTile = sideGuy;
            NextTile.PrevTile = this;
        } // Check for dup at end: Append after
        else if (NextTile == null && PrevTile != null)
        {
            var sideGuy = PrevTile.NextTile;
            if (sideGuy == this) return false;
            if (sideGuy == null)  return true;
            if (sideGuy.NextTile != null || sideGuy.PrevTile != PrevTile)  return true;
            sideGuy.NextTile = this;
            PrevTile = sideGuy;
        } // Check for dup at begin: Append before
        else if (NextTile != null && PrevTile == null)
        {
            var sideGuy = NextTile.PrevTile;
            if (sideGuy == this) return false;
            if (sideGuy == null)  return true;
            if (sideGuy.NextTile != NextTile || sideGuy.PrevTile != null)  return true;
            sideGuy.PrevTile = this;
            NextTile = sideGuy;
        }
        return false;
    }

    public void OnValidate()
    {
        // Check if is dup
        if (CheckDupAction())
        {
            PrevTile = null;
            NextTile = null;
        }
        else
            RecheckLinks(true);

        foreach (var tile in FindObjectsOfType<GameTile>())
        {
            tile.RecheckLinks();
        }
    }

    private static void GizmoDraw(GameTile from, GameTile to)
    {
        // draw arrow
        Gizmos.DrawLine(from.transform.position, to.transform.position);
        // draw arrow head
        Vector3 dir = to.transform.position - from.transform.position;
        Vector3 left = Quaternion.Euler(0, 30, 0) * dir * 0.2f;
        Vector3 right = Quaternion.Euler(0, -30, 0) * dir * 0.2f;
        Gizmos.DrawLine(to.transform.position, to.transform.position - left);
        Gizmos.DrawLine(to.transform.position, to.transform.position - right);
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
        if (PrevTile != null)
        {
            Gizmos.color = Color.red;
            GizmoDraw(PrevTile, this);
        }
    }

#endif
    #endregion
}

public abstract class OwnableTile : GameTile
{

}

public abstract class EffectsTile : GameTile
{
}

public class ChanceTile : EffectsTile
{
}

public class PoliceTile : EffectsTile
{
}

public class TransportTile : OwnableTile
{
}