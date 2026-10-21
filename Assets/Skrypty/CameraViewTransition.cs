using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Camera))]
public sealed class CameraViewTransition : MonoBehaviour
{
    public Transform player;
    public Transform eyePoint;
    public TopDownCamera topDownCamera;
    public CharacterPanelUI characterPanel;
    public Renderer[] hideInFirstPerson;
    public float transitionDuration = 1.1f;
    public float firstPersonFov = 70f;
    public float mouseSensitivity = 0.12f;
    public float pitchLimit = 80f;
    public bool IsFirstPerson { get; private set; }
    public bool IsTransitioning { get; private set; }
    public bool MenuOpen => characterPanel != null && characterPanel.panel != null && characterPanel.panel.activeInHierarchy;
    Camera view;
    Vector3 startPosition, offset;
    Quaternion startRotation;
    float startFov, topDownFov, elapsed, yaw, pitch, oldNearClip;
    bool targetFirstPerson, wasTopDownEnabled, avatarHidden;
    ShadowCastingMode[] originalShadowModes;
    CursorLockMode originalLock;
    bool originalVisible;
    bool previousMenuOpen;

    void Awake()
    {
        view = GetComponent<Camera>();
        if (topDownCamera == null) topDownCamera = GetComponent<TopDownCamera>();
        topDownFov = view.fieldOfView;
        oldNearClip = view.nearClipPlane;
        originalLock = Cursor.lockState;
        originalVisible = Cursor.visible;
        if (hideInFirstPerson == null) hideInFirstPerson = new Renderer[0];
        originalShadowModes = new ShadowCastingMode[hideInFirstPerson.Length];
        for (int i = 0; i < hideInFirstPerson.Length; i++)
            if (hideInFirstPerson[i] != null) originalShadowModes[i] = hideInFirstPerson[i].shadowCastingMode;
    }

    public void ToggleView()
    {
        if (IsTransitioning || player == null || eyePoint == null || topDownCamera == null) return;
        targetFirstPerson = !IsFirstPerson;
        if (targetFirstPerson)
        {
            offset = topDownCamera.offset;
            wasTopDownEnabled = topDownCamera.enabled;
            topDownFov = view.fieldOfView;
            yaw = player.eulerAngles.y;
            pitch = 0;
            view.nearClipPlane = Mathf.Min(oldNearClip, 0.05f);
        }
        topDownCamera.enabled = false;
        startPosition = transform.position;
        startRotation = transform.rotation;
        startFov = view.fieldOfView;
        elapsed = 0;
        IsTransitioning = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsFirstPerson || IsTransitioning || MenuOpen || player == null) return;
        // Escape releases the mouse; a click in the Game view captures it again.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }
        if (Mouse.current == null) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
        player.rotation = Quaternion.Euler(0, yaw, 0);
    }

    void LateUpdate()
    {
        if (player == null || eyePoint == null) return;
        if (IsTransitioning)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, transitionDuration));
            float blend = t * t * (3f - 2f * t);
            Vector3 destination = targetFirstPerson ? eyePoint.position : player.position + offset;
            Quaternion rotation = targetFirstPerson ? Quaternion.Euler(pitch, yaw, 0)
                : Quaternion.LookRotation(player.position + Vector3.up * 1.5f - destination);
            transform.SetPositionAndRotation(Vector3.Lerp(startPosition, destination, blend),
                Quaternion.Slerp(startRotation, rotation, blend));
            view.fieldOfView = Mathf.Lerp(startFov, targetFirstPerson ? firstPersonFov : topDownFov, blend);
            HideAvatar(Vector3.Distance(transform.position, eyePoint.position) < 0.65f);
            if (t >= 1)
            {
                IsFirstPerson = targetFirstPerson;
                IsTransitioning = false;
                HideAvatar(IsFirstPerson);
                if (!IsFirstPerson)
                {
                    view.nearClipPlane = oldNearClip;
                    topDownCamera.enabled = wasTopDownEnabled;
                }
                ApplyCursor();
            }
        }
        else if (IsFirstPerson)
        {
            transform.SetPositionAndRotation(eyePoint.position, Quaternion.Euler(pitch, yaw, 0));
            if (previousMenuOpen && !MenuOpen) ApplyCursor();
            if (MenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!Cursor.visible) Cursor.lockState = CursorLockMode.Locked;
        }
        previousMenuOpen = MenuOpen;
    }

    public void ApplyCursor()
    {
        Cursor.lockState = IsFirstPerson && !MenuOpen ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !IsFirstPerson || MenuOpen;
    }

    void HideAvatar(bool hide)
    {
        if (avatarHidden == hide) return;
        avatarHidden = hide;
        for (int i = 0; i < hideInFirstPerson.Length; i++)
            if (hideInFirstPerson[i] != null)
                hideInFirstPerson[i].shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : originalShadowModes[i];
    }

    void OnDisable()
    {
        HideAvatar(false);
        if (view != null) view.nearClipPlane = oldNearClip;
        if ((IsFirstPerson || IsTransitioning) && topDownCamera != null) topDownCamera.enabled = wasTopDownEnabled;
        IsTransitioning = false;
        IsFirstPerson = false;
        Cursor.lockState = originalLock;
        Cursor.visible = originalVisible;
    }
}
