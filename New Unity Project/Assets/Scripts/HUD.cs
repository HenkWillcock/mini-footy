using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public List<Player> allPlayers;
    public bool paused = true;
    public Ball ball;
    public Goal redGoal;
    public Goal blueGoal;
    public Text scoreboard;

    void Start() {
        this.allPlayers = new List<Player>(FindObjectsOfType<Player>());
    }

    void Update()
    {
        if (Input.GetKeyUp("return")) {
            this.paused = false;
        }
        scoreboard.text = blueGoal.goals + " - " + redGoal.goals;

        if (this.ball.transform.position.y < -5f) {
            this.ball.holder = null;
            this.ball.transform.position = Vector3.up * 0.5f;
            this.ball.rigidbody.velocity = Vector3.zero;
            this.ball.rigidbody.angularVelocity = Vector3.zero;
            foreach (Player player in this.allPlayers) {
                if (player.transform.position.y < -5f) {
                    player.transform.position = new Vector3(Random.Range(10f, -10f), 1f, Random.Range(6f, -6f));
                }
            }
            this.paused = true;
        }
    }
}
