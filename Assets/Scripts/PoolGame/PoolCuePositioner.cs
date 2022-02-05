using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Positions the cue stick in an orbital direction off a center point and radius offset.
public class PoolCuePositioner : MonoBehaviour
{
    [SerializeField] private GameObject OrbitCenter;
    [SerializeField] private float OrbitOffset = 0;
    [SerializeField, Range(-89.9f, 89.9f)] private float OrbitPitch = 0;
    [SerializeField, Range(0.0f, 359.9f)] private float OrbitYaw = 0;

    public bool bIsPitchIgnored = false;
    public bool bIsYawIgnored = false;

    private float PullPct = 0;

    [Min(0)]
    public float MaxPullDistance = 12;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!OrbitCenter)
        {
            return;
        }

        float totalOffset = OrbitOffset + (PullPct * MaxPullDistance);
        Vector3 orbitCenter = OrbitCenter.transform.position;
        Vector3 orbitDir = GetOrbitDirection();

        Quaternion cueOrientation = new Quaternion();
        cueOrientation.SetLookRotation(-orbitDir);

        transform.SetPositionAndRotation(orbitCenter + (orbitDir * totalOffset), cueOrientation);
    }

    public void SetOrbitObject(GameObject gameObject)
    {
        if(gameObject != OrbitCenter)
        {
            OrbitCenter = gameObject;
            ResetPositioning();
        }
    }

    public void SetOrbitOffsetDistance(float offsetDist)
    {
        OrbitOffset = offsetDist;
    }

    public void SetPullPct(float pct)
    {
        PullPct = Mathf.Clamp(pct, 0.0f, 1.0f);
    }

    public void SetOrbitOrientation(float pitch, float yaw)
    {
        OrbitPitch = Mathf.Clamp(pitch, -89.0f, 89.0f);
        OrbitYaw = Mathf.Clamp(yaw, 0.0f, 359.9f);
    }

    public void OnOrbit(float pitch, float yaw)
    {
        pitch = bIsPitchIgnored ? 0 : pitch;
        yaw = bIsYawIgnored ? 0 : yaw;

        SetOrbitOrientation(OrbitPitch + pitch, OrbitYaw + yaw);
    }

    public void GetOrbitOrientation(out float pitch, out float yaw)
    {
        pitch = OrbitPitch;
        yaw = OrbitYaw;
    }

    public float GetPullPct()
    {
        return PullPct;
    }

    private void ResetPositioning()
    {
        OrbitPitch = 0.0f;
        OrbitYaw = 90.0f;
        OrbitOffset = 0.0f;
        PullPct = 0.0f;
    }

    private Vector3 GetOrbitDirection()
    {
        Vector3 dir;

        float pitchRadian = Mathf.Deg2Rad * OrbitPitch;
        float yawRadian = Mathf.Deg2Rad * OrbitYaw;

        dir.x = Mathf.Cos(yawRadian) * Mathf.Cos(pitchRadian);
        dir.z = Mathf.Sin(yawRadian) * Mathf.Cos(pitchRadian);
        dir.y = Mathf.Sin(pitchRadian);

        dir.Normalize();

        return dir;
    }
}
