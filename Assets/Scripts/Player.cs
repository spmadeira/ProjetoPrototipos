using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Animator animator;

    public bool canMove;
    
    public int MaxEnergy;
    public int CurrentEnergy;
    public int JumpEnergyCost;
    public float MovementSpeed;
    public float JumpForce;

    public void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        var hInput = Input.GetAxisRaw("Horizontal");
        var vInput = Input.GetAxisRaw("Vertical");
        
        Movement(hInput);
    }

    private void Movement(float hInput)
    {
        if (!canMove)
            return;
        
        transform.position += new Vector3(hInput*MovementSpeed*Time.fixedDeltaTime, 0);
    }
}
