using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public int Timer = 480;
    public int CollisionTimerCost = 80;
    public int StayTimerCost = 15;
    public float ExplosionRadius = 3;
    public LayerMask ExplosionLayer;
    private bool HasExploded = false;
    public void Start()
    {
        HasExploded = false;
    }

    private void FixedUpdate()
    {
        Timer--;
        
        if (Timer <= 0 && !HasExploded)
            Explode();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, ExplosionRadius);
    }

    private void Explode()
    {
        HasExploded = true;
        var hits = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius, ExplosionLayer);
        
        //Quando uma tile é destruida as adjacentes são reconstruidas, então é necessário adquirir a referencia
        //a nova tile entre destruições. Pegando a referencia a posição na grid em vez ao objeto é possivel
        //manter a referencia ao objeto que vai virar.
        Debug.Log("Indo Destruir");
        
        var tilesToDestroy = new List<Vector3Int>();
        foreach (var hit in hits)
        {
            tilesToDestroy.Add(GameController.Instance.Tilemap.WorldToCell(hit.transform.position));
        }

        foreach (var tile in tilesToDestroy)
            GameController.Instance.Tilemap.SetTile(tile,null);
        
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            Timer -= CollisionTimerCost;
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            Timer -= StayTimerCost;
        }
    }
}
