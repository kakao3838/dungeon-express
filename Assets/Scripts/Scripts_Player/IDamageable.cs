// 데미지를 받을 수 있는 모든 대상(플레이어, 몬스터 등)이 공통으로 구현하는 인터페이스
public interface IDamageable
{
    void TakeDamage(int amount);
}
