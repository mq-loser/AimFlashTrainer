using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float mouseSensitivity = 100f;
    public Camera playerCamera;
    public float gravity = -9.81f;

    CharacterController controller;
    float verticalVelocity;
    float cameraPitch;
    bool cursorLocked;
    bool suppressShootThisFrame;
    TrainingSession session;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        session = TrainingSession.Instance;
        SetCursorLock(false);
    }

    void Update()
    {
        suppressShootThisFrame = false;
        HandleCursorLock();

        Move();

        if (!cursorLocked || !Application.isFocused)
        {
            return;
        }

        Look();
        Shoot();
    }

    void HandleCursorLock()
    {
        if (!Application.isFocused)
        {
            if (cursorLocked)
            {
                SetCursorLock(false);
            }
            return;
        }

        if (!cursorLocked && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            SetCursorLock(true);
            suppressShootThisFrame = true;
        }

        if (cursorLocked && Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }

        if (session != null && session.isFinished && Input.GetKeyDown(KeyCode.R))
        {
            session.ResetSession();
            if (cursorLocked)
            {
                session.StartSession();
            }
        }
    }

    void SetCursorLock(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        if (session != null)
        {
            if (locked)
            {
                session.StartSession();
                session.SetPaused(false);
            }
            else
            {
                session.SetPaused(true);
            }
        }
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
        playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move *= moveSpeed;

        if (controller.isGrounded)
        {
            verticalVelocity = 0f;
        }
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    void Shoot()
    {
        if (suppressShootThisFrame) return;
        if (session != null && (!session.isRunning || session.isPaused || session.isFinished)) return;
        if (!Input.GetButtonDown("Fire1")) return;

        session?.RegisterShot();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            var target = hit.collider.GetComponent<Target>();
            if (target != null)
            {
                session?.RegisterHit();
                target.Hit();
            }
        }
    }
}
