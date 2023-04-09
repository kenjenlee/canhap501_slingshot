using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    public GameObject Target;
    public float HideDistance;

    void Update()
    {
        var dir = Target.transform.position - transform.position;


        if (dir.magnitude < HideDistance)
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }

        else
        {
            if (dir.magnitude > HideDistance)
            {
                transform.GetChild(0).gameObject.SetActive(true);
            }
            
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

    }
}
