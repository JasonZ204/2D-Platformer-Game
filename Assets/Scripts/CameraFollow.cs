using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;       // the Player
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] private float smoothing = 8f;   // higher = snappier

    void LateUpdate()
    {
        if (!target) return;

        Vector3 goal = new Vector3(target.position.x + offset.x,
                                   target.position.y + offset.y,
                                   transform.position.z);

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, goal, t);
    }
}