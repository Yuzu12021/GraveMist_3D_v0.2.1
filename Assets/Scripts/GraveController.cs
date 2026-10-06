
using System;
using UnityEngine;

public enum GraveFaceResult
{
    Front,      // 赤
    Back,       // 青
    Side,       // 黄
    Vertical,   // 緑
    Reverse     // 逆立ち
}

public class GraveController : MonoBehaviour
{
    // =========================================================
    // Inspector
    // =========================================================

    [Header("Stop Detection")]
    public float velocityThreshold = 0.05f;
    public float stopTime = 0.3f;
    public float maxMovingTime = 8f;

    [Header("Fall Detection")]
    public float fallYThreshold = -3f;

    [SerializeField]
    private Transform judgePivot;


    // =========================================================
    // Runtime State
    // =========================================================

    private Rigidbody rb;

    private float stillTimer = 0f;
    private float movingTimer = 0f;

    private bool hasStopped = false;
    private bool isInvalid = false;
    private bool hasBouncedOnBoard = false;


    // =========================================================
    // External State / Events
    // =========================================================

    public bool hasGraveSupporting = false;

    public event Action<GraveController> OnStopped;


    // =========================================================
    // Unity Lifecycle
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (hasStopped)
        {
            return;
        }

        movingTimer += Time.fixedDeltaTime;

        // -----------------------------------------------------
        // Board外への落下判定
        // -----------------------------------------------------

        if (!isInvalid && transform.position.y < fallYThreshold)
        {
            isInvalid = true;

            StopGrave(true);
            return;
        }

        // -----------------------------------------------------
        // 通常停止判定
        // -----------------------------------------------------

        bool isBelowVelocityThreshold =
            rb.linearVelocity.magnitude < velocityThreshold &&
            rb.angularVelocity.magnitude < velocityThreshold;

        if (isBelowVelocityThreshold)
        {
            stillTimer += Time.fixedDeltaTime;

            if (stillTimer >= stopTime)
            {
                StopGrave(false);
                return;
            }
        }
        else
        {
            stillTimer = 0f;
        }

        // -----------------------------------------------------
        // 停止フェイルセーフ
        // -----------------------------------------------------

        if (movingTimer >= maxMovingTime)
        {
            Debug.LogWarning(
                $"[Grave] 停止判定が {maxMovingTime:F1} 秒以内に完了しなかったため強制停止"
            );

            StopGrave(true);
        }
    }

    private void StopGrave(bool makeKinematic)
    {
        hasStopped = true;

        if (makeKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        OnStopped?.Invoke(this);
    }
    // =========================================================
    // Collision
    // =========================================================

    private void OnCollisionEnter(Collision collision)
    {
        if (hasBouncedOnBoard)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Board"))
        {
            hasBouncedOnBoard = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE("grave_bounce");
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Grave以外との接触は対象外
        if (!collision.gameObject.CompareTag("Grave"))
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            // 接触法線が上向きなら、
            // 相手のGraveが下側にいると判定
            float dot = Vector3.Dot(contact.normal, Vector3.up);

            if (dot > 0.5f)
            {
                hasGraveSupporting = true;
                return;
            }
        }
    }


    // =========================================================
    // Board State
    // =========================================================

    public bool IsOutOfBoard()
    {
        // 現行挙動維持のため -3f をそのまま使用
        return transform.position.y < -3f;
    }

    public bool IsInvalid()
    {
        return isInvalid;
    }


    // =========================================================
    // Face Result
    // =========================================================

    public GraveFaceResult GetResult()
    {
        Vector3 worldUp = Vector3.up;

        Vector3 up = judgePivot.up;
        Vector3 right = judgePivot.right;
        Vector3 forward = judgePivot.forward;

        float dotUp = Vector3.Dot(up, worldUp);
        float dotRight = Vector3.Dot(right, worldUp);
        float dotForward = Vector3.Dot(forward, worldUp);

        float absUp = Mathf.Abs(dotUp);
        float absRight = Mathf.Abs(dotRight);
        float absForward = Mathf.Abs(dotForward);

        Debug.Log(
            $"[GraveJudge] " +
            $"Up={dotUp:F2} / " +
            $"Right={dotRight:F2} / " +
            $"Forward={dotForward:F2}"
        );

        // -----------------------------------------------------
        // 縦面
        // -----------------------------------------------------

        if (absUp >= absRight && absUp >= absForward)
        {
            // judgePivot.up が上向き → 正立
            if (dotUp > 0f)
            {
                return GraveFaceResult.Vertical;
            }

            // judgePivot.up が下向き → 逆立ち
            return GraveFaceResult.Reverse;
        }

        // -----------------------------------------------------
        // 表 / 裏
        // -----------------------------------------------------

        if (absForward >= absRight)
        {
            return dotForward > 0f
                ? GraveFaceResult.Back
                : GraveFaceResult.Front;
        }

        // -----------------------------------------------------
        // 横面
        // -----------------------------------------------------

        return GraveFaceResult.Side;
    }
}
