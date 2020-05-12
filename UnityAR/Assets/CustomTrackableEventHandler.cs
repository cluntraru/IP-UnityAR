using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Vuforia;
using Assets.Utils;

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
    private string[] textureIds = new string[mTextureCount] { "1", "2" };

    private PIXEL_FORMAT mPixelFormat;
    private bool mServerFoundTrackable = false;

    private bool mIsVuforiaStarted = false;
    private bool mIsFirstValidUpdate = true;


    IEnumerator GetIsTextureChanged(int textureIdx)
    {
        while (true)
        {
            string serverTextureId = textureIds[textureIdx];
            string url = "http://" + mServerIp + ":" + mServerPort + "/app/poll/" + serverTextureId;
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError("Failed to poll for texture change");
            }
            else
            {
                string downloadText = www.downloadHandler.text;
                mPollResults[textureIdx] = (downloadText == "1");
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator GetTexture(int textureIdx)
    {
        mIsTextureDownloaded[textureIdx] = false;
        string serverTextureId = textureIds[textureIdx];
        string url = "http://" + mServerIp + ":" + mServerPort + "/app/get/" + serverTextureId;
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Failed to retrieve trackable texture from server");
        }
        else
        {
            mTextures[textureIdx] = ((DownloadHandlerTexture)www.downloadHandler).texture;
            // Debug
            File.WriteAllBytes(Application.persistentDataPath + "/test.jpg", mTextures[textureIdx].EncodeToJPG());
        }

        mIsTextureDownloaded[textureIdx] = true;
    }

    IEnumerator PutCameraImage()
    {
        while (!mServerFoundTrackable)
        {
            bool isCameraImageValid = false;
            Vuforia.Image cameraImage = null;
            while (!isCameraImageValid)
            {
                cameraImage = CameraDevice.Instance.GetCameraImage(mPixelFormat);
                isCameraImageValid = cameraImage.Width > 0 && cameraImage.Height > 0;

                if (!isCameraImageValid)
                {
                    yield return new WaitForSeconds(.2f);
                }
            }
            Texture2D cameraTexture = new Texture2D(cameraImage.Width, cameraImage.Height);
            cameraImage.CopyToTexture(cameraTexture, false);

#if UNITY_EDITOR
            cameraTexture.Point(cameraTexture.width / 3 < 1440 ? cameraTexture.width/3 : 1440, cameraTexture.height / 3 < 1080 ? cameraTexture.height/3 : 1080);
#else
            cameraTexture.Point(cameraTexture.width /2, cameraTexture.height / 2);
            cameraTexture.RotateTexture(true);
#endif
            byte[] data = cameraTexture.EncodeToPNG();
            string url = "http://" + mServerIp + ":" + mServerPort + "/app/put";
            UnityWebRequest www = UnityWebRequest.Put(url, data);
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError("Failed to upload camera image");
            }
            else
            {
                Debug.Log("Successfully uploaded camera image");

            }
            yield return new WaitForSeconds(0.2f);

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
        tbh.RegisterOnTrackableStatusChanged(statusChange =>
        {
            if (statusChange.NewStatus.Equals(TrackableBehaviour.Status.NO_POSE))
            {
                mServerFoundTrackable = false;
                StartCoroutine(PutCameraImage());
            }
            else if (statusChange.NewStatus.Equals(TrackableBehaviour.Status.TRACKED))
            {
                mServerFoundTrackable = true;
            }
        });
        // Create quad to display image
        GameObject childQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        MeshRenderer meshRenderer = childQuad.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        meshRenderer.sharedMaterial.SetTexture("_MainTex", overlayTexture);

        childQuad.transform.parent = tbh.gameObject.transform;
        childQuad.transform.localScale = new Vector3(overlayTexture.width/(float)overlayTexture.height, 1f, 1f);
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
