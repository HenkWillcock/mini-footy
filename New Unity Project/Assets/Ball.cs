using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Rigidbody rigidbody;
    public Player holder;
    public bool canBePickedUp = true;

    void Update()
    {
        if (this.transform.position.y < -5f) {
            this.transform.position = Vector3.up;
        }

        if (this.holder != null) {
            if (!this.holder.standing) {
                this.DropBall();
            }
            this.transform.position = this.holder.transform.position;
        }
    }

    public void OnCollisionEnter(Collision collision) {
        if (this.holder != null) {
            return;
        }

        Player catcher = collision.gameObject.GetComponent<Player>();

        if (catcher != null && catcher.standing) {
            this.holder = catcher;
            this.holder.ball = this;

            foreach (Collider collider in collision.gameObject.GetComponentsInChildren<Collider>()) {
                Physics.IgnoreCollision(collider, GetComponent<Collider>());
            }
        }
    }

    public void DropBall() {
        this.holder.ball = null;
        this.holder = null;
    }
}
