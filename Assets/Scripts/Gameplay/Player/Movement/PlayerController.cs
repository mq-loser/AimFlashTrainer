using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7f;

    [Header("Look (Valorant-style)")]
    [Tooltip("Same numeric value as Valorant in-game sensitivity. Example: 0.1")]
    public float valorantSensitivity = 0.1f;

    [Tooltip("Community standard: Valorant uses ~0.07 degrees per mouse count at sensitivity = 1.")]
    public float valorantYawDegreesPerCount = 0.07f;

    [Tooltip("Extra multiplier for Mouse X/Y input. Keep 1 for the default project settings.")]
    public float mouseInputMultiplier = 1f;

    public bool invertY = false;
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

        if (!cursorLocked && (WasMouseButtonPressedThisFrame(0) || WasMouseButtonPressedThisFrame(1)))
        {
            SetCursorLock(true);
            suppressShootThisFrame = true;
        }

        if (cursorLocked && WasKeyPressedThisFrame(KeyCode.Escape))
        {
            SetCursorLock(false);
        }

        if (session != null && session.isFinished && WasKeyPressedThisFrame(KeyCode.R))
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
        Vector2 mouseDelta = ReadMouseDelta() * mouseInputMultiplier;

        float yawDelta = mouseDelta.x * valorantSensitivity * valorantYawDegreesPerCount;
        float pitchDelta = mouseDelta.y * valorantSensitivity * valorantYawDegreesPerCount;

        if (invertY)
        {
            pitchDelta = -pitchDelta;
        }

        transform.Rotate(Vector3.up * yawDelta);

        cameraPitch -= pitchDelta;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
        playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    void Move()
    {
        Vector2 moveInput = ReadMoveInput();
        float x = moveInput.x;
        float z = moveInput.y;

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
        if (!WasPrimaryFirePressedThisFrame()) return;

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

    static bool WasMouseButtonPressedThisFrame(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        return button switch
        {
            0 => Mouse.current.leftButton.wasPressedThisFrame,
            1 => Mouse.current.rightButton.wasPressedThisFrame,
            2 => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false,
        };
#else
        return Input.GetMouseButtonDown(button);
#endif
    }

    static bool WasPrimaryFirePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetButtonDown("Fire1");
#endif
    }

    static bool WasKeyPressedThisFrame(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;

        return key switch
        {
            KeyCode.Escape => Keyboard.current.escapeKey.wasPressedThisFrame,
            KeyCode.R => Keyboard.current.rKey.wasPressedThisFrame,
            _ => false,
        };
#else
        return Input.GetKeyDown(key);
#endif
    }

    static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
    }

    static Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return Vector2.zero;

        float x = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

        float y = 0f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;

        return new Vector2(x, y);
#else
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif
    }
}
