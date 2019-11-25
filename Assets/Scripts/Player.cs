﻿using System;
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

    public Transform BombSpawn;
    public GameObject BombPrefab;
    [Range(0,180)]public float BombAngle;
    [Range(0, 1)] public float BombForce;
    public float BombForceMultiplier;
    public float BombTorqueMultiplier;
    public PlayerState playerState = PlayerState.Inactive;
    private Vector2 BombAngleVector => new Vector2(Mathf.Cos(BombAngle * Mathf.Deg2Rad), Mathf.Sin(BombAngle * Mathf.Deg2Rad));

    public BombEvent ShootBombEvent = new BombEvent();

    public enum PlayerState { Inactive, Moving, Shooting };

    [System.Serializable]
    public class BombEvent : UnityEvent<Bomb> { }
    
    public void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        camera = Camera.main;

    }

    private void Update()
    {
        //UpdateAnimator();

        //if (Input.GetKeyDown(KeyCode.Z))
        //    Jump();

        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    if (!isShooting)
        //        StartShoot();
        //    else
        //        EndShoot();
        //}
        UpdateAnimator();
        switch (playerState)
        {
            case PlayerState.Inactive:
                break;
            case PlayerState.Moving:
                if (Input.GetButtonDown("Jump")){
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
                var hInput = Input.GetAxisRaw("Horizontal");
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
            var hInput = Input.GetAxisRaw("Horizontal");
            var vInput = Input.GetAxisRaw("Vertical");

            Movement(hInput);
        }
    }

    private void AdjustAngle(float angleDelta)
    {
        BombAngle = Mathf.Clamp(BombAngle + angleDelta, 0, 180);
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
        //foreach (var foot in feet)
        //{
        //    Gizmos.DrawSphere(foot.position,FeetCollisionRadius);
        //}
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
        var hInput = Input.GetAxisRaw("Horizontal");
        var isMoving = (hInput != 0) && canMove;
        
        animator.SetBool("IsMoving", isMoving && canMove);
        animator.SetBool("IsGrounded", IsGrounded);
        animator.SetFloat("VerticalSpeed", rigidbody2d.velocity.y);

        if (playerState == PlayerState.Moving && hInput != 0)
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
        if (!canShoot)
            return;
        isShooting = true;
        playerState = PlayerState.Shooting;
        animator.SetBool("IsShooting",true);
    }

    private void EndShoot()
    {
        if (!canShoot)
            return;
        isShooting = false;
        playerState = PlayerState.Inactive;
        animator.SetBool("IsShooting",false);
    }

    private void ShootBomb()
    {
        var bomb = Instantiate(BombPrefab, BombSpawn.position, BombSpawn.rotation);
        var bombComponent = bomb.GetComponent<Bomb>();
        var bombRigidbody2d = bomb.GetComponent<Rigidbody2D>();

        var force = BombForceMultiplier * BombForce * BombAngleVector;
        var torque = BombForce * BombTorqueMultiplier;
        
        bombRigidbody2d.AddForce(force);
        bombRigidbody2d.AddTorque(torque);
        ShootBombEvent.Invoke(bombComponent);
    }
}
