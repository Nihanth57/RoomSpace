using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ARWallObjectManager : MonoBehaviour
{
    public GameObject SpawnableObject;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private GameObject selectedObject;
    private bool isDragging = false;
    private bool isScaling = false;
    private bool planesDisabled = false;
    private float holdTimeThreshold = 1.0f;
    private float touchHoldTimer = 0f;
    private bool isHolding = false;

    private float rotationSpeed = 0.2f;
    private float scaleSpeed = 0.0015f;

    private float tapResetDelay = 0.5f;
    private int tapCount = 0;

    public void SetSelectedPrefab(GameObject prefab)
    {
        SpawnableObject = prefab;
    }

    void Update()
    {
        if (Input.touchCount == 0) return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            Ray ray = Camera.main.ScreenPointToRay(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    tapCount++;
                    if (tapCount == 1)
                        Invoke("ResetTapCount", tapResetDelay);
                    else if (tapCount == 3)
                    {
                        TryTripleTapDestroy(touch.position);
                        tapCount = 0;
                        return;
                    }

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (hit.collider.CompareTag("PlacedObject"))
                        {
                            selectedObject = hit.collider.gameObject;
                            isHolding = true;
                            touchHoldTimer = 0f;
                        }
                        else
                        {
                            selectedObject = null;
                            isHolding = false;
                        }
                    }
                    else
                    {
                        selectedObject = null;
                        isHolding = false;

                        // Wall & Ceiling detection
                        if (raycastManager.Raycast(touch.position, raycastHits, TrackableType.PlaneWithinPolygon))
                        {
                            Pose hitPose = raycastHits[0].pose;
                            ARPlane plane = planeManager.GetPlane(raycastHits[0].trackableId);
                            Vector3 normal = plane.transform.forward;

                            // Check if it's a wall or ceiling
                            if (Vector3.Dot(plane.transform.up, Vector3.up) < 0.5f || plane.transform.up.y < 0)
                            {
                                GameObject newObj = Instantiate(
                                    SpawnableObject,
                                    hitPose.position,
                                    Quaternion.LookRotation(-normal)
                                );

                                newObj.transform.position += normal * 0.01f; // small forward offset
                                newObj.tag = "PlacedObject";
                                selectedObject = newObj;

                                if (!planesDisabled)
                                {
                                    foreach (var p in planeManager.trackables)
                                        p.gameObject.SetActive(false);
                                    planeManager.enabled = false;
                                    planesDisabled = true;
                                }
                            }
                        }
                    }
                    break;

                case TouchPhase.Stationary:
                    if (isHolding)
                    {
                        touchHoldTimer += Time.deltaTime;
                        if (touchHoldTimer >= holdTimeThreshold)
                            isDragging = true;
                    }
                    break;

                case TouchPhase.Moved:
                    if (selectedObject != null)
                    {
                        if (isDragging && !isScaling)
                        {
                            if (raycastManager.Raycast(touch.position, raycastHits, TrackableType.PlaneWithinPolygon))
                            {
                                Pose hitPose = raycastHits[0].pose;
                                float dragSpeed = 10f * Time.deltaTime;
                                selectedObject.transform.position = Vector3.Lerp(
                                    selectedObject.transform.position,
                                    hitPose.position,
                                    dragSpeed
                                );
                            }
                        }
                        else if (!isDragging && !isScaling)
                        {
                            Vector2 delta = touch.deltaPosition;
                            selectedObject.transform.Rotate(0, -delta.x * rotationSpeed, 0);
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    isHolding = false;
                    break;
            }
        }

        // Pinch to scale
        if (Input.touchCount == 2 && selectedObject != null)
        {
            isScaling = true;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevT0 = t0.position - t0.deltaPosition;
            Vector2 prevT1 = t1.position - t1.deltaPosition;

            float prevMag = (prevT0 - prevT1).magnitude;
            float currentMag = (t0.position - t1.position).magnitude;

            float diff = currentMag - prevMag;

            Vector3 newScale = selectedObject.transform.localScale + Vector3.one * diff * scaleSpeed;
            newScale = Vector3.Max(newScale, Vector3.one * 0.1f);
            newScale = Vector3.Min(newScale, Vector3.one * 5f);
            selectedObject.transform.localScale = newScale;
        }
        else
        {
            isScaling = false;
        }
    }

    void ResetTapCount()
    {
        tapCount = 0;
    }

    void TryTripleTapDestroy(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("PlacedObject"))
            {
                Destroy(hit.collider.gameObject);
                selectedObject = null;
            }
        }
    }

    public void SwitchDecoration(GameObject obj)
    {
        SpawnableObject = obj;
    }
}
