using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Animator animator;
    private Vector3 baseScale;
    private Camera camera;
    
    public bool canJump => CurrentEnergy-JumpEnergyCost >= 0 && !isShooting;
    public bool canMove => CurrentEnergy-(WalkEnergyCost*Time.fixedDeltaTime) >= 0 && !isShooting;
    public bool isShooting = false;
    private bool IsGrounded => feet.Select(foot => Physics2D.OverlapCircle(foot.position, FeetCollisionRadius, CollisionLayer)).Any(hit => hit != null);

    public int MaxEnergy = 100;
    public float CurrentEnergy;
    public int JumpEnergyCost = 20;
    public int WalkEnergyCost = 10;
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

    public Transform BombSpawn;
    public GameObject BombPrefab;
    [Range(0,180)]public float BombAngle;
    [Range(0, 1)] public float BombForce;
    public float BombForceMultiplier;
    public float BombTorqueMultiplier;
    private Vector2 BombAngleVector => new Vector2(Mathf.Cos(BombAngle * Mathf.Deg2Rad) * -(Mathf.Sign(transform.localScale.x)), Mathf.Sin(BombAngle * Mathf.Deg2Rad));
    
    public void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;
        camera = Camera.main;
    }

    private void Update()
    {
        UpdateAnimator();
        
        if (Input.GetKeyDown(KeyCode.Z))
            Jump();

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isShooting)
                StartShoot();
            else
                EndShoot();
        }
    }

    private void FixedUpdate()
    {
        var hInput = Input.GetAxisRaw("Horizontal");
        var vInput = Input.GetAxisRaw("Vertical");
        
        Movement(hInput);
    }

    private void Movement(float hInput)
    {
        if (!canMove || hInput == 0)
            return;

        CurrentEnergy -= WalkEnergyCost * Time.fixedDeltaTime;
        //rigidbody2d.velocity = new Vector2(hInput*MovementSpeed, rigidbody2d.velocity.y);
        transform.position += new Vector3(hInput*MovementSpeed*Time.deltaTime, 0);
    }

    private void OnDrawGizmos()
    {
        foreach (var foot in feet)
        {
            Gizmos.DrawSphere(foot.position,FeetCollisionRadius);
        }
    }

    private void OnGUI()
    {
        if (isShooting)
            return;
        
        GUI.color = EneryBarColor;
        var position = camera.WorldToScreenPoint(EnergyBarLocation.position);
        var convertedPosY = Screen.height - position.y;
        
        var startPosition = new Vector2(position.x - (EnergyBarLength / 2), convertedPosY - EnergyBarHeight/2);
        var endPosition = new Vector2(EnergyBarLength * (CurrentEnergy / MaxEnergy), + EnergyBarHeight);

        var energyBarRect = new Rect(startPosition, endPosition);

        GUI.DrawTexture(energyBarRect, EnergyBarTexture);
    }

    private void UpdateAnimator()
    {
        var hInput = Input.GetAxisRaw("Horizontal");
        var isMoving = hInput != 0;
        
        animator.SetBool("IsMoving", isMoving && canMove);
        animator.SetBool("IsGrounded", IsGrounded);
        animator.SetFloat("VerticalSpeed", rigidbody2d.velocity.y);

        if (isMoving)
        {
            var direction = -Mathf.Sign(hInput);
            transform.localScale = new Vector3(baseScale.x*direction,baseScale.y,baseScale.z);
        }
    }

    private void Jump()
    {
        if (canJump && IsGrounded)
        {
            rigidbody2d.AddForce(Vector2.up*JumpForce);
            CurrentEnergy -= JumpEnergyCost;
        }
    }

    private void StartShoot()
    {
        isShooting = true;
        animator.SetBool("IsShooting",true);
    }

    private void EndShoot()
    {
        isShooting = false;
        animator.SetBool("IsShooting",false);
    }

    private void ShootBomb()
    {
        var bomb = Instantiate(BombPrefab, BombSpawn.position, BombSpawn.rotation);
        var bombRigidbody2d = bomb.GetComponent<Rigidbody2D>();

        var force = BombForceMultiplier * BombForce * BombAngleVector;
        var torque = BombForce * BombTorqueMultiplier;
        
        bombRigidbody2d.AddForce(force);
        bombRigidbody2d.AddTorque(torque);
    }
}
