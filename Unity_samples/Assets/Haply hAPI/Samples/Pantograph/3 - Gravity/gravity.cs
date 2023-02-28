using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gravity : MonoBehaviour
{
    public GameObject sun;
    private Vector3 sunPos;
    
    private float sunM = 1000000000000.0f;

    public GameObject planet;
    private Vector3 planetPos;
    private Vector3 planetOldPos;
    public Rigidbody planetRb;
    private Vector3 planetVel = new Vector3 (0f,5f,0f);
    private Vector3 planetAccel;
    private float planetM = 10000.0f;
    
    float G = 6.67f*Mathf.Pow(10,-11);
    float time = 0.002f;
    

    // Start is called before the first frame update
    void Start()
    {
        //planetRb.AddForce(planetVel, ForceMode.VelocityChange);
        Debug.Log( $"gravity: {G}" );
    }

    // Update is called once per frame
    void Update()
    {
        planetOldPos = planet.transform.position;

        planetAccel = calculateForce()/planetM;

        planet.transform.position = planetOldPos + ((planetVel+0.5f*planetAccel*time)*time);

        planetPos = planet.transform.position;

        planetVel = (planetPos-planetOldPos)/time;
    }

    public Vector3 calculateForce(){
        sunPos         = sun.transform.position;
        planetPos      = planet.transform.position;

        float distance = Vector3.Distance(sunPos,planetPos);
        float distsq   = distance*distance;
        float magnitude    = G*sunM*planetM/distsq;

        Vector3 heading = (sunPos-planetPos);
        Vector3 force = (magnitude*heading/heading.magnitude);
        return(force);
    }
}
