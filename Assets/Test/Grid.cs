using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public int type;
    private int playerIndex = -1;
    public int architectureIndex;
    private GameObject architecture;
    private Action<Player, bool> actionFn;
    Coroutine coroutine;

    private void Awake() {
        float scale = Control.instance.setting.gridScale;
        Vector3 newScale = new Vector3(scale, scale, scale);
        transform.localScale = newScale;
    }

    private void Start() {
        //updatePlayer(Random.Range(0, Control.playerNumber));
        GameObject architecture = Control.instance.setting.architectures.getRandArchitecture(type);
        this.architecture = Instantiate(architecture, transform);

        switch (type) {
            case 1:
                actionFn = actionType1;
                break;
            case 2:
                actionFn = actionType2;
                updateColor(Color.yellow);
                break;
        }
    }

    public void updatePlayer(int index) {
        if (index < 0 || index >= Control.playerNumber)
            return;

        playerIndex = index;
        Color color = Control.instance.setting.playerColors[index];
        ColorHSV colorHSV =  (ColorHSV)color;
        colorHSV.s *= 0.35f;
        updateColor((Color)colorHSV);
    }

    public void updateColor(Color color) {
        foreach (Renderer rend in gameObject.GetComponentsInChildren<Renderer>(true)) {
            rend.material.color = color;
        }
    }

    public void action(Player player, bool byEffect) {
        actionFn(player, byEffect);
    }

    // Normal House
    public void actionType1(Player player, bool byEffect) {
        if (byEffect)
            return;
        if (playerIndex < 0 || playerIndex >= Control.playerNumber) {
            updatePlayer(player.index);
        }
    }

    // Draw Card
    public void actionType2(Player player, bool byEffect) {
        if (byEffect)
            return;
        GlobalSetting setting =  Control.instance.setting;
        int cardIndex = UnityEngine.Random.Range(0, setting.cards.Length);
        GlobalSetting.Card card = setting.cards[cardIndex];
        Control.instance.showCard(card);

        coroutine = StartCoroutine(afterActionType2(card, player));
    }

    public IEnumerator afterActionType2(GlobalSetting.Card card, Player player) {
        do {
            //if (Input.GetKey(KeyCode.W)) {
            if (Input.GetMouseButton(0)) {
                // move to forward
                if (card.MoveForward.target) {
                    Control.instance.moveForward(player, card.MoveForward.step, true);
                }
                if (card.MoveBack.target) {
                    Control.instance.moveBack(player, card.MoveBack.step, true);
                }
                Control.instance.hideCard();
                StopCoroutine(coroutine);
            }
            yield return new WaitForEndOfFrame();
        } while (true);
    }
}
