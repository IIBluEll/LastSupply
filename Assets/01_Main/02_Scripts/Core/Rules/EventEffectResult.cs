using System;

namespace LastSupply.Core
{
    // 사건 효과 계산 후의 값
    // 실제 상태 반영은 Session이 담당
    public readonly struct EventEffectResult
    {
        public int CurrentHp { get; }
        public int Noise { get; }
        public ResourceAmount Loot { get; }

        public EventEffectResult(int currentHp , int noise , ResourceAmount loot)
        {
            if ( currentHp < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            if ( noise < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(noise));
            }

            CurrentHp = currentHp;
            Noise = noise;
            Loot = loot;
        }
    }
}