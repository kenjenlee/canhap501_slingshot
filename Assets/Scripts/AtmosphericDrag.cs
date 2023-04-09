using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphericDrag : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "EndEffectorAvatar")
        {
            collision.gameObject.GetComponent<Rigidbody2D>().drag = 0.1f;
            Debug.Log("Entered the gravitational pull region");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "EndEffectorAvatar")
        {
            collision.gameObject.GetComponent<Rigidbody2D>().drag = 0;
            Debug.Log("Left the gravitational pull region");
        }
    }

}
