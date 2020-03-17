using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CustomTrackableEventManager : ImageTargetBehaviour
{

    public

    // Start is called before the first frame update
    void Start()
    {
        RegisterOnTrackableStatusChanged(statusChange =>
        {
            GameObject runtimeGameObject = GameObject.Find("CustomImageTargetRuntimeGameObject");
            CustomTrackableEventHandler evHandler = runtimeGameObject.GetComponent<CustomTrackableEventHandler>();
            evHandler.TrackableStateChange(statusChange.NewStatus);
        });
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
