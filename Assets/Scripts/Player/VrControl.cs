using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerVr))]
public class VrControl : MonoBehaviour
{
    //private Rigidbody rb;
    private PlayerVr vr;
    public Transform grabOrigin;
    public Transform grabSource;
    public GrabInteractableBase grabbedObject => vr.gamePlayer.Holding.FirstOrDefault()?.GetComponent<GrabInteractableBase>();
    public new Camera camera;

    public InputActionReference MoveAction;

    public float moveTargetSpeed = 50;

    public float grabRange = 100;
    public float grabSphere = 0.1f;
    public float grabSphereMul = 1.7f;
    public int grabSphereTime = 10;
    private Quaternion grabHoldRotationQ = Quaternion.identity;
    private Vector2 grabHoldRotation;
    private bool ClubFlipped = false;

    public bool ProcessMouseAction = false;
    public bool ProcessKeyboardAction = false;

    void Start()
    {
        //rb = GetComponent<Rigidbody>();
        vr = GetComponent<PlayerVr>();
    }

    void Update()
    {
        var move = !ProcessKeyboardAction ? default : MoveAction.action.ReadValue<Vector2>() * moveTargetSpeed;
    }
}