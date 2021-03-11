using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keeper : Player
{
    public override void GoToPosition() {
        this.target = this.startingPosition;
    }
}
