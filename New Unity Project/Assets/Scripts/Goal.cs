using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public int goals;

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerExit(Collider collider) {
        Ball ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null) {
            this.goals++;
        }
    }
}
