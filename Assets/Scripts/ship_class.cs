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
    public float fuel = 100f;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        if (GameManager.GetState() == GameState.Released)   {
            GetComponent<Rigidbody2D>().AddForce(new Vector2 (8e8f * gravitational_forces[0], 8e8f * gravitational_forces[1]));
        }
        if (fuel<=0)
        {
            Debug.Log("No More Fuel!");   
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            fuel -= 1f;
        }
    }
}