using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ship_class : MonoBehaviour
{
    public new Rigidbody2D rigidbody { get; private set; }
    public Vector2 gravitational_forces;
	public float mass;
    public int fuel = 10;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {

        if (fuel<=0)
        {
            Debug.Log("No Mo Fuel!");   
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            --fuel;
        }
    }
}