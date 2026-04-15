using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityServiceLocator;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private CinemachineBasicMultiChannelPerlin m_cameraShake;

    private void Start()
    {
        ServiceLocator.Global.Register(this);
    }

    public void ShakeCam(float duration, float amp, float freq)
    {
        if (m_cameraShake == null)
        {
            Debug.LogWarning($"{nameof(CameraShake)}: {nameof(m_cameraShake)} is not assigned.", this);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ShakeCamCoroutine(duration, amp, freq));
    }

    private IEnumerator ShakeCamCoroutine(float duration, float amp, float freq)
    {
        m_cameraShake.AmplitudeGain = amp;
        m_cameraShake.FrequencyGain = freq;

        yield return new WaitForSeconds(duration);

        m_cameraShake.AmplitudeGain = 0f;
        m_cameraShake.FrequencyGain = 0f;
    }
}