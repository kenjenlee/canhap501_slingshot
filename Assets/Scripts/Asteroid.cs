using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Asteroid : MonoBehaviour
{
    public new Rigidbody2D rigidbody;
    public SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    public float ratio;
    public float size = 1f;
    public float minSize = 0.35f;
    public float maxSize = 1.65f;
    public float mass; 
    public float movementSpeed = 50f;
    public float maxLifetime = 30f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody = GetComponent<Rigidbody2D>();
        int LayerAsteroid = LayerMask.NameToLayer("Asteroid");
        gameObject.layer = LayerAsteroid;
    }
    private void Start()
    {
        // Assign random properties to make each asteroid feel unique
        spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
        transform.eulerAngles = new Vector3(0f, 0f, Random.value * 360f);
        mass = size * ratio;

        // Set the scale and mass of the asteroid based on the assigned size so
        // the physics is more realistic
        transform.localScale = Vector3.one * size;
        rigidbody.mass = mass;

        // Destroy the asteroid after it reaches its max lifetime
        Destroy(gameObject, maxLifetime);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Celestial"))
        {
            Destroy(gameObject);
        }
    }
    public void SetTrajectory(Vector2 direction)
    {
        // The asteroid only needs a force to be added once since they have no
        // drag to make them stop moving
        rigidbody.AddForce(direction * movementSpeed);
    }
}

