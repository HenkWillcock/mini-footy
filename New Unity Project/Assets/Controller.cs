using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public Player selectedPlayer;
    public Player quickRunPlayer;
    public Player.Team team;
    public Ball ball;

    void Update()
    {
        // Quick Run
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit)) {
                Player playerHit = hit.rigidbody.gameObject.GetComponent<Player>();

                if (playerHit != null && playerHit.team == this.team) {
                    this.quickRunPlayer = playerHit;
                }
            }
        }

        // Select Player
        if (Input.GetMouseButtonUp(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit) && hit.rigidbody != null) {
                Player playerHit = hit.rigidbody.gameObject.GetComponent<Player>();

                if (playerHit != null && playerHit.team == this.team) {
                    this.selectedPlayer = playerHit;
                }
            }

            if (this.quickRunPlayer != null) {
                this.quickRunPlayer.target = hit.point;
                this.quickRunPlayer = null;
            }
        }

        // Move With Selected Player
        if (Input.GetMouseButtonUp(1)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit)) {
                this.selectedPlayer.target = hit.point;
            }
        }

        if (Input.GetKeyUp("q")) {
            if (this.ball.holder != null && this.ball.holder.team == this.team) {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit)) {
                    Vector3 towardsTarget = hit.point - this.ball.transform.position;
                    towardsTarget.y = 0f;
                    this.ball.rigidbody.velocity = towardsTarget.normalized*10f;
                    StartCoroutine(ReenableCollisions(this.ball.holder));
                    this.ball.DropBall();
                }
            }

        } else if (Input.GetKeyUp("w")) {
            if (this.ball.holder != null && this.ball.holder.team == this.team) {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit)) {
                    this.ball.rigidbody.position += Vector3.up;

                    Vector3 towardsTarget = hit.point - this.ball.transform.position;
                    towardsTarget.Normalize();
                    towardsTarget.y = 1f;
                    this.ball.rigidbody.velocity = towardsTarget.normalized*8f;
                    StartCoroutine(ReenableCollisions(this.ball.holder));
                    this.ball.DropBall();
                }
            }
        }
    }

    IEnumerator ReenableCollisions(Player oldHolder) {
        yield return new WaitForSeconds(0.2f);

        foreach (Collider collider in oldHolder.gameObject.GetComponentsInChildren<Collider>()) {
            Physics.IgnoreCollision(collider, this.ball.GetComponent<Collider>(), false);
        }
    }
}
