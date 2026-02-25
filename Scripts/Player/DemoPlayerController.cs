using UnityEngine;
using EasySharedSpace;

/// <summary>
/// Simple player controller for demo scene.
/// Attach to player prefab.
/// </summary>
public class DemoPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;
    public float jumpForce = 5f;

    [Header("Visuals")]
    public Renderer playerRenderer;
    public TextMesh nameLabel;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.1f;
    public LayerMask groundMask;

    private SharedPlayer _sharedPlayer;
    private Rigidbody _rigidbody;
    private bool _isGrounded;

    private void Awake()
    {
        _sharedPlayer = GetComponent<SharedPlayer>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Only control local player
        if (_sharedPlayer != null && !_sharedPlayer.IsLocalPlayer)
        {
            enabled = false;
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            return;
        }

        // Setup local player
        SetupLocalPlayer();
    }

    private void SetupLocalPlayer()
    {
        // Add camera follow
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 1.6f, 0);
        Camera.main.transform.localRotation = Quaternion.identity;

        // Hide name label for self
        if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Only process for local player
        if (_sharedPlayer != null && !_sharedPlayer.IsLocalPlayer) return;

        // Ground check
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Jump
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Visual color update
        if (playerRenderer != null && _sharedPlayer != null)
        {
            playerRenderer.material.color = _sharedPlayer.PlayerColor.Value;
        }

        // Name label for remote players
        if (nameLabel != null && _sharedPlayer != null)
        {
            nameLabel.text = _sharedPlayer.PlayerName.Value;
            nameLabel.transform.LookAt(Camera.main.transform);
            nameLabel.transform.Rotate(0, 180, 0);
        }
    }

    private void FixedUpdate()
    {
        // Only process for local player
        if (_sharedPlayer != null && !_sharedPlayer.IsLocalPlayer) return;

        // Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * horizontal + transform.forward * vertical;
        _rigidbody.MovePosition(_rigidbody.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Rotation (mouse)
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.fixedDeltaTime;
        _rigidbody.MoveRotation(_rigidbody.rotation * Quaternion.Euler(Vector3.up * mouseX));
    }
}
