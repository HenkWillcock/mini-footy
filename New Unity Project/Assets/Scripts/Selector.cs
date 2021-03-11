using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : MonoBehaviour
{
    public Controller controller;

    void Update()
    {
        this.transform.position = new Vector3(
            this.controller.selectedPlayer.transform.position.x,
            0.52f,
            this.controller.selectedPlayer.transform.position.z
        );
    }
}
