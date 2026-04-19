namespace Gameplay.Audio
{
    // Game-semantic priority values used by SoundEvent assets and AudioService voice
    // stealing. Higher value wins when the pool is saturated. Designers pick the
    // closest tier rather than typing raw numbers into every SoundEvent.
    public static class AudioPriorities
    {
        public const int Footstep      = 10;
        public const int AmbientDetail = 15;
        public const int EnemyHurt     = 20;
        public const int ProjectileHit = 30;
        public const int EnemyDeath    = 40;
        public const int PlayerFire    = 45;
        public const int EnemyFire     = 45;
        public const int Pickup        = 50;
        public const int Chest         = 55;
        public const int PlayerHurt    = 60;
        public const int PlayerDeath   = 75;
        public const int BossStinger   = 80;
        public const int UI            = 90;
        public const int Dialogue      = 95;
    }
}
