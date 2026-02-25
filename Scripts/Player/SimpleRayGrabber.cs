using UnityEngine;

namespace EasySharedSpace
{
    /// <summary>
    /// Simple ray-based grabber for desktop/non-VR setups.
    /// Uses mouse/screen center to raycast and grab objects.
    /// </summary>
    public class SimpleRayGrabber : MonoBehaviour
    {
        [Header("Ray Settings")]
        [Tooltip("The camera to cast rays from")]
        public Camera playerCamera;

        [Tooltip("Max distance to grab objects")]
        public float grabRange = 3f;

        [Tooltip("Layer mask for grabbable objects")]
        public LayerMask grabbableLayers;

        [Header("Input")]
        [Tooltip("Input button name for grabbing")]
        public string grabButton = "Fire1";

        [Tooltip("Key to release object")]
        public KeyCode releaseKey = KeyCode.E;

        [Header("Visuals")]
        [Tooltip("Show grab ray in editor")]
        public bool showDebugRay = true;

        [Tooltip("Color of debug ray when can grab")]
        public Color canGrabColor = Color.green;

        [Tooltip("Color of debug ray when cannot grab")]
        public Color cannotGrabColor = Color.red;

        // State
        private SharedGrabbableObject _grabbedObject;
        private SharedGrabbableObject _hoveredObject;
        private Transform _grabPoint;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            // Create grab point
            GameObject grabPointObj = new GameObject("GrabPoint");
            _grabPoint = grabPointObj.transform;
            _grabPoint.SetParent(playerCamera.transform);
            _grabPoint.localPosition = Vector3.forward * grabRange * 0.5f;
        }

        private void Update()
        {
            if (_grabbedObject != null)
            {
                UpdateGrabbed();
            }
            else
            {
                UpdateHover();
                
                if (Input.GetButtonDown(grabButton))
                {
                    TryGrab();
                }
            }
        }

        private void UpdateHover()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            SharedGrabbableObject hitObject = null;

            if (Physics.Raycast(ray, out hit, grabRange, grabbableLayers))
            {
                hitObject = hit.collider.GetComponent<SharedGrabbableObject>();
                
                if (hitObject != null && !hitObject.isGrabbable)
                {
                    hitObject = null;
                }
            }

            // Update hover state
            if (hitObject != _hoveredObject)
            {
                if (_hoveredObject != null)
                {
                    _hoveredObject.SetHovered(false);
                }

                _hoveredObject = hitObject;

                if (_hoveredObject != null)
                {
                    _hoveredObject.SetHovered(true);
                }
            }

            // Update debug ray
            if (showDebugRay)
            {
                Color rayColor = (hitObject != null) ? canGrabColor : cannotGrabColor;
                Debug.DrawRay(ray.origin, ray.direction * grabRange, rayColor);
            }
        }

        private void TryGrab()
        {
            if (_hoveredObject == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            Vector3 grabPoint = _hoveredObject.transform.position;
            if (Physics.Raycast(ray, out hit, grabRange, grabbableLayers))
            {
                if (hit.collider.GetComponent<SharedGrabbableObject>() == _hoveredObject)
                {
                    grabPoint = hit.point;
                }
            }

            if (_hoveredObject.TryGrab(_grabPoint, grabPoint))
            {
                _grabbedObject = _hoveredObject;
                _hoveredObject = null;
            }
        }

        private void UpdateGrabbed()
        {
            // Update grab point position based on camera
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            _grabPoint.position = ray.GetPoint(grabRange * 0.5f);
            _grabPoint.rotation = playerCamera.transform.rotation;

            // Check for release
            if (Input.GetButtonUp(grabButton) || Input.GetKeyDown(releaseKey))
            {
                Release();
            }

            // Visual feedback
            if (showDebugRay)
            {
                Debug.DrawLine(playerCamera.transform.position, _grabPoint.position, Color.yellow);
            }
        }

        private void Release()
        {
            if (_grabbedObject == null) return;

            // Calculate throw velocity based on camera movement
            Vector3 throwVelocity = Vector3.zero;
            // Could add mouse velocity here for throwing

            _grabbedObject.Release(throwVelocity);
            _grabbedObject = null;
        }
    }
}
