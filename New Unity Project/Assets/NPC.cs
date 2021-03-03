using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    // TODO just assign a team, then find players in Start();
    public List<Player> npcPlayers;
    private Dictionary<Player, Vector3> positions;
    public Player.Team team;
    public Ball ball;

    void Start() {
        this.positions = new Dictionary<Player, Vector3>();

        foreach (Player player in npcPlayers) {
            this.positions.Add(player, new Vector3(
                player.transform.position.x,
                player.transform.position.y,
                player.transform.position.z
            ));
        }
    }

    void Update()
    {
        if (this.ball.holder == null || this.ball.holder.team != this.team) {
            Player nearestNpc = null;
            float minDist = Mathf.Infinity;

            // If don't have the ball, nearest player chase it.
            foreach (Player npc in npcPlayers) {
                float distanceToBall = Vector3.Distance(npc.transform.position, this.ball.transform.position);
                if (distanceToBall < minDist && npc.standing) {
                    nearestNpc = npc;
                    minDist = distanceToBall;
                }
            }

            foreach (Player npc in npcPlayers) {
                if (npc == nearestNpc) {
                    npc.target = this.ball.transform.position;
                } else {
                    // Others return to position.
                    this.GoToPosition(npc);
                }
            }

        } else {
            foreach (Player npc in npcPlayers) {
                if (npc == this.ball.holder) {
                    // Player with the ball run for the goal.
                    npc.target = Vector3.forward * 10f;
                } else {
                    // Others return to position.
                    this.GoToPosition(npc);
                }
            }
            this.ball.holder.target = Vector3.forward * 10f;
        }
    }

    public void GoToPosition(Player npc) {
        npc.target = this.positions[npc] + (this.ball.transform.position - this.positions[npc]) / 2;
    }
}
