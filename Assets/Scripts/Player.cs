using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Animator animator;
    private Vector3 baseScale;
    private Camera camera;

    public bool canShoot => true;
    public bool canJump => CurrentEnergy-JumpEnergyCost >= 0 && playerState == PlayerState.Moving;
    public bool canMove => CurrentEnergy-(WalkEnergyCost*Time.fixedDeltaTime) >= 0 && playerState == PlayerState.Moving;
    public bool isShooting = false;
    private bool IsGrounded => feet.Select(foot => Physics2D.OverlapCircle(foot.position, FeetCollisionRadius, CollisionLayer)).Any(hit => hit != null);

    public int Health = 3;
    public int MaxEnergy = 100;
    public float CurrentEnergy;
    public int JumpEnergyCost = 20;
    public int WalkEnergyCost = 10;
    public float AngleChangeSpeed = 2;
    public float MovementSpeed = 5;
    public float JumpForce = 750;
    public float FeetCollisionRadius = 0.05f;
    public LayerMask CollisionLayer;
    public Transform[] feet;
    
    public Transform EnergyBarLocation;
    public float EnergyBarLength = 75;
    public float EnergyBarHeight = 6;
    public Texture EnergyBarTexture;
    public Color EneryBarColor = Color.green;
    public Color EmptyEnergyBarColor = Color.red;

    public GameObject BreathBar;
    private float baseBreathBarHeight;
    public GameObject DeathPrefab;
    public Transform BombSpawn;
    public GameObject BombPrefab;
    [Range(0,180)]public float BombAngle;
    [Range(0.8f, 1.3f)] public float BombForce;
    public float BombForceMultiplier;
    public float BombTorqueMultiplier;
    public PlayerState playerState = PlayerState.Inactive;
    private Vector2 BombAngleVector => new Vector2(-Mathf.Cos(BombAngle * Mathf.Deg2Rad), Mathf.Sin(BombAngle * Mathf.Deg2Rad));

    public int HorizontalInput =>
        (int) (Input.GetAxisRaw("Horizontal") + GameController.Instance.SkreduinoConnector.GetAxis("h"));

    public BombEvent ShootBombEvent = new BombEvent();
    [HideInInspector]public List<Player> Team = null;

    public enum PlayerState { Inactive, Moving, Shooting, Breathing };

    [System.Serializable]
    public class BombEvent : UnityEvent<Bomb> { }
    
    public void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        camera = Camera.main;
        baseBreathBarHeight = BreathBar.transform.localScale.y;
        SetBreathBarHeight(0f);
    }

    private void Update()
    {
        UpdateAnimator();
        switch (playerState)
        {
            case PlayerState.Inactive:
            case PlayerState.Breathing:
                break;
            case PlayerState.Moving:
                if (Input.GetButtonDown("Jump"))
                {
                    Jump();
                }
                if (Input.GetButtonDown("Fire1"))
                {
                    StartShoot();
                }
                break;
            case PlayerState.Shooting:
                if (Input.GetButtonDown("Fire1"))
                {
                    EndShoot();
                }
                var hInput = HorizontalInput;
                if (hInput != 0)
                {
                    AdjustAngle(hInput*AngleChangeSpeed);
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (playerState == PlayerState.Moving)
        {
            var hInput = HorizontalInput;

            Movement(hInput);
        }
    }

    private void OnDestroy()
    {
        Team?.Remove(this);
    }

    private void AdjustAngle(float angleDelta)
    {
        BombAngle = Mathf.Clamp(BombAngle + angleDelta, 0, 180);
        if (BombAngle < 90)
        {
            transform.localScale = new Vector3(baseScale.x,baseScale.y);
        } else {
            transform.localScale = new Vector3(-baseScale.x, baseScale.y);
        }
    }

    private void Movement(float hInput)
    {
        if (!canMove || hInput == 0)
            return;

        CurrentEnergy -= WalkEnergyCost * Time.fixedDeltaTime;
        //rigidbody2d.velocity = new Vector2(hInput*MovementSpeed, rigidbody2d.velocity.y);
        transform.position += new Vector3(hInput*MovementSpeed*Time.deltaTime, 0);
    }
    
    private void OnGUI()
    {
        if (playerState != PlayerState.Moving)
            return;
        
        var position = camera.WorldToScreenPoint(EnergyBarLocation.position);
        var convertedPosY = Screen.height - position.y;
        
        var energyBarRectStart = new Vector2(position.x - (EnergyBarLength / 2), convertedPosY - EnergyBarHeight/2);
        var energyBarRectEnd = new Vector2(EnergyBarLength * (CurrentEnergy / MaxEnergy), EnergyBarHeight);
        
        var emptyBarRectStart = new Vector2(energyBarRectStart.x+energyBarRectEnd.x,convertedPosY - EnergyBarHeight/2);
        var emptyBarRectEnd = new Vector2(EnergyBarLength - energyBarRectEnd.x,EnergyBarHeight);

        
        var energyBarRect = new Rect(energyBarRectStart, energyBarRectEnd);
        var emptyEnergyBarRect = new Rect(emptyBarRectStart,emptyBarRectEnd);

        GUI.color = EneryBarColor;
        GUI.DrawTexture(energyBarRect, EnergyBarTexture);
        GUI.color = EmptyEnergyBarColor;
        GUI.DrawTexture(emptyEnergyBarRect,EnergyBarTexture);
    }

    private void UpdateAnimator()
    {
        //var hInput = Input.GetAxisRaw("Horizontal");
        var hInput = HorizontalInput;
        var isMoving = (hInput != 0) && canMove;
        
        animator.SetBool("IsMoving", isMoving && canMove);
        animator.SetBool("IsGrounded", IsGrounded);
        animator.SetFloat("VerticalSpeed", rigidbody2d.velocity.y);
        //animator.SetBool("IsBreathing", playerState == PlayerState.Breathing);

        if (playerState == PlayerState.Moving && hInput != 0)
        {
            var direction = -Mathf.Sign(hInput);
            transform.localScale = new Vector3(baseScale.x*direction,baseScale.y,baseScale.z);
        }
    }

    public void Jump()
    {
        if (canJump && IsGrounded)
        {
            rigidbody2d.AddForce(Vector2.up*JumpForce);
            CurrentEnergy -= JumpEnergyCost;
        }
    }

    public void StartShoot()
    {
        if (!canShoot)
            return;
        isShooting = true;
        animator.Play("Player_Windup_Bomb");
        var direction = Mathf.Sign(transform.localScale.x);
        if (direction == 1)
        {
            BombAngle = 60;
        } else
        {
            BombAngle = 120;
        }

        playerState = PlayerState.Shooting;
        animator.SetBool("IsShooting",true);
    }

    public void EndShoot(float force)
    {
        if (!canShoot)
            return;

        BombForce = 0.8f + force/2f;
        isShooting = false;
        playerState = PlayerState.Inactive;
        animator.SetBool("IsShooting",false);
    }

    public void EndShoot()
    {
        if (!canShoot)
            return;

        isShooting = false;
        playerState = PlayerState.Inactive;
        animator.SetBool("IsShooting", false);
    }

    private void ShootBomb()
    {
        var bomb = Instantiate(BombPrefab, BombSpawn.position, BombSpawn.rotation);
        var bombComponent = bomb.GetComponent<Bomb>();
        var bombRigidbody2d = bomb.GetComponent<Rigidbody2D>();

        var totalForce = BombForceMultiplier * BombForce * BombAngleVector;
        var torque = BombForce * BombTorqueMultiplier;
        bombComponent.Team = Team;
        
        bombRigidbody2d.AddForce(totalForce);
        bombRigidbody2d.AddTorque(torque);
        ShootBombEvent.Invoke(bombComponent);
    }

    public void TakeDamage()
    {
        Health--;
        if (Health <= 0)
        {
            animator.Play("Player_Die");
        }
        else
        {
            animator.Play("Player_Take_Damage");
        }
    }

    public void Die()
    {
        //Instantiate(DeathPrefab).transform.position = transform.position;
        //Team.Remove(this);
        gameObject.SetActive(false);
        if (GameController.Instance.ActivePlayer == this)
        {
            //GameController.Instance.PlayerTurn(GameController.Instance.NextPlayer());
            GameController.Instance.NextPlayer();
        }
        //Destroy(gameObject);

    }

    public void SetBreathBarHeight(float rel)
    {
        BreathBar.transform.localScale = new Vector2(BreathBar.transform.localScale.x, rel*baseBreathBarHeight);
    }
}
