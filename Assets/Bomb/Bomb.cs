using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Bomb : MonoBehaviour
{
    public int Timer = 480;
    public int CollisionTimerCost = 80;
    public int StayTimerCost = 15;
    public float ExplosionRadius = 3;
    public float DamageRadius;
    public float DamageKnockbackForce = 800;
    public LayerMask ExplosionLayer;
    public LayerMask DamageLayer;
    public GameObject ExplosionPrefab;
    public FontStyle TimerFont;
    public int TimerFontSize;
    public Transform TimerLocation;
    private bool HasExploded = false;
    private Camera camera;

#if UNITY_EDITOR
    public enum ShowRadius
    {
        None,
        Explosion,
        Damage
    };

    public ShowRadius showRadius;
#endif
    
    public UnityEvent ExplodeEvent = new UnityEvent();
    [HideInInspector]public List<Player> Team = null;
    public void Start()
    {
        camera = Camera.main;
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
#if UNITY_EDITOR
        switch (showRadius)
        {
            case ShowRadius.None:
                break;
            case ShowRadius.Explosion:
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, ExplosionRadius);
                break;
            case ShowRadius.Damage:
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, DamageRadius);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
#endif
    }

    private void OnGUI()
    {
        //var secondsUntilExplosion = (int)(Timer * Time.fixedDeltaTime);
        //var centerPosition = camera.WorldToScreenPoint(TimerLocation.position);

        //GUI.color = Color.red;
        //var style = new GUIStyle
        //{
        //    alignment = TextAnchor.MiddleCenter,
        //    fontSize = TimerFontSize,
        //    fontStyle = TimerFont,
        //};

        //GUI.Label(new Rect(centerPosition,new Vector2(0,0)), secondsUntilExplosion.ToString(), style);
    }

    private void Explode()
    {
        HasExploded = true;
        DestroyBlocks();
        DamagePlayers();
        
        Instantiate(ExplosionPrefab).transform.position = transform.position;
        ExplodeEvent.Invoke();
        Destroy(gameObject);
    }

    private void DamagePlayers()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, DamageRadius, DamageLayer);

        foreach (var hit in hits)
        {
            var isPlayer = hit.TryGetComponent<Player>(out var player);
            
            if (!isPlayer || player.Team == Team)
                continue;

            var hasCollider = player.TryGetComponent<Rigidbody2D>(out var rb2d);
            if (!hasCollider)
                continue;
            var position = transform.position;
            var point = hit.ClosestPoint(position);
            var away = player.transform.position - position;
            rb2d.AddForceAtPosition(away.normalized*DamageKnockbackForce,point);
            player.TakeDamage();
        }
    }
    
    private void DestroyBlocks()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius, ExplosionLayer);
        
        //Quando uma tile é destruida as adjacentes são reconstruidas, então é necessário adquirir a referencia
        //a nova tile entre destruições. Pegando a referencia a posição na grid em vez ao objeto é possivel
        //manter a referencia ao objeto que vai virar.
        var tilesToDestroy = new List<Vector3Int>();
        foreach (var hit in hits)
        {
            tilesToDestroy.Add(GameController.Instance.Tilemap.WorldToCell(hit.transform.position));
        }

        foreach (var tile in tilesToDestroy)
            GameController.Instance.Tilemap.SetTile(tile,null);
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
