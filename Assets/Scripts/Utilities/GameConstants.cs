using UnityEngine;

public static class GameConstants
{
    public static class Layers
    {
        public const string Targeting = "Targeting";
        public const string Projectile = "Projectile";
        public const string Player = "Player";
        public const string Weapon = "Weapon";
        public const string Enemy = "Enemy";

        public static readonly int TargetingLayer = LayerMask.NameToLayer(Targeting);
        public static readonly int ProjectileLayer = LayerMask.NameToLayer(Projectile);
        public static readonly int PlayerLayer = LayerMask.NameToLayer(Player);
        public static readonly int WeaponLayer = LayerMask.NameToLayer(Weapon);
        public static readonly int EnemyLayer = LayerMask.NameToLayer(Enemy);
    }

    public static class Tags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
    }
}
