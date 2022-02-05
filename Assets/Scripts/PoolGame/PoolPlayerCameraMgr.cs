using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CameraViewParams
{
    public float FOV;
    public float NearZ;
    public float FarZ;

    public CameraViewParams(float fov, float nearz, float farz)
    {
        FOV = fov;
        NearZ = nearz;
        FarZ = farz;
    }
}

/* This component should exist only for the local player */
public class PoolPlayerCameraMgr : PlayerGameInfo
{
    [SerializeField] private GameObject TopDownCameraSpot;
    [SerializeField] private GameObject CueAimCameraSpot;

    [SerializeField] private CameraViewParams TopDownCameraView = new CameraViewParams(60.0f, 0.3f, 1000.0f);
    [SerializeField] private CameraViewParams CueAimCameraView  = new CameraViewParams(30.0f, 0.1f, 1000.0f);

    static string TOP_CAMERASPOT_NAME = "TopCameraSpot";
    static string CUE_CAMERASPOT_NAME = "CueCameraSpot";

    // Start is called before the first frame update
    void Start()
    {
        if(!TopDownCameraSpot)
        {
            TopDownCameraSpot = GameObject.Find(TOP_CAMERASPOT_NAME);
        }

        if(!CueAimCameraSpot)
        {
            CueAimCameraSpot = GameObject.Find(CUE_CAMERASPOT_NAME);
        }

        if(!CueAimCameraSpot || !TopDownCameraSpot)
        {
            Debug.LogWarning("TopDown or Cue camera spots not found, falling back to main camera postion");
        }

        EnableTopDownCamera();
    }

    // Update is called once per frame
    void Update()
    {
        if(CueAimCameraSpot != null && CueAimCameraSpot.activeSelf)
        {
            UpdateCueAimCamera();
        }
    }

    public void ToggleCamera()
    {
        if(TopDownCameraSpot.activeSelf)
        {
            EnableCueAimCamera();
        }
        else
        {
            EnableTopDownCamera();
        }
    }

    public bool IsTopDownCameraEnabled()
    {
        return TopDownCameraSpot != null && TopDownCameraSpot.activeSelf;
    }

    public void EnableTopDownCamera()
    {
        Camera.main.transform.SetParent(null);

        CueAimCameraSpot.SetActive(false);
        TopDownCameraSpot.SetActive(true);

        Camera.main.transform.position = TopDownCameraSpot.transform.position;
        Camera.main.transform.rotation = TopDownCameraSpot.transform.rotation;

        Camera.main.fieldOfView     = TopDownCameraView.FOV;
        Camera.main.nearClipPlane   = TopDownCameraView.NearZ;
        Camera.main.farClipPlane    = TopDownCameraView.FarZ;

        Camera.main.transform.SetParent(TopDownCameraSpot.transform);
    }

    public void EnableCueAimCamera()
    {
        Camera.main.transform.SetParent(null);

        CueAimCameraSpot.SetActive(true);
        TopDownCameraSpot.SetActive(false);

        Camera.main.transform.position = CueAimCameraSpot.transform.position;
        Camera.main.transform.rotation = CueAimCameraSpot.transform.rotation;

        Camera.main.fieldOfView     = CueAimCameraView.FOV;
        Camera.main.nearClipPlane   = CueAimCameraView.NearZ;
        Camera.main.farClipPlane    = CueAimCameraView.FarZ;

        Camera.main.transform.SetParent(CueAimCameraSpot.transform);
    }

    public void SetCueAimCamera(Vector3 position, Quaternion orientation)
    {
        //CueAimCamera.transform.position = position;
        //CueAimCamera.transform.rotation = orientation;
    }

    public void SetCueAimCamera(Vector3 position, Vector3 dir)
    {
        SetCueAimCamera(position, Quaternion.LookRotation(dir));
    }

    void UpdateCueAimCamera()
    {
        // Position ourselves in the forward direction of our cue
        PoolGamePlayer poolPlayer = PoolGameDirector.Instance.GetLocalPoolPlayer();
        if(poolPlayer != null)
        {
            if(poolPlayer.CueObject)
            {
                Vector3 localForwardDir = poolPlayer.CueObject.transform.forward;
                Vector3 localUpDir = poolPlayer.CueObject.transform.up;

                CueAimCameraSpot.transform.position = (poolPlayer.CueObject.transform.position + (localUpDir * 0.1f) - (localForwardDir * 1.0f));
                CueAimCameraSpot.transform.rotation = poolPlayer.CueObject.transform.rotation;
            }
        }
    }
}
