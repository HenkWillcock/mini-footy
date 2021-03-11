using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum Team {RED, BLUE}

    public Rigidbody rigidbody;
    public Team team;
    public LineRenderer targetLine;

    private Ball ball;
    private HUD hud;

    public bool standing = true;
    public bool holdingPosition = false;
    public Vector3 target;
    public Player playerTarget;
    public bool chaseBall;

    protected Vector3 startingPosition;
    public Color color;

    void Start()
    {
        this.startingPosition = this.transform.position;
        this.target = this.transform.position;

        if (this.team == Team.RED) {
            this.color = new Color(1, 0, 0);
        } else if (this.team == Team.BLUE) {
            this.color = new Color(0, 0, 1);
        }

        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>()) {
            renderer.material.color = this.color;
        }

        this.ball = FindObjectsOfType<Ball>()[0];
        this.hud = FindObjectsOfType<HUD>()[0];
    }

    void Update()
    {
        if (this.chaseBall) {
            this.target = this.ball.transform.position;
        } else if (this.playerTarget) {
            this.target = this.playerTarget.transform.position;
        }
        if (this.hud.paused) {
            this.rigidbody.velocity = Vector3.zero;
            this.rigidbody.rotation = Quaternion.AngleAxis(0, Vector3.up);
            return;
        }
        if (!this.standing) {
            return;
        }
        if (this.holdingPosition) {
            this.GoToPosition();
        }
        float distanceToTarget = Vector3.Distance(
            this.rigidbody.transform.position, 
            this.target
        );
        if (distanceToTarget > 0.5f) {
            if (this.ball.holder == this) {
                this.MoveTowardsTarget(1f, 1.5f, this.target);
            } else if (this.TeamHasBall()) {
                // Make a run speed bonus
                this.MoveTowardsTarget(1f, 2.5f, this.target);

            } else {
                this.MoveTowardsTarget(1f, 2f, this.target);
            }
        } else {
            this.rigidbody.velocity *= 0.95f;
        }

        this.rigidbody.rotation = Quaternion.AngleAxis(0, Vector3.up);
    }

    protected void MoveTowardsTarget(float accelleration, float topSpeed, Vector3 target) {
        Vector3 towardsTarget = target - this.rigidbody.position;
        towardsTarget.Normalize();

        float accellerationMagnitude = (1 - this.rigidbody.velocity.magnitude/topSpeed) * accelleration;

        if (accellerationMagnitude > 0) {
            this.rigidbody.AddForce(towardsTarget*accellerationMagnitude, ForceMode.Impulse);
        }
    }

    public void OnCollisionEnter(Collision collision) {
        Player otherPlayer = collision.gameObject.GetComponent<Player>();

        if (
            this.standing && 
            otherPlayer != null && 
            otherPlayer.team != this.team &&
            otherPlayer.standing &&
            this != this.ball.holder
        ) {
            foreach (Collider collider in otherPlayer.gameObject.GetComponentsInChildren<Collider>()) {
                Physics.IgnoreCollision(collider, otherPlayer.ball.GetComponent<Collider>(), false);
            }
            otherPlayer.Knockdown(this);

            if (otherPlayer == this.ball.holder) {
                this.ball.SetHolder(this.gameObject);
                this.chaseBall = false;
            } else {
                this.Knockdown(otherPlayer);
            }
        }
    }

    public virtual void GoToPosition() {
        this.target = this.startingPosition + (this.ball.transform.position - this.startingPosition) / 2;
    }

    public void Knockdown(Player otherPlayer) {
        this.standing = false;
        Vector3 awayFromOther = this.transform.position - otherPlayer.transform.position;
        awayFromOther.Normalize();
        this.rigidbody.AddForce(awayFromOther*10f, ForceMode.Impulse);
        this.chaseBall = false;
        this.playerTarget = null;
        this.target = this.transform.position;
        StartCoroutine(GetUp());
    }

    IEnumerator GetUp() {
        yield return new WaitForSeconds(5f);
        this.standing = true;
    }

    private bool TeamHasBall() {
        return this.ball.holder != null && this.ball.holder != this && this.ball.holder.team == this.team;
    }
}
