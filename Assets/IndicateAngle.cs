using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicateAngle : MonoBehaviour
{
    public Player Player;
    public GameObject Indicator;

    private void Update()
    {
        if (!Player.isShooting && Indicator.activeSelf)
        {
            Indicator.SetActive(false);
        } else if (Player.isShooting)
        {
            if (!Indicator.activeSelf)
                Indicator.SetActive(true);

            var rawAngle = Player.BombAngle;
            var actualAngle = Mathf.Sign(Player.transform.localScale.x) == 1 ? 180 - rawAngle : rawAngle;
            
            transform.rotation = Quaternion.Euler(0, 0, actualAngle - 90);
        }
    }
}
