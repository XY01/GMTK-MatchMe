using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    public float yRange = 8;
    public float xRange = 4.5f;
    public float smoothing = 6;
    private Quaternion targetRot;
    
    void Update()
    {
        float xOffsetNorm = (Input.mousePosition.y / Screen.height);
        float yOffsetNorm = (Input.mousePosition.x / Screen.width);

        targetRot = Quaternion.Euler(
            Mathf.Lerp(-xRange, xRange, xOffsetNorm),
            Mathf.Lerp(-yRange, yRange, yOffsetNorm),
            0);
        transform.localRotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothing);
    }
}
