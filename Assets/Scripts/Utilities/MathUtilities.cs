using UnityEngine;

public static class MathUtilities
{
    public static Quaternion CalculateAimRotation(Vector3 direction, float offsetDegrees = 0f)
    {
        float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, rotZ + offsetDegrees);
    }

    /// <summary>
    /// Converts a direction vector into a [0, 360) clockwise angle suitable for an Animator blend-tree
    /// parameter that drives 8-directional sprite facing.
    /// </summary>
    public static float CalculateSpriteFacingAngleDegrees(Vector2 direction)
    {
        return Mathf.Repeat(-Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 360f);
    }
}
