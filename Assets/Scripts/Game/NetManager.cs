using UnityEngine;

public class NetManager : MonoBehaviour
{
    public static NetManager Instance;

    void Start() { Instance = this; }
}
