using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Control : MonoBehaviour
{
    // setting
    public const int playerNumber = 4;
    private const int formDistance = 3;
    private const int mapSize = 10 * 4;
    private const int gridSize = 10;
    private const int randRange = 6;
    public Button btn;

    // set var
    public GameObject playerPrefab;
    public GameObject gridPrefeb;
    public GlobalSetting setting;

    public Text txtNextAction;
    public Text txtButton;

    public static Control instance;

    public GameObject[] players;
    public GameObject[] maps;

    private int actionIndex = 0;

    private void Awake() {
        if (!instance)
            instance = this;
        else
            Debug.Log("instance exist");

        // spawn player
        players = new GameObject[playerNumber];
        
        for (int i = 0; i < playerNumber; ++i) {
            GameObject player = Instantiate(playerPrefab, new Vector3(0 - (i - playerNumber / 2) * formDistance, 0, 0), Quaternion.identity);
            players[i] = player;

            player.name = "Player" + (i + 1);
            player.GetComponent<Player>().index = i;
            player.GetComponent<Player>().position = mapSize - 1;
            
        }

        // spawn map
        maps = new GameObject[mapSize];
        float gridScale = setting.gridScale * gridSize;
        int mapLineSize = mapSize / 4;
        Vector2 startPos = new Vector2(-Mathf.FloorToInt(mapLineSize / 2), -Mathf.FloorToInt(mapLineSize / 2));
        if (mapLineSize % 2 == 1) {
            startPos += new Vector2(0.5f, 0.5f);
        }
        startPos *= gridScale;

        for (int i = 0; i < mapSize; ++i) {
            GameObject obj = Instantiate(gridPrefeb, transform);
            maps[i] = obj;

            obj.name = "Grid" + (i + 1);
            
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

    private void Start() {
        foreach (GameObject player in players) {
            setPlayerPos(player);
        }

        setActionText();
    }

    public void setPlayerPos(GameObject player) {
        float playerNum = players.Length;
        float i = 0.5f;
        int pos = player.GetComponent<Player>().position;
        Transform gridTrs = maps[pos].transform;
        Vector3 rot = new Vector3(0, (i + player.GetComponent<Player>().index) / playerNum * 360f, 0);

        player.transform.position = gridTrs.position + gridTrs.localScale / 2 + new Vector3(0, setting.gridScale * gridSize, 0);
        player.transform.Rotate(rot);
        player.transform.position += player.transform.forward * gridTrs.localScale.x * gridSize / 5;

        player.transform.rotation = Quaternion.identity;
    }

    public static Vector3 calPos(float distance, float degrees) {
        float radians = degrees * Mathf.Deg2Rad;
        float x = Mathf.Cos(radians);
        float y = Mathf.Sin(radians);
        return new Vector3(x, 0, y);
    }

    private void Update() {
        
    }

    public void Action() {
        Player player = players[actionIndex].GetComponent<Player>();
        int pos = player.position;
        int randNum = Random.Range(1, 1 + randRange);
        player.position = (pos + randNum) % mapSize;
        setPlayerPos(player.gameObject);
        actionIndex = (actionIndex + 1) % playerNumber;
        txtButton.text = randNum.ToString();
        setActionText();
    }

    public void setActionText() {
        Color[] colors = setting.playerColors;
        if (actionIndex < colors.Length)
            txtNextAction.color = colors[actionIndex];
        else
            txtNextAction.color = new Color(0, 0, 0, 1);
        txtNextAction.text = (actionIndex + 1).ToString();
    }
}
