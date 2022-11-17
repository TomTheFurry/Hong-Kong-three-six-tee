using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSetting : MonoBehaviour {
    public float gridScale = 0.5f;
    
    public GameObject grid;
    public Color[] playerColors;
    public Architectures architectures;
    public Card[] cards;

    [Serializable]
    public struct Architectures {
        /*
         * type 1: Normal House
         * type 2: Draw Card
         */
        public GameObject[] type1;
        public GameObject[] type2;

        public GameObject[] getArchitectures(int type) {
            switch (type) {
                default:
                case 1:
                    return type1;
                case 2:
                    return type2;
            }
        }

        public GameObject getArchitecture(int type, int index) {
            return getArchitectures(type)[index];
        }

        public GameObject getRandArchitecture(int type) {
            GameObject[] architectures = getArchitectures(type);
            if (architectures.Length > 0)
                return architectures[UnityEngine.Random.Range(0, architectures.Length)];
            else
                return Instantiate(new GameObject());
        }
    }

    [Serializable]
    public struct Card {
        [Serializable]
        public struct CardEffect {
            public bool target;
            public int step;
        }

        public string description;
        public CardEffect MoveForward;
        public CardEffect MoveBack;
    }

    private void Awake() {
        return;
        int i = cards.Length;
        int addCard = 10;
        Card[] temp = new Card[cards.Length + addCard];
        for (int j = 0; j < i; ++j) {
            temp[j] = cards[j];
        }
        while (i < temp.Length) {
            temp[i] = new Card() { description = "Card" + (i + 1) };
            ++i;
        }
        cards = temp;
    }
}
