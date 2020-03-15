using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CustomTrackableEventManager : MonoBehaviour, ITrackableEventHandler
{
    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        GameObject runtimeGameObject = GameObject.Find("CustomImageTargetRuntimeGameObject");
        CustomTrackableEventHandler evHandler = runtimeGameObject.GetComponent<CustomTrackableEventHandler>();
        evHandler.TrackableStateChange(newStatus);
    }

    public

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
