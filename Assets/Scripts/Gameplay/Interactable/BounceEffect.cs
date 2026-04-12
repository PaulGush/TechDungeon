using UnityEngine;

public class BounceEffect : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float m_bounceSpeed = 0.25f;
    [SerializeField] private float m_bounceVerticalDistance = 0.1f;

    private Vector3 m_bounceUpperTarget;
    private Vector3 m_bounceLowerTarget;
    private bool m_isMovingToUpper = true;
    private bool m_hasTargets;

    private void OnEnable()
    {
        m_isMovingToUpper = true;
    }

    public void SetTargets()
    {
        m_bounceLowerTarget = new Vector3(transform.position.x, transform.position.y - m_bounceVerticalDistance, transform.position.z);
        m_bounceUpperTarget = new Vector3(transform.position.x, transform.position.y + m_bounceVerticalDistance, transform.position.z);
        m_hasTargets = true;
    }

    private void Update()
    {
        if (!m_hasTargets) return;

        Vector3 target = m_isMovingToUpper ? m_bounceUpperTarget : m_bounceLowerTarget;
        transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * m_bounceSpeed);

        if ((transform.position - target).sqrMagnitude < Mathf.Epsilon)
        {
            m_isMovingToUpper = !m_isMovingToUpper;
        }
    }

    public void Stop()
    {
        enabled = false;
        if (m_hasTargets)
        {
            transform.position = (m_bounceUpperTarget + m_bounceLowerTarget) / 2f;
        }
        m_hasTargets = false;
    }
}
