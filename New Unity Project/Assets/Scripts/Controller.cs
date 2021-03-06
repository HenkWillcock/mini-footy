﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public Player selectedPlayer;
    public List<Player> players;
    public Selector selector;

    public Player.Team team;
    public Ball ball;
    public HUD hud;

    private float powerBar = 0.5f;

    void MakeRun(Player player) {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit)) {
            Player playerClicked = this.PlayerClicked(
                hit.point, 
                new List<Player>(FindObjectsOfType<Player>())
            );
            if (player == this.ball.holder) {
                player.target = hit.point;
                player.playerTarget = null;
                player.chaseBall = false;

            } else if (this.BallClicked(hit.point)) {
                player.chaseBall = true;
                player.playerTarget = null;
            } else if (playerClicked != null) {
                player.playerTarget = playerClicked;
                player.chaseBall = false;
            } else {
                player.target = hit.point;
                player.playerTarget = null;
                player.chaseBall = false;
            }
            player.holdingPosition = false;
        }
    }

    void Start()
    {
        this.players = new List<Player>();

        foreach (Player player in FindObjectsOfType<Player>()) {
            if (player.team == this.team) {
                this.players.Add(player);
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit)) {
                Player playerClicked = this.PlayerClicked(hit.point, this.players);

                if (playerClicked != null && playerClicked.team == this.team) {
                    this.selectedPlayer = playerClicked;
                }
            }
        }

        if (Input.GetMouseButtonUp(2)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            List<Player> freePlayers = new List<Player>();
            foreach (Player player in this.players) {
                if (player != this.ball.holder && player != this.selectedPlayer) {
                    freePlayers.Add(player);
                }
            }

            if (Physics.Raycast(ray, out hit)) {
                Player player = this.NearestPlayerToPoint(hit.point, freePlayers);
                this.MakeRun(player);
            }
        }

        if (Input.GetMouseButtonUp(1)) {
            this.MakeRun(this.selectedPlayer);
        }

        if (Input.GetKeyUp("q")) {
            // TODO add tackling
            this.Kick("Ground");

        } else if (Input.GetKeyUp("w")) {
            this.Kick("Chip");

        } else if (Input.GetKeyUp("e")) {
            this.Kick("Left Curl");

        } else if (Input.GetKeyUp("r")) {
            this.Kick("Right Curl");
        }

        if (Input.GetKeyUp("space") && this.selectedPlayer.IsGrounded()) {
            this.selectedPlayer.rigidbody.velocity += Vector3.up * 12f;
        }

        if ((Input.GetKey("q") || Input.GetKey("w")) && this.ball.holder != null && this.ball.holder.team == this.team) {
            this.hud.powerBarValue += 0.01f;

            if (this.hud.powerBarValue > 1f) {
                if (Input.GetKey("q")) {
                    this.Kick("Ground");
                } else if (Input.GetKey("w")) {
                    this.Kick("Chip");
                }
                this.hud.powerBarValue = 0f;
            }
        } else {
            this.hud.powerBarValue = 0f;
        }

        // TODO add jumping.

        foreach (Player player in this.players) {
            player.targetLine.SetPosition(0, player.transform.position - this.transform.up * 0.5f);
            player.targetLine.SetPosition(1, player.target);

            if (player == this.selectedPlayer) {
                player.targetLine.material.color = new Color(1, 1, 0);
            } else {
                player.targetLine.material.color = player.color;
            }
        }
    }

    public void Kick(string type) {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit)) {
            if (this.selectedPlayer == this.ball.holder) {
                Player playerClicked = this.PlayerClicked(hit.point, this.players);

                if (playerClicked) {
                    this.selectedPlayer = playerClicked;
                }

                this.ball.holder.Kick(type, hit.point, this.hud.powerBarValue/2 + 0.5f);
            } else {
                this.selectedPlayer.Kick(type, hit.point, this.hud.powerBarValue/2 + 0.5f);
            }
        }
    }

    public Player NearestPlayerToPoint(Vector3 point, List<Player> players) {
        Player nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Player player in players) {
            float distance = Vector3.Distance(
                player.transform.position, 
                point
            );
            if (distance < minDist && player.standing) {
                nearest = player;
                minDist = distance;
            }
        }

        return nearest;
    }

    public float DistanceFromNearestPlayerToPoint(Vector3 point, List<Player> players) {
        Player nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Player player in players) {
            float distance = Vector3.Distance(
                player.transform.position, 
                point
            );
            if (distance < minDist && player.standing) {
                nearest = player;
                minDist = distance;
            }
        }

        return minDist;
    }

    public Player PlayerClicked(Vector3 point, List<Player> players) {
        if (this.DistanceFromNearestPlayerToPoint(point, players) < 1.5f) {
            return this.NearestPlayerToPoint(point, players);
        } else {
            return null;
        } 
    }

    public bool BallClicked(Vector3 point) {
        return Vector3.Distance(
            this.ball.transform.position,
            point
        ) < 1.5f;
    }
}
