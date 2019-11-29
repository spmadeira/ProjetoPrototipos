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

        var isBomb = collision.TryGetComponent<Bomb>(out var bomb);
        
        if (isBomb)
        {
          bomb.Explode(true);
        }
    }
}
