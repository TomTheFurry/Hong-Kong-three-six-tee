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

    // set var
    public GameObject playerPrefab;
    public GameObject gridPrefeb;
    public GlobalSetting setting;

    public Text txtNextAction;
    public Text txtButton;
    public Text txtCard;

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
            // for test
            int rand = Random.Range(1, 3 + 1);
            obj.GetComponent<Grid>().type = rand == 2 ? 2 : 1;

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
            setPlayerPos(player.GetComponent<Player>());
        }

        setActionText();
    }

    public void setPlayerPos(Player player) {
        float playerNum = players.Length;
        float i = 0.5f;
        int pos = player.position;
        Transform gridTrs = maps[pos].transform;
        Vector3 rot = new Vector3(0, (i + player.GetComponent<Player>().index) / playerNum * 360f, 0);

        player.transform.position = gridTrs.position + gridTrs.localScale / 2 + Vector3.up * setting.gridScale * gridSize;
        player.transform.Rotate(rot);
        player.transform.position += player.transform.forward * gridTrs.localScale.x * gridSize / 5;

        player.transform.rotation = Quaternion.identity;
    }

    public void action() {
        Player player = players[actionIndex].GetComponent<Player>();
        moveForwardRand(player, false);

        // after action 
        actionIndex = (actionIndex + 1) % playerNumber;
        setActionText();
    }

    public int rollDisk() {
        int randNum = Random.Range(1, 1 + randRange);
        // other disk code
        txtButton.text = randNum.ToString();

        return randNum;
    }

    public void moveForwardRand(Player player, bool byEffect) {
        moveForward(player, rollDisk(), byEffect);
    }

    public void moveForward(Player player, int step, bool byEffect) {
        int pos = player.position;
        player.position = (pos + step) % mapSize;
        setPlayerPos(player);
        afterMove(player, byEffect);
    }

    public void moveBackRand(Player player, bool byEffect) {
        moveBack(player, rollDisk(), byEffect);
    }

    public void moveBack(Player player, int step, bool byEffect) {
        int pos = player.position;
        player.position = (pos + mapSize - step) % mapSize;
        setPlayerPos(player);
        afterMove(player, byEffect);
    }

    public void afterMove(Player player, bool byEffect) {
        // grid
        Grid grid = maps[player.position].GetComponent<Grid>();
        grid.action(player, byEffect);
        //grid.updateColor(Color.gray);
    }

    public void setActionText() {
        Color[] colors = setting.playerColors;
        if (actionIndex < colors.Length)
            txtNextAction.color = colors[actionIndex];
        else
            txtNextAction.color = new Color(0, 0, 0, 1);
        txtNextAction.text = (actionIndex + 1).ToString();
    }

    public void hideCard() {
        txtCard.gameObject.SetActive(false);
    }

    public void showCard(GlobalSetting.Card card) {
        txtCard.gameObject.SetActive(true);
        txtCard.text = card.description;
    }

    public static Vector3 calPos(float distance, float degrees) {
        float radians = degrees * Mathf.Deg2Rad;
        float x = Mathf.Cos(radians);
        float y = Mathf.Sin(radians);
        return new Vector3(x, 0, y);
    }
}
