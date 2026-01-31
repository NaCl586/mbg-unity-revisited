using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimeTravel : Powerups
{
    public float timeBonus = 5f; 
    public class OnGetTimeTravel : UnityEvent { };
    public static OnGetTimeTravel onGetTimeTravel = new OnGetTimeTravel();

    public void Start()
    {
        onGetTimeTravel.AddListener(UsePowerup);
        bottomTextMsg = "You picked up a " + timeBonus + " second Time Travel Bonus!";
    }

    protected override void UsePowerup()
    {
        Marble.instance.ActivateTimeTravel(timeBonus);
    }
}
