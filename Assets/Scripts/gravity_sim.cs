using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


using static Slingshot;
using Debug = UnityEngine.Debug;

public class gravity_sim : MonoBehaviour
{
    [SerializeField]
    public float gravitationalConstant = 6.67e-11f; //1000f;
    public float physicsTimeStep = 0.001f;
    public float sun_mass = 1e+06f;
    private float sub_mass;

    private object m_ConcurrentDataLock;


    private int m_Steps;
    private int m_Frames;

    private int m_DrawSteps;
    private int m_DrawFrames;

    private Task g_SimulationLoopTask;

    GameObject avatar_obj;
    ship_class ship_val;
    private Vector2 ship_force;

    GameObject[] celestials;
    planet[] planet_vals;
    private void Awake()
    {
        celestials = GameObject.FindGameObjectsWithTag("Celestial");
        planet_vals = FindObjectsOfType<planet> ();
        avatar_obj = GameObject.FindGameObjectWithTag("EndEffectorAvatar");
        ship_val = FindObjectOfType<ship_class>();
        int size = celestials.Length;
        Time.fixedDeltaTime = physicsTimeStep;
        Debug.Log("Setting fixedDeltaTime to: " + gravitationalConstant);
        ship_val.mass = 20f;
        SetInitialVelocity();
    }

    void FixedUpdate()
    {
        for (int i = 0; i < celestials.Length; i++)
        {
            GameObject main_planet = celestials[i];
            Vector2 accel = Vector2.zero;
            float main_mass = main_planet.GetComponent<Rigidbody2D>().mass;

            foreach (GameObject sub_planet in celestials)
            {
                if (!main_planet.Equals(sub_planet))
                {
                    sub_mass = sub_planet.GetComponent<Rigidbody2D>().mass;
                    float r = Vector2.Distance(main_planet.transform.position, sub_planet.transform.position);
                    accel += (Vector2)(sub_planet.transform.position - main_planet.transform.position).normalized * gravitationalConstant * sub_mass / (r * r);
                    
                }
            }
            planet_vals[i].grav = main_mass * accel;
            planet_vals[i].vel += accel * physicsTimeStep;
            //Debug.Log(accel);
        }
        for (int i = 0; i < celestials.Length; i++)
        {
            //if (celestials[i].name != "Sun")
            celestials[i].GetComponent<Rigidbody2D>().MovePosition(celestials[i].GetComponent<Rigidbody2D>().position + planet_vals[i].vel * physicsTimeStep);

        }
        ship_force = Vector2.zero;
        foreach (GameObject sub_planet in celestials)
        {
            float r = Vector2.Distance(avatar_obj.transform.position, sub_planet.transform.position);
            ship_force += (Vector2)(sub_planet.transform.position - avatar_obj.transform.position).normalized * gravitationalConstant * sub_mass / (r * r);
        }
        ship_val.gravitational_forces = ship_val.mass * ship_force;
    }
    private void SetInitialVelocity()
    {
        Debug.Log("Setting Initial Velocity...\n");
        for (int i = 0; i < celestials.Length; i++)
        {
            GameObject main_planet = celestials[i];
            Debug.Log(main_planet.name);
            foreach (GameObject sub_planet in celestials)
            {
                if (!main_planet.Equals(sub_planet))
                {
                    float sub_planet_mass = sub_planet.GetComponent<Rigidbody2D>().mass;
                    float r = Vector2.Distance(main_planet.transform.position, sub_planet.transform.position);
                    Vector2 vel = (Vector2)main_planet.transform.right * Mathf.Sqrt((gravitationalConstant * sub_planet_mass) / r);
                    Debug.Log(vel.y);
                    planet_vals[i].vel += vel;
                }
            }

        }
    }
}
