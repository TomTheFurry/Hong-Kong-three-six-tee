using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public int type;
    private int playerIndex = -1;
    private GameObject architecture;

    private void Awake() {
        float scale = Control.instance.setting.gridScale;
        Vector3 newScale = new Vector3(scale, scale, scale);
        transform.localScale = newScale;

        GameObject[] architectures = Control.instance.setting.architectures;
        type = Random.Range(0, architectures.Length);
        architecture = Instantiate(architectures[type], transform);
    }

    private void Start() {
        //updatePlayer(Random.Range(0, Control.playerNumber));
    }

    public void updatePlayer(int index) {
        if (index < 0 || index >= Control.playerNumber)
            return;

        playerIndex = index;
        Color color = Control.instance.setting.playerColors[index];
        foreach (Renderer rend in gameObject.GetComponentsInChildren<Renderer>(true)) {;
            rend.material.color = color;
        }
    }
}
