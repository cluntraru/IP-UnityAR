﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Vuforia;

public class CustomTrackableEventHandler : MonoBehaviour
{
    [SerializeField]
    public string mServerIp = "127.0.0.1";
    [SerializeField]
    public string mServerPort = "8000";

    private const int mTextureCount = 2;
    // 0 is trackable, 1 is overlay
    private Texture2D[] mTextures = new Texture2D[mTextureCount];
    private bool[] mPollResults = new bool[mTextureCount];
    private bool[] mIsTextureDownloaded = new bool[mTextureCount];

    private PIXEL_FORMAT mPixelFormat;
    private bool mServerFoundTrackable = false;

    private bool mIsVuforiaStarted = false;
    private bool mIsFirstValidUpdate = true;

    public void TrackableStateChange(TrackableBehaviour.Status newStatus)
    {
        if (newStatus.Equals(TrackableBehaviour.Status.NO_POSE))
        {
            mServerFoundTrackable = false;
            StartCoroutine(PutCameraImage());
        }
    }

    IEnumerator GetIsTextureChanged(int textureIdx)
    {
        while (true)
        {
            int serverTextureId = textureIdx + 1;
            string url = "http://" + mServerIp + ":" + mServerPort + "/app/poll/" + serverTextureId;
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError("Failed to poll for texture change");
            } else
            {
                string downloadText = www.downloadHandler.text;
                mPollResults[textureIdx] = (downloadText == "1");
            }

            yield return new WaitForSeconds(.2f);
        }
    }

    IEnumerator GetTexture(int textureIdx)
    {
        mIsTextureDownloaded[textureIdx] = false;
        int serverTextureId = textureIdx + 1;
        string url = "http://" + mServerIp + ":" + mServerPort + "/app/get/" + serverTextureId;
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Failed to retrieve trackable texture from server");
        } else
        {
           mTextures[textureIdx] = ((DownloadHandlerTexture) www.downloadHandler).texture;
            // Debug
           File.WriteAllBytes(Application.persistentDataPath + "/test.jpg", mTextures[textureIdx].EncodeToJPG());
        }

        mIsTextureDownloaded[textureIdx] = true;
    }

    IEnumerator PutCameraImage()
    {
        //while (!mServerFoundTrackable)
        while (true)
        {
            Vuforia.Image cameraImage = CameraDevice.Instance.GetCameraImage(mPixelFormat);
            byte[] data = cameraImage.Pixels;
    
            string url = "http://" + mServerIp + ":" + mServerPort + "/app/put";
            UnityWebRequest www = UnityWebRequest.Put(url, data);
            yield return www.SendWebRequest();
    
            if (www.isNetworkError)
            {
                Debug.LogError("Failed to upload camera image");
            } else
            {
                Debug.Log("Successfully uploaded camera image");
            }
    
            Debug.Log("Hello");
            yield return new WaitForSeconds(.2f);
        }
    }

    void OnVuforiaStart()
    {
        // Camera init
#if UNITY_EDITOR
        mPixelFormat = PIXEL_FORMAT.GRAYSCALE; // unity
#else
        mPixelFormat = PIXEL_FORMAT.RGB888; // mobile
#endif

        CameraDevice.Instance.SetFrameFormat(mPixelFormat, true);
        mIsVuforiaStarted = true;
    }

    void Start()
    {
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStart);

        for (int i = 0; i < mTextureCount; ++i)
        {
            StartCoroutine(GetIsTextureChanged(i));
        }
    }

    void MakeTrackable(Texture2D trackableTexture, Texture2D overlayTexture)
    {
        ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        objectTracker.DestroyAllDataSets(true);
        DataSet dataSet = objectTracker.CreateDataSet();
        objectTracker.RuntimeImageSource.SetImage(trackableTexture, 0.16f, "newTargetImage");

        DataSetTrackableBehaviour tbh = dataSet.CreateTrackable(objectTracker.RuntimeImageSource, "NewTrackableObject");
        tbh.gameObject.AddComponent<DefaultTrackableEventHandler>();
        tbh.gameObject.AddComponent<TurnOffBehaviour>();
        tbh.gameObject.AddComponent<CustomTrackableEventManager>();

        // Create quad to display image
        GameObject childQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        MeshRenderer meshRenderer = childQuad.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        meshRenderer.sharedMaterial.SetTexture("_MainTex", overlayTexture);

        childQuad.transform.parent = tbh.gameObject.transform;
        childQuad.transform.localScale = new Vector3(1f, 0.5f, 1f);
        childQuad.transform.Rotate(new Vector3(90f, 0f, 0f));
        // End quad

        objectTracker.ActivateDataSet(dataSet);
    }

    void Update()
    {
        if (!mIsVuforiaStarted)
        {
            return;
        }

        // Executes once - at the beginning of runtime, server needs to be sent image
        if (mIsFirstValidUpdate)
        {
            mServerFoundTrackable = false;
            StartCoroutine(PutCameraImage());

            mIsFirstValidUpdate = false;
        }

        for (int i = 0; i < mTextureCount - 1; i += 2)
        {
            if (mPollResults[i] && mPollResults[i + 1])
            {
                mServerFoundTrackable = true;
                mPollResults[i] = false;
                mPollResults[i + 1] = false;

                StartCoroutine(GetTexture(i));
                StartCoroutine(GetTexture(i + 1));
            }

            if (mIsTextureDownloaded[i] && mIsTextureDownloaded[i + 1])
            {
                mIsTextureDownloaded[i] = false;
                mIsTextureDownloaded[i + 1] = false;
                MakeTrackable(mTextures[i], mTextures[i + 1]);
            }
        }
    }
}
