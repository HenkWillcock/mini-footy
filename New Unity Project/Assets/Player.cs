using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum Team {RED, BLUE}

    public Rigidbody rigidbody;
    public Team team;
    public Ball ball;

    public bool standing = true;
    public Vector3 target;
    private Color color;

    void Start()
    {
        this.target = this.rigidbody.position;

        if (this.team == Team.RED) {
            this.color = new Color(1, 0, 0);
        } else if (this.team == Team.BLUE) {
            this.color = new Color(0, 0, 1);
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) {
            renderer.material.color = this.color;
        }
    }

    void Update()
    {
        float distanceToTarget = Vector3.Distance(
            this.rigidbody.transform.position, 
            this.target
        );
        if (distanceToTarget > 0.5f && this.standing) {
            if (this.ball == null) {
                this.MoveTowardsTarget(1f, 2.5f);
            } else {
                this.MoveTowardsTarget(1f, 2f);
            }
        } else {
            this.rigidbody.velocity *= 0.95f;
        }
        if (this.standing) {
            this.rigidbody.rotation = Quaternion.AngleAxis(0, Vector3.up);
        }
    }

    protected void MoveTowardsTarget(float accelleration, float topSpeed) {
        Vector3 towardsTarget = this.target - this.rigidbody.position;
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
            otherPlayer.ball != null
        ) {
            foreach (Collider collider in otherPlayer.gameObject.GetComponentsInChildren<Collider>()) {
                Physics.IgnoreCollision(collider, otherPlayer.ball.GetComponent<Collider>(), false);
            }
            otherPlayer.Knockdown();
        }
    }

    public void Knockdown() {
        this.standing = false;
        StartCoroutine(GetUp());
    }

    IEnumerator GetUp() {
        yield return new WaitForSeconds(3f);
        this.standing = true;
    }
}
