using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooter : MonoBehaviour
{
    public Transform FirePoint;
    public GameObject Fire;

    public GameObject HitPoint1;
    public GameObject HitPoint2;
    public GameObject HitPoint3;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shooting();
        }
    }

    public void Shooting()
    {
        RaycastHit hit;

        if (Physics.Raycast(FirePoint.position, transform.TransformDirection(Vector3.forward), out hit, 100))
        {
            Debug.DrawRay(FirePoint.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

            GameObject a = Instantiate(Fire, FirePoint.position, Quaternion.identity);
            Destroy(a, 1);

            if (hit.transform.name == "Obstacle1")
            {
                GameObject b = Instantiate(HitPoint1, hit.point, Quaternion.identity);
                Destroy(b, 1);
            }

            if (hit.transform.name == "Obstacle2")
            {
                GameObject b = Instantiate(HitPoint1, hit.point, Quaternion.identity);
                Destroy(b, 1);
            }

            if (hit.transform.name == "Obstacle3")
            {
                GameObject b = Instantiate(HitPoint1, hit.point, Quaternion.identity);
                Destroy(b, 1);
            }
        } 
    }
}
