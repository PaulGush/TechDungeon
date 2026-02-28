using UnityEngine;

public class BounceEffect : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float m_bounceSpeed = 0.25f;
    [SerializeField] private float m_bounceVerticalDistance = 0.1f;

    private Vector3 m_bounceUpperTarget;
    private Vector3 m_bounceLowerTarget;
    private bool m_isMovingToUpper = true;

    private void OnEnable()
    {
        m_isMovingToUpper = true;
    }

    public void SetTargets()
    {
        m_bounceLowerTarget = new Vector3(transform.position.x, transform.position.y - m_bounceVerticalDistance, transform.position.z);
        m_bounceUpperTarget = new Vector3(transform.position.x, transform.position.y + m_bounceVerticalDistance, transform.position.z);
    }

    private void Update()
    {
        if (m_bounceLowerTarget == Vector3.zero && m_bounceUpperTarget == Vector3.zero) return;

        Vector3 target = m_isMovingToUpper ? m_bounceUpperTarget : m_bounceLowerTarget;
        transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * m_bounceSpeed);

        if (Vector3.Distance(transform.position, target) < 0.001f)
        {
            m_isMovingToUpper = !m_isMovingToUpper;
        }
    }

    public void Stop()
    {
        enabled = false;
        if (m_bounceLowerTarget != Vector3.zero && m_bounceUpperTarget != Vector3.zero)
        {
            transform.position = (m_bounceUpperTarget + m_bounceLowerTarget) / 2f;
        }
        m_bounceUpperTarget = Vector3.zero;
        m_bounceLowerTarget = Vector3.zero;
    }
}
