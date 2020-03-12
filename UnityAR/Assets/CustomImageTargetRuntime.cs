using System.Collections;
using System.Collections.Generic;
using System.IO;
using Vuforia;
using UnityEngine;

public class CustomImageTargetRuntime : MonoBehaviour
{
    private ObjectTracker mObjectTracker = null;
    private Texture2D mTex = null;
    private DataSet mDataSet = null;
    // Start is called before the first frame update
    void Start()
    {
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(MakeTrackable);
    }

    // Update is called once per frame
    void Update()
    {
    }

    void MakeTrackable()
    {
        string filePath = Application.persistentDataPath + "/sample.jpg";
        byte[] fileData = null;
        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(fileData);

        File.WriteAllBytes(Application.persistentDataPath + "/test.jpg", texture.EncodeToJPG());

        mObjectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        mDataSet = mObjectTracker.CreateDataSet();
        mObjectTracker.RuntimeImageSource.SetImage(texture, 0.16f, "newTargetImage");

        DataSetTrackableBehaviour tbh = mDataSet.CreateTrackable(mObjectTracker.RuntimeImageSource, "NewTrackableObject");
        tbh.gameObject.AddComponent<DefaultTrackableEventHandler>();
        tbh.gameObject.AddComponent<TurnOffBehaviour>();

        // Create quad to display image
        GameObject childQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        MeshRenderer meshRenderer = childQuad.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        meshRenderer.sharedMaterial.SetTexture("_MainTex", texture);

        childQuad.transform.parent = tbh.gameObject.transform;
        childQuad.transform.localScale = new Vector3(1f, 0.5f, 1f);
        childQuad.transform.Rotate(new Vector3(90f, 0f, 0f));
        // End quad

        mObjectTracker.ActivateDataSet(mDataSet);
    }
}
