using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class FurnitureManager : MonoBehaviour
{
    public GameObject SpawnableFurniture;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private GameObject selectedObject;
    private bool isDragging = false;
    private bool isScaling = false;
    private bool planesDisabled = false;
    private float holdTimeThreshold = 0.5f;
    private float touchHoldTimer = 0f;
    private bool isHolding = false;

    private float rotationSpeed = 0.3f;  // Increased sensitivity for horizontal rotation
    private float verticalRotationSpeed = 0.2f;  // Increased vertical rotation sensitivity
    private float scaleSpeed = 0.0015f;

    private float tapResetDelay = 0.4f;
    private int tapCount = 0;

    private bool isDoubleTap = false;

    // ✅ Add the prefab names that should be treated as wall objects
    private HashSet<string> wallObjectNames = new HashSet<string>
    {
        "Canvas_Nat_01",          // A painting
        "canvas_modern_02",       // Another painting
        "wall_clock_01",          // A wall clock
        "mirror_oval_large",      // A mirror
        "painting_abstract_red"   // An abstract painting
    };

    public void SetSelectedPrefab(GameObject prefab)
    {
        SpawnableFurniture = prefab;
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
                    else if (tapCount == 2)
                    {
                        // Double tap detected, trigger hold for vertical rotation
                        isDoubleTap = true;
                    }
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

                        if (SpawnableFurniture != null && raycastManager.Raycast(touch.position, raycastHits, TrackableType.PlaneWithinPolygon))
                        {
                            Pose hitPose = raycastHits[0].pose;
                            TrackableId planeId = raycastHits[0].trackableId;
                            ARPlane hitPlane = planeManager.GetPlane(planeId);

                            GameObject newObj = null;

                            string prefabName = SpawnableFurniture.name.ToLower();

                            bool isWallItem = wallObjectNames.Contains(prefabName);

                            if (hitPlane != null && hitPlane.alignment == PlaneAlignment.Vertical && isWallItem)
                            {
                                // Wall item detected, so adjust placement and rotation
                                Debug.Log($"Wall item detected: {prefabName}, Plane alignment: {hitPlane.alignment}");

                                Vector3 wallNormal = hitPose.rotation * Vector3.back;
                                Vector3 spawnPos = hitPose.position + wallNormal * 0.01f;

                                // Adjust rotation to ensure the object is parallel to the wall
                                Quaternion rotation = Quaternion.LookRotation(wallNormal);
                                rotation *= Quaternion.Euler(0f, -90f, 0f);  // Adjust this for correct alignment with the wall

                                newObj = Instantiate(SpawnableFurniture, spawnPos, rotation);
                            }
                            else
                            {
                                newObj = Instantiate(SpawnableFurniture, hitPose.position, hitPose.rotation);
                            }

                            if (newObj != null)
                            {
                                newObj.tag = "PlacedObject";
                                selectedObject = newObj;

                                if (!planesDisabled)
                                {
                                    foreach (var plane in planeManager.trackables)
                                        plane.gameObject.SetActive(false);
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

                            // Only apply horizontal rotation for furniture and non-wall objects
                            if (!isDoubleTap && !wallObjectNames.Contains(selectedObject.name.ToLower()))
                            {
                                // Horizontal rotation (around Y-axis)
                                selectedObject.transform.Rotate(0, -delta.x * rotationSpeed, 0);
                            }

                            // Apply vertical rotation if double-tap is detected and it's a wall object
                            if (isDoubleTap && wallObjectNames.Contains(selectedObject.name.ToLower()))
                            {
                                // Invert the direction of vertical rotation: Move finger up rotates up, down rotates down
                                // Apply rotation only on the local X-axis of the object when it's a wall item
                                selectedObject.transform.Rotate(-delta.y * verticalRotationSpeed, 0, 0, Space.Self);
                            }
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    isHolding = false;
                    isDoubleTap = false; // Reset double-tap flag
                    break;
            }
        }

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

    public void SwitchFurniture(GameObject furniture)
    {
        SpawnableFurniture = furniture;
    }
}
