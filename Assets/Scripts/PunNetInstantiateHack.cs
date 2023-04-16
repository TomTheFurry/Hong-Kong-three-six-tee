using System;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

namespace Assets.Scripts
{
    public static class PunNetInstantiateHack
    {

        public static GameObject SetupForLinkObj(GameObject localObj, bool asRoomObj, Action<int[]> rpcSender)
        {
            PhotonView[] photonViews;

            if (localObj.activeSelf)
            {
                Debug.LogWarning($"Trying to network-link an already active object [{localObj}] ! It should instead return an inactive object.");
                localObj.SetActive(false);
            }
            photonViews = localObj.GetPhotonViewsInChildren();
            if (photonViews.Length == 0)
            {
                Debug.LogError($"Trying to network-link object [{localObj}] without a PhotonView component!");
                throw new MissingComponentException("PhotonView");
            }
            int[] viewId = new int[photonViews.Length];
            for (int i = 0; i < photonViews.Length; i++)
            {
                // when this client instantiates a GO, it has to allocate viewIDs accordingly.
                // ROOM objects are created as actorNumber 0 (no matter which number this player has).
                viewId[i] = (asRoomObj) ? PhotonNetwork.AllocateViewID(0) : PhotonNetwork.AllocateViewID(PhotonNetwork.LocalPlayer.ActorNumber);
                var view = photonViews[i];

                view.ViewID = 0;
                view.sceneViewId = 0;
                view.isRuntimeInstantiated = true;
                view.InstantiationId = viewId[0];
                view.ViewID = viewId[i];    // with didAwake true and viewID == 0, this will also register the view
            }
            rpcSender(viewId);
            localObj.SetActive(true);
            return localObj;
        }

        public static GameObject RecieveLinkObj(Player caller, GameObject localObj, int[] viewId)
        {
            PhotonView[] photonViews;
            
            if (localObj.activeSelf)
            {
                Debug.LogWarning($"Trying to network-link an already active object [{localObj}] ! It should instead return an inactive object.");
                localObj.SetActive(false);
            }
            
            photonViews = localObj.GetPhotonViewsInChildren();
            
            if (photonViews.Length == 0)
            {
                Debug.LogError($"Trying to network-link object [{localObj}] without a PhotonView component!");
                throw new MissingComponentException("PhotonView");
            }

            for (int i = 0; i < photonViews.Length; i++)
            {
                var view = photonViews[i];

                view.ViewID = 0;
                view.sceneViewId = 0;
                view.isRuntimeInstantiated = true;
                view.InstantiationId = viewId[0];
                view.ViewID = viewId[i];    // with didAwake true and viewID == 0, this will also register the view
            }
            localObj.SetActive(true);
            return localObj;
        }

        public static GameObject LocalUnlinkObj(GameObject localObj)
        {
            foreach (PhotonView view in localObj.GetPhotonViewsInChildren())
            {
                view.ViewID = 0;
            }
            return localObj;
        }
    }
}