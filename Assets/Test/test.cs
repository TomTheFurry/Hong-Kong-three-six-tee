using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    private Renderer renderer;

    private void Awake() {
        renderer = GetComponent<Renderer>();
    }

    void Start()
    {
        renderer.material.color = new Color(1,1,1,1);
    }
}
