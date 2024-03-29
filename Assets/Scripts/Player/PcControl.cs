using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerPc))]
[RequireComponent(typeof(Rigidbody))]
public class PcControl : MonoBehaviour
{
    private Rigidbody rb;
    private PlayerPc pc;
    public Transform grabOrigin;
    public Transform grabSource;
    public GrabInteractableBase grabbedObject => pc.gamePlayer.Holding.FirstOrDefault()?.GetComponent<GrabInteractableBase>();
    public new Camera camera;

    public InputActionReference MoveAction;
    public InputActionReference LookAction;
    public InputActionReference GrabAction;
    public InputActionReference HoldRotateAction;
    public InputActionReference HoldRotateResetAction;
    public InputActionReference HoldZoomAction;
    public InputActionReference JumpAction;

    public float moveForce = 200;
    public float moveTargetSpeed = 50;
    public float jumpForce = 50;
    public float mouseMoveSpeed = 1f;

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
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerPc>();
    }

    void Update()
    {
        var move = !ProcessKeyboardAction ? default : MoveAction.action.ReadValue<Vector2>();
        var look = !ProcessMouseAction ? default : LookAction.action.ReadValue<Vector2>();
        look *= mouseMoveSpeed;

        var relVel = transform.InverseTransformDirection(rb.velocity);
        relVel.y = 0;
        var targetVel = Vector3.Normalize(new Vector3(move.x, 0, move.y)) * moveTargetSpeed;
        targetVel.y = 0;
        float maxAccel = moveForce * Time.deltaTime;
        var accel = Vector3.ClampMagnitude(targetVel - relVel, maxAccel);
        rb.AddRelativeForce(accel, ForceMode.VelocityChange);

        if (ProcessKeyboardAction && JumpAction.action.triggered)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        if (ProcessKeyboardAction && grabbedObject != null && HoldZoomAction.action.ReadValue<Vector2>().y != 0f)
        {
            float val = HoldZoomAction.action.ReadValue<Vector2>().y;
            //Debug.Log($"Y: {val}");
            var loc = grabSource.localPosition;
            loc.z -= HoldZoomAction.action.ReadValue<Vector2>().y / 2000f;
            loc.z = Math.Clamp(loc.z, 0.5f, 2f);
            grabSource.localPosition = loc;
        }

        if (ProcessKeyboardAction && grabbedObject != null && HoldRotateResetAction.action.triggered)
        {
            grabHoldRotationQ = Quaternion.identity;
            grabOrigin.localEulerAngles = new Vector3(camera.transform.localEulerAngles.x, 0, 0);
            grabSource.localRotation *= grabHoldRotationQ;
        }

        if (ProcessKeyboardAction && grabbedObject != null && HoldRotateAction.action.ReadValue<float>() != 0f)
        {
            //grabHoldRotation.x = Math.Clamp(grabHoldRotation.x, -60, 60);
            //grabHoldRotation.y = Math.Clamp(grabHoldRotation.y, -15, 15);
            //Debug.Log(grabHoldRotation);
            Quaternion newQ = Quaternion.Euler(look.y, -look.x, 0);
            grabHoldRotationQ = newQ * grabHoldRotationQ;
            grabSource.localRotation = grabHoldRotationQ;
        }
        else
        {
            //grabHoldRotation = Vector2.zero;
            rb.transform.rotation *= Quaternion.Euler(0, look.x, 0);
            float pitch = camera.transform.localEulerAngles.x;
            pitch = (pitch + 180) % 360 - 180;
            pitch = Mathf.Clamp(pitch - look.y, -90, 90);
            camera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
            grabSource.localRotation = grabHoldRotationQ;
        }
        
        GameTile hovered = null;
        GamePlayer playerHovered = null;
        PlayerUiIcon uiIconHovered = null;
        {
            float sphere = grabSphere * 2f;
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            if (Physics.SphereCast(
                    ray, sphere, out var hit, grabRange, LayerMask.GetMask("Tile", "Piece", "3D_UI"),
                    QueryTriggerInteraction.Ignore
                ))
            {
                var tile = hit.collider.transform.GetComponentInParent<GameTile>();
                var player = hit.collider.transform.GetComponentInParent<Piece>()?.Owner;
                var uiIcon = hit.collider.transform.GetComponentInParent<PlayerUiIcon>();
                if (tile != null)
                {
                    hovered = tile;
                    TileInteractor.Instance.OnHover(hovered, camera.transform);
                }
                if (player != null)
                {
                    playerHovered = player;
                    PlayerInteractor.Instance.OnHover(playerHovered);
                }
                if (uiIcon != null)
                {
                    uiIconHovered = uiIcon;
                    uiIconHovered.OnHover();
                }
            }
        }

        if (ProcessMouseAction && GrabAction.action.triggered)
        {
            if (grabbedObject != null)
            {
                grabbedObject.ReleaseObject();
            }
            else if (hovered != null)
            {
                TileInteractor.Instance.OnClick(hovered);
            }
            else if (playerHovered != null)
            {
                PlayerInteractor.Instance.OnClick(playerHovered);
            }
            else if (uiIconHovered != null)
            {
                uiIconHovered.OnClick();
            }
            else
            {


                // Do raycast
                for (int i = 1; i <= grabSphereTime; i++)
                {
                    float sphereRadius = grabSphere * MathF.Pow(grabSphereMul, i);
                    var ray = new Ray(camera.transform.position, camera.transform.forward);
                    if (Physics.SphereCast(ray, sphereRadius, out var hit, grabRange, LayerMask.GetMask("Item"), QueryTriggerInteraction.Ignore))
                    {
                        if (hit.rigidbody != null && hit.rigidbody.TryGetComponent<GrabInteractableBase>(out var grab))
                        {
                            grab.TryGrabObject(grabSource, () =>
                            {
                                Debug.Log("Grabbed");
                            });
                            break;
                        }
                    }
                }
            }
        }
    }

    public void Teleport(Vector3 pos)
    {
        rb.transform.position = pos;
    }

    public Transform GetTransform() => rb.transform;
}