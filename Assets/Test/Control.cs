using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour
{
    // setting
    public const int playerNumber = 4;
    private const int formDistance = 3;
    private const int mapSize = 10 * 4;
    private const int gridSize = 10;

    // set var
    public GameObject playerPrefab;
    public GameObject gridPrefeb;
    public GlobalSetting setting;

    public static Control instance;

    public List<GameObject> players;
    public List<GameObject> map;



    private void Awake() {
        if (!instance)
            instance = this;
        else
            Debug.Log("instance exist");

        // spawn player
        players = new List<GameObject>();
        Color[] colors = setting.playerColors;
        for (int i = 0; i < playerNumber; ++i) {
            GameObject obj = Instantiate(playerPrefab, new Vector3(0 - (i - playerNumber / 2) * formDistance, 0, 0), Quaternion.identity);
            players.Add(obj);

            obj.name = "Player" + (i + 1);
            Renderer rend = obj.GetComponent<Renderer>();
            if (i < colors.Length)
                rend.material.color = colors[i];
            else
                rend.material.color = new Color(0,0,0,1);
        }

        // spawn map
        float gridScale = setting.gridScale * gridSize;
        int mapLineSize = mapSize / 4;
        Vector2 startPos = new Vector2(-Mathf.FloorToInt(mapLineSize / 2), -Mathf.FloorToInt(mapLineSize / 2));
        if (mapLineSize % 2 == 1) {
            startPos += new Vector2(0.5f, 0.5f);
        }
        startPos *= gridScale;

        int gridCount = 1;
        for (int i = 0; i < mapSize; ++i) {
            GameObject obj = Instantiate(gridPrefeb, transform);
            map.Add(obj);

            obj.name = "Grid" + gridCount++;
            
            if (i < mapLineSize) {
                obj.transform.localPosition = new Vector3(startPos.x + gridScale, 0, startPos.y);
            }
            else if (i < mapLineSize * 2) {
                obj.transform.localPosition = new Vector3(startPos.x, 0, startPos.y + gridScale);
            }
            else if (i < mapLineSize * 3) {
                obj.transform.localPosition = new Vector3(startPos.x - gridScale, 0, startPos.y);
            }
            else if (i < mapLineSize * 4) {
                obj.transform.localPosition = new Vector3(startPos.x, 0, startPos.y - gridScale);
            }

            startPos.x = obj.transform.localPosition.x;
            startPos.y = obj.transform.localPosition.z;
        }
    }
}
