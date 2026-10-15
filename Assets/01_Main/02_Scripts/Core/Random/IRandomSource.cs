namespace LastSupply.Core
{
    public interface IRandomSource
    {
        // minInclusive 이상, maxExclusive 미만의 정수를 반환
        int NextInt(int minInclusive, int maxExclusive);
    }
}