using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float verticalSpeed = 4f;

    [Header("Look")]
    [Tooltip("Базовая чувствительность мыши. Множитель из GameSettings.MouseSensitivity применяется поверх.")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private float _yaw;
    private float _pitch;

    private void Start()
    {
        var euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;

        if (lockCursorOnStart)
            LockCursor(true);
    }

    private void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (PauseMenu.IsPaused)
            return;

        if (Input.GetMouseButtonDown(0))
            LockCursor(true);

        if (Cursor.lockState == CursorLockMode.Locked)
            Look();

        Move();
    }

    private void Look()
    {
        float sens = mouseSensitivity * GameSettings.MouseSensitivity;
        _yaw += Input.GetAxis("Mouse X") * sens;
        _pitch -= Input.GetAxis("Mouse Y") * sens;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void Move()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z) * speed;

        float y = 0f;
        if (Input.GetKey(KeyCode.Space)) y += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) y -= 1f;
        move += Vector3.up * (y * verticalSpeed);

        transform.position += move * Time.deltaTime;
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

