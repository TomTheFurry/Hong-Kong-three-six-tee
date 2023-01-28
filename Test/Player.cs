using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player2 : MonoBehaviour
{
    public int index;
    public int position;

    private void Awake() {

    }

    private void Start() {
        Color[] colors = Control.instance.setting.playerColors;
        Renderer rend = GetComponent<Renderer>();
        if (index < colors.Length)
            rend.material.color = colors[index];
        else
            rend.material.color = new Color(0, 0, 0, 1);
    }
}
