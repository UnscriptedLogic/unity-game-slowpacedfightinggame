using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingBehaviour : MonoBehaviour
{
    [SerializeField] private float height = 0.01f;
    [SerializeField] private float timeOffset;

    private void Update()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + Mathf.Sin(Time.time + timeOffset) * height, transform.position.z);
    }
}
