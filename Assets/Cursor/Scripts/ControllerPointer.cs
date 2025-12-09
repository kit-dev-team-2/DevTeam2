using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
public class ControllerPointer : OVRCursor
{
    [SerializeField]
    private float _maxDistance = 10.0f;

    private LineRenderer _lineRenderer;
    private Vector3 _endPoint;
    private bool _hitTarget;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public override void SetCursorRay(Transform ray)
    {
        _hitTarget = false;
    }

    public override void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal)
    {
        _hitTarget = true;
        _endPoint = dest;
    }

    private void LateUpdate()
    {
        _lineRenderer.SetPosition(0, transform.position);
        if (_hitTarget)
        {
            _lineRenderer.SetPosition(1, _endPoint);
        }
        else
        {
            _lineRenderer.SetPosition(1, transform.position + transform.forward * _maxDistance);
        }
    }
}