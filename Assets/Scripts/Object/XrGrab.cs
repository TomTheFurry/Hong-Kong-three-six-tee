
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(GrabInteractableBase))]
public class XrGrab : XRGrabInteractable
{
    GrabInteractableBase gb;

    void Start()
    {
        gb = GetComponent<GrabInteractableBase>();
        selectEntered.AddListener(e => gb.TryGrabObject(e.interactorObject.transform, () => { }));
    }

    public override bool IsHoverableBy(IXRHoverInteractor interactor)
    {
        return gb.canGrab;
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return gb.canGrab;
    }
}
