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
    [SerializeField, Range(-89.9f, 89.9f)] private float PitchLimitLower = -89.9f;
    [SerializeField, Range(-89.9f, 89.9f)] private float PitchLimitUpper = 89.9f;
    [SerializeField, Range(-359.9f, 359.9f)] private float YawLimitLower = -359.9f;
    [SerializeField, Range(-359.9f, 359.9f)] private float YawLimitUpper = 359.9f;

    public bool bIsPitchIgnored = false;
    public bool bIsYawIgnored = false;

    [Min(0.0f)]
    public float InterpolateDuration = 0.1f;

    private float PullPct = 0.0f;

    [Min(0)]
    public float MaxPullDistance = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        LimitPitch(ref OrbitPitch);
        LimitYaw(ref OrbitYaw);
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

        LimitPitch(ref OrbitPitch);
        LimitPitch(ref OrbitYaw);
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

        LimitPitch(ref OrbitPitch);
        LimitYaw(ref OrbitYaw);
    }

    public Vector3 GetOrbitDirection()
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

    public void SetOrbitDirection(Vector3 dir, bool bKeepPitch = false, bool bKeepYaw = false)
    {
        // Rearranging above equations, we get...
        float yawRadian = Mathf.Atan2(dir.z, dir.x);
        float pitchRadian = Mathf.Asin(dir.y);

        OrbitPitch = bKeepPitch ? OrbitPitch : pitchRadian * Mathf.Rad2Deg;
        OrbitYaw = bKeepYaw ? OrbitYaw : yawRadian * Mathf.Rad2Deg;

        LimitPitch(ref OrbitPitch);
        LimitYaw(ref OrbitYaw);
        //Debug.LogFormat("Pitch={0}, Yaw={1}", OrbitPitch, OrbitYaw);
    }

    public static Vector3 OrientationToDirection(float pitchDeg, float yawDeg)
    {
        Vector3 dir;

        float pitchRadian = Mathf.Deg2Rad * pitchDeg;
        float yawRadian = Mathf.Deg2Rad * yawDeg;

        dir.x = Mathf.Cos(yawRadian) * Mathf.Cos(pitchRadian);
        dir.z = Mathf.Sin(yawRadian) * Mathf.Cos(pitchRadian);
        dir.y = Mathf.Sin(pitchRadian);

        dir.Normalize();

        return dir;
    }

    public static void DirectionToOrientation(Vector3 dir, out float pitchDeg, out float yawDeg)
    {
        dir.Normalize();

        float yawRadian = Mathf.Atan2(dir.z, dir.x);
        float pitchRadian = Mathf.Asin(dir.y);

        pitchDeg = pitchRadian * Mathf.Rad2Deg;
        yawDeg = yawRadian * Mathf.Rad2Deg;
    }

    public bool IsValidOrientation(float pitchDeg, float yawDeg)
    {
        return pitchDeg >= PitchLimitLower && pitchDeg <= PitchLimitUpper && yawDeg >= YawLimitLower && yawDeg <= YawLimitUpper;
    }

    public void LimitPitch(ref float pitchDeg)
    {
        pitchDeg = Mathf.Clamp(pitchDeg, PitchLimitLower, PitchLimitUpper);
    }

    public void LimitYaw(ref float yawDeg)
    {
        yawDeg = Mathf.Clamp(yawDeg, YawLimitLower, YawLimitUpper);
    }
}
