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
        StopAllCoroutines();
        StartCoroutine(ShakeCamCoroutine(duration, amp, freq));
    }

    private IEnumerator ShakeCamCoroutine(float duration, float amp, float freq)
    {
        m_cameraShake.AmplitudeGain = amp;
        m_cameraShake.FrequencyGain = freq;
        
        yield return new WaitForSeconds(duration);
        
        m_cameraShake.AmplitudeGain = 0;
        m_cameraShake.FrequencyGain = 0;
    }
}