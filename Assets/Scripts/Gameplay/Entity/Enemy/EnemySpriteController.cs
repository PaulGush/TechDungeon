using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    
    private EnemyTargeting _targeting;

    private void Awake()
    {
        _targeting = _enemyController.Targeting;
    }

    private void Update()
    {
        if (_targeting == null || _targeting.CurrentTarget == null) return;
        _spriteRenderer.flipX = !_targeting.IsTargetRightOfTransform();
    }
}