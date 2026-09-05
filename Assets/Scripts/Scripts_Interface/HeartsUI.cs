using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [Header("연결")]
    public PlayerHealth playerHealth;
    public Image heartIconPrefab;   // 하트 한 칸짜리 UI Image 프리팹
    public Transform heartsContainer; // Horizontal Layout Group이 붙은 부모 오브젝트

    [Header("스프라이트 (나중에 실제 하트 이미지가 생기면 여기만 교체하세요)")]
    public Sprite fullHeartSprite;  // 비워두면 기본 원형 스프라이트 그대로 사용됩니다

    private readonly List<Image> heartIcons = new List<Image>();

    void Awake()
    {
        // 씬이 바뀌어도(Player는 DontDestroyOnLoad로 유지됨) 자동으로 연결되도록 함
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateDisplay;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateDisplay;
        }
    }

    void Start()
    {
        if (playerHealth == null) return;
        BuildHearts(playerHealth.maxHearts);
        UpdateDisplay(playerHealth.CurrentHearts, playerHealth.maxHearts);
    }

    // 하트 개수(max)만큼 아이콘을 새로 생성
    void BuildHearts(int max)
    {
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        heartIcons.Clear();

        for (int i = 0; i < max; i++)
        {
            Image icon = Instantiate(heartIconPrefab, heartsContainer);
            heartIcons.Add(icon);
        }
    }

    void UpdateDisplay(int current, int max)
    {
        // 최대 체력이 바뀐 경우(장비로 하트 늘어남 등) 아이콘 다시 생성
        if (heartIcons.Count != max)
        {
            BuildHearts(max);
        }

        for (int i = 0; i < heartIcons.Count; i++)
        {
            bool filled = i < current;

            // 맞아서 잃은 하트는 아예 사라지게 함
            heartIcons[i].gameObject.SetActive(filled);

            if (fullHeartSprite != null)
            {
                heartIcons[i].sprite = fullHeartSprite;
            }
            heartIcons[i].color = new Color(1f, 0.23f, 0.23f);
        }
    }
}
