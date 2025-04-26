using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlantManager : MonoBehaviour
{
    public GameObject[] flowers;
    public XROrigin xROrigin;

   public ARRaycastManager raycastManager;

   public ARPlaneManager planeManager;

   private List<ARRaycastHit> raycastHits=new List<ARRaycastHit>();

    private void Update()
    {
        if(Input.GetTouch(0).phase==TouchPhase.Began){
            bool collision=raycastManager.Raycast(Input.mousePosition,raycastHits,TrackableType.PlaneWithinPolygon);
            if (collision )
            {
                GameObject _object = Instantiate(flowers[Random.Range(0,flowers.Length-1)]);
                _object.transform.position = raycastHits[0].pose.position;
            }
            foreach(var planes in planeManager.trackables){
                planes.gameObject.SetActive(false);
            }
            planeManager.enabled=false;
        }

    }
}
