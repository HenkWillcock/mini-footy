using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    // TODO extend Controller
    public List<Player> npcPlayers;
    public Player.Team team;
    public Ball ball;

    void Start()
    {
        this.npcPlayers = new List<Player>();

        foreach (Player player in FindObjectsOfType<Player>()) {
            if (player.team == this.team) {
                this.npcPlayers.Add(player);
                player.holdingPosition = true;
            }
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
                    npc.holdingPosition = false;
                } else {
                    // Others return to position.
                    npc.holdingPosition = true;
                }
            }

        } else {
            foreach (Player npc in npcPlayers) {
                if (npc == this.ball.holder) {
                    // Player with the ball run for the goal.
                    npc.target = Vector3.forward * 12f;
                    npc.holdingPosition = false;

                    if (Vector3.Distance(npc.transform.position, Vector3.forward * 12f) < 10f) {
                        npc.Kick("Ground", Vector3.forward * 12f, 0.8f);
                    }
                } else {
                    // Others return to position.
                    npc.holdingPosition = true;
                }
            }
        }
    }
}
