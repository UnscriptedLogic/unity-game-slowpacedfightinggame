using UnityEngine;
using UnscriptedEngine;

public class UIC_AbilityHUD : UCanvasController, ICanvasController
{
    [SerializeField] private float followLerp = 10f;
    private Transform mainCameraTransform;

    public void Initialize()
    {
        mainCameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (mainCameraTransform != null)
        {
            Vector3 targetDir = mainCameraTransform.forward;
            float step = 10f * Time.fixedDeltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }
    }
}