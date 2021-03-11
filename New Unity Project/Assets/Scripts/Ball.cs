using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Rigidbody rigidbody;
    public Player holder;
    public bool canBePickedUp = true;
    public float spin;

    void Update()
    {
        if (this.holder != null) {
            this.transform.position = this.holder.transform.position;
            if (!this.holder.standing) {
                this.holder = null;
            }
        }

        this.rigidbody.velocity += Quaternion.AngleAxis(90, Vector3.up) * this.rigidbody.velocity.normalized * spin;
    }

    public void OnCollisionEnter(Collision collision) {
        this.spin = 0f;

        if (this.holder != null) {
            return;
        }

        Player catcher = collision.gameObject.GetComponent<Player>();

        if (catcher != null && catcher.standing) {
            this.SetHolder(catcher.gameObject);
            catcher.chaseBall = false;
        }
    }

    public void SetHolder(GameObject player) {
        this.holder = player.GetComponent<Player>();

        foreach (Collider collider in player.GetComponentsInChildren<Collider>()) {
            Physics.IgnoreCollision(collider, GetComponent<Collider>());
        }
    }
}
