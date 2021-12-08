using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbulancePanner : MonoBehaviour
{
    
    public float x;
    private float z;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        z = Mathf.Sin(Time.time * 0.5f) * (30);
        float tx = Mathf.PingPong(Time.time * 0.75f, 15.5f) + x;
        // x go between 15.5 and input
        // z go tween 
        // Set the z to -30 and 30
        transform.position = new Vector3(tx, 0, z);
    }
}
