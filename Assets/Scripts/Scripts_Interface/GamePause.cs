using System.Collections.Generic;
using UnityEngine;

// 대화창, 결과창처럼 게임을 멈춰야 하는 UI가 함께 쓰는 정지 처리입니다.
// Push()로 멈추고 Pop()으로 풉니다. 창이 여러 개 겹쳐도 마지막 창이 닫힐 때 한 번만 풀립니다.
// 시간이 멈춰도 Update의 입력 처리는 계속 돌아서, 플레이어 조작 스크립트도 같이 꺼둡니다.
public static class GamePause
{
    private static int holders = 0;
    private static float prevTimeScale = 1f;
    private static readonly List<Behaviour> disabledInputs = new List<Behaviour>();

    public static bool IsPaused => holders > 0;

    // 에디터에서 도메인 리로드를 끈 경우에도 이전 플레이의 값이 남지 않게 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        holders = 0;
        prevTimeScale = 1f;
        disabledInputs.Clear();
    }

    public static void Push()
    {
        holders++;
        if (holders > 1) return;

        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        disabledInputs.Clear();
        DisableIfEnabled(Object.FindFirstObjectByType<PlayerController>());
        DisableIfEnabled(Object.FindFirstObjectByType<PlayerAttack>());
    }

    public static void Pop()
    {
        if (holders == 0) return;
        holders--;
        if (holders > 0) return;

        Time.timeScale = prevTimeScale;

        // 원래 켜져 있던 것만 다시 켬 (플레이어가 죽어서 꺼진 상태 등은 그대로 둠)
        foreach (var b in disabledInputs)
        {
            if (b != null) b.enabled = true;
        }
        disabledInputs.Clear();
    }

    static void DisableIfEnabled(Behaviour b)
    {
        if (b == null || !b.enabled) return;
        b.enabled = false;
        disabledInputs.Add(b);
    }
}
