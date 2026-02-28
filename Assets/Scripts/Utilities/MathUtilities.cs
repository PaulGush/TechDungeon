using UnityEngine;

public static class MathUtilities
{
    public static Quaternion CalculateAimRotation(Vector3 direction, float offsetDegrees = 0f)
    {
        float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, rotZ + offsetDegrees);
    }
}
