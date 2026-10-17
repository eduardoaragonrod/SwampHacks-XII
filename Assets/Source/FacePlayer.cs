using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    public enum FacingMode
    {
        Rotate360,     
        FlipHorizontal
    }

    [Header("Facing Mode")]
    [SerializeField] private FacingMode mode = FacingMode.Rotate360;

    [Header("360 Rotation Settings")]
    [Tooltip("Adjusts for the angle the sprite was drawn at. Use -135 for the diagonal Moth sprite, -90 if drawn facing straight Up, 0 if drawn facing Right.")]
    [SerializeField] private float angleOffset = -135f;
    [SerializeField] private float turnSpeed = 720f;

    [Header("Horizontal Flip Settings")]
    [SerializeField] private bool spriteDrawnFacingLeft = false;

    private Transform _player;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            var playerMove = FindFirstObjectByType<CharacterMovement>();
            if (playerMove != null) _player = playerMove.transform;
        }
    }

    private void LateUpdate()
    {
        if (_player == null) return;

        Vector2 direction = _player.position - transform.position;
        if (direction.sqrMagnitude < 0.001f) return;

        if (mode == FacingMode.Rotate360)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                turnSpeed * Time.deltaTime
            );
        }
        else if (mode == FacingMode.FlipHorizontal && _spriteRenderer != null)
        {
            if (Mathf.Abs(direction.x) > 0.05f)
            {
                bool targetIsLeft = direction.x < 0f;
                _spriteRenderer.flipX = spriteDrawnFacingLeft ? !targetIsLeft : targetIsLeft;
            }
        }
    }
}