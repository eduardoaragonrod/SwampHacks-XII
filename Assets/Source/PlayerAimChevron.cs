using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimChevron : MonoBehaviour
{
    [Header("Chevron Visual Settings")]

    [SerializeField] private Sprite customChevronSprite;
    [SerializeField] private float orbitRadius = 1.15f;
    [SerializeField] private float chevronScale = 0.85f;
    [SerializeField] private float rotationSmoothSpeed = 28f;
    [SerializeField] private int sortingOrder = 25;

    [Header("Juice & Element Color Sync")]
    [SerializeField] private bool tintWithCurrentElement = true;
    [SerializeField, Range(1f, 3f)] private float hdrGlowMultiplier = 1.8f;
    [SerializeField] private float hoverBobAmplitude = 0.08f;
    [SerializeField] private float hoverBobFrequency = 6f;
    [SerializeField] private float shootPunchDistance = 0.3f;

    [Header("Input Behavior")]
  
    [SerializeField] private bool alwaysShowOnGamepad = true;
    [SerializeField] private float stickDeadzone = 0.15f;

    private Transform _chevronTransform;
    private SpriteRenderer _chevronRenderer;
    private Vector2 _currentAimDir = Vector2.down;
    private bool _usingGamepad;
    private float _currentShootPunch;
    private float _targetAlpha = 1f;
    private Color _baseColor = Color.white;

    private void Awake()
    {
        CreateChevronVisualObject();
    }

    private void OnEnable()
    {
        ElementManager.OnChanged += OnElementChanged;
        if (ElementManager.Current.HasValue)
        {
            OnElementChanged(ElementManager.Current.Value);
        }
    }

    private void OnDisable()
    {
        ElementManager.OnChanged -= OnElementChanged;
    }

    private void LateUpdate()
    {
        UpdateAimDirectionAndDevice();
        UpdateShootPunchFeedback();
        UpdateChevronTransformAndVisuals();
    }

    private void UpdateAimDirectionAndDevice()
    {
        if (Gamepad.current != null)
        {
            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            if (rightStick.sqrMagnitude >= stickDeadzone * stickDeadzone)
            {
                _usingGamepad = true;
                _currentAimDir = rightStick.normalized;
                _targetAlpha = 1f;
            }
            else if (_usingGamepad)
            {
                _targetAlpha = alwaysShowOnGamepad ? 0.85f : 0f;
            }
        }

        if (Mouse.current != null)
        {
            bool mouseMoved = Mouse.current.delta.ReadValue().sqrMagnitude > 1.5f;
            bool mouseClicked = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame;

            if (mouseMoved || mouseClicked || Gamepad.current == null)
            {
                _usingGamepad = false;
            }

            if (!_usingGamepad && Camera.main != null)
            {
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 toCursor = (Vector2)(mouseWorld - transform.position);
                if (toCursor.sqrMagnitude > 0.001f)
                {
                    _currentAimDir = toCursor.normalized;
                }
                _targetAlpha = 1f;
            }
        }
    }

    private void UpdateShootPunchFeedback()
    {
        bool firedThisFrame = false;

        if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame)
            firedThisFrame = true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            firedThisFrame = true;

        if (firedThisFrame)
        {
            _currentShootPunch = shootPunchDistance;
        }

        _currentShootPunch = Mathf.Lerp(_currentShootPunch, 0f, Time.unscaledDeltaTime * 16f);
    }

    private void UpdateChevronTransformAndVisuals()
    {
        if (_chevronTransform == null) return;

        float bobOffset = Mathf.Sin(Time.unscaledTime * hoverBobFrequency) * hoverBobAmplitude;
        float currentRadius = orbitRadius + bobOffset + _currentShootPunch;

        Vector3 targetWorldPos = transform.position + (Vector3)(_currentAimDir * currentRadius);
        _chevronTransform.position = targetWorldPos;

        float targetAngle = Mathf.Atan2(_currentAimDir.y, _currentAimDir.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        _chevronTransform.rotation = Quaternion.Slerp(_chevronTransform.rotation, targetRot, Time.unscaledDeltaTime * rotationSmoothSpeed);

        float scaleBoost = 1f + (_currentShootPunch / Mathf.Max(0.01f, shootPunchDistance)) * 0.35f;
        _chevronTransform.localScale = Vector3.one * (chevronScale * scaleBoost);

        if (_chevronRenderer != null)
        {
            Color c = _baseColor * hdrGlowMultiplier;
            c.a = Mathf.Lerp(_chevronRenderer.color.a, _targetAlpha, Time.unscaledDeltaTime * 15f);
            _chevronRenderer.color = c;
        }
    }

    private void OnElementChanged(Element element)
    {
        if (!tintWithCurrentElement) return;

        var data = ElementManager.GetData(element);
        if (data != null)
        {
            _baseColor = Color.Lerp(data.Color, Color.white, 0.25f);
        }
    }

    private void CreateChevronVisualObject()
    {
        GameObject chevronObj = new GameObject("AimChevronIndicator");
        chevronObj.transform.SetParent(transform, false);
        _chevronTransform = chevronObj.transform;

        _chevronRenderer = chevronObj.AddComponent<SpriteRenderer>();
        _chevronRenderer.sortingOrder = sortingOrder;
        _chevronRenderer.sprite = customChevronSprite != null ? customChevronSprite : GenerateProceduralChevronSprite();
    }

    private Sprite GenerateProceduralChevronSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        float centerY = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dy = Mathf.Abs(y - centerY);

                float outerChevronX = 26f - dy * 1.15f;
                bool isOuterChevron = Mathf.Abs(x - outerChevronX) <= 2.2f && dy <= 11f;

                float innerChevronX = 17f - dy * 1.15f;
                bool isInnerChevron = Mathf.Abs(x - innerChevronX) <= 1.5f && dy <= 6.5f;

                if (isOuterChevron)
                {
                    pixels[y * size + x] = Color.white;
                }
                else if (isInnerChevron)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 0.75f);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}