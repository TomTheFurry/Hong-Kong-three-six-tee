using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.XR;

public static class XrManager
{
    public static bool IsInitialized { get; private set; } = false;

    public static InputDevice GetDeviceOrThrow(this XRInputSubsystem subsystem)
    {
        var devices = new List<InputDevice>();
        
        if (subsystem.TryGetInputDevices(devices) || devices.Count == 0)
        {
            throw new System.Exception("No devices found for subsystem " + subsystem.SubsystemDescriptor.id);
        }
        return devices[0];
    }

    public static bool TryGetDevice(this XRInputSubsystem subsystem, out InputDevice device)
    {
        var devices = new List<InputDevice>();
        if (subsystem.TryGetInputDevices(devices) || devices.Count == 0)
        {
            device = default;
            return false;
        }
        device = devices[0];
        return true;
    }

    public static bool HasXRDevices { get; private set; }
    
    public static XRDisplaySubsystem? DisplaySubsystem { get; private set; }

    public static List<XRInputSubsystem> InputSubsystems { get; private set; } = new List<XRInputSubsystem>();

    public static XRInputSubsystem? LeftHandInput { get; private set; }

    public static XRInputSubsystem? RightHandInput { get; private set; }

    public static event Action OnXRDevicesChanged = delegate { };

    private static void UpdateOnce()
    {
        HasXRDevices = false;

        DisplaySubsystem = null;
        var subsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances(subsystems);

        if (subsystems.Count > 0)
        {
            Debug.Log($"Found {subsystems.Count} XR Display Subsystem(s): {string.Join(", ", subsystems)}");
            DisplaySubsystem = subsystems[0];
        }
        else
        {
            Debug.Log("No XR Display found");
        }

        HasXRDevices = DisplaySubsystem != null;
        IsInitialized = true;
    }

    public static void Update()
    {
        Debug.Log("Updating All XR Inputs...");
        InputSubsystems.Clear();
        if (HasXRDevices)
        {
            SubsystemManager.GetInstances(InputSubsystems);
            InputSubsystems = InputSubsystems.Where(s => s.TryGetDevice(out _)).ToList();
            if (InputSubsystems.Count > 0)
            {
                Debug.Log($"Found {InputSubsystems.Count} XR Input Subsystem(s): {string.Join(", ", InputSubsystems)}");
            }
            else
            {
                Debug.Log("No XR Input Subsystems found");
            }
        }

        LeftHandInput = null;
        RightHandInput = null;
        if (HasXRDevices)
        {
            foreach (var inputSubsystem in InputSubsystems)
            {
                if (inputSubsystem.GetDeviceOrThrow().characteristics.HasFlag(InputDeviceCharacteristics.Left))
                {
                    Debug.Log($"Found {(LeftHandInput == null ? "" : "(but will not use)")} Left Hand Input: {inputSubsystem.GetDeviceOrThrow().name}");
                    LeftHandInput ??= inputSubsystem;
                }
                else if (inputSubsystem.GetDeviceOrThrow().characteristics.HasFlag(InputDeviceCharacteristics.Right))
                {
                    Debug.Log($"Found {(RightHandInput == null ? "" : "(but will not use)")} Right Hand Input: {inputSubsystem.GetDeviceOrThrow().name}");
                    RightHandInput ??= inputSubsystem;
                }
                if (LeftHandInput != null && RightHandInput != null)
                {
                    break;
                }
            }
        }

        OnXRDevicesChanged.Invoke();
    }
    
    [RuntimeInitializeOnLoadMethod]
    public static void Init()
    {
        if (IsInitialized)
        {
            Debug.LogWarning("XRManager already initialized");
            return;
        }
        UpdateOnce();
        InputTracking.nodeAdded += _ =>
        {
            Debug.Log("XR Input Node Added.");
            Update();
        };
        InputTracking.nodeRemoved += _ =>
        {
            Debug.Log("XR Input Node Removed.");
            Update();
        };
        Update();
    }

}
