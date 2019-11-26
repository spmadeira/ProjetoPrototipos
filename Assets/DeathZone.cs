using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var isPlayer = collision.TryGetComponent<Player>(out var player);

        if (isPlayer)
        {
            player.Die();
        }
    }
}
