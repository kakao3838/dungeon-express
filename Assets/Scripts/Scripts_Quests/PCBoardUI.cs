using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 사무실 PC 화면입니다. 홈 화면에서 '의뢰 접수 / 장비 강화 / 가방 강화'를 고르고,
// 의뢰 접수에서는 접수 가능한 퀘스트 목록과 상세 정보를 보고 접수합니다.
// 장비/가방 강화는 메인 퀘스트 2를 끝낼 때까지 잠겨 있고, 해금되어도 아직은 '준비 중' 화면만 보여줍니다.
// 열려 있는 동안에는 게임 시간이 멈추고 플레이어 조작이 막힙니다 (GamePause).
public class PCBoardUI : MonoBehaviour
{
    [Header("연결 - 전체")]
    public GameObject panel;
    public Button closeButton;
    public TMP_Text hintText; // 잠긴 기능을 눌렀을 때 안내

    [Header("연결 - 홈 화면 (아이콘 3개)")]
    public GameObject homeView;
    public Button requestIconButton;
    public Button equipIconButton;
    public Button bagIconButton;
    public GameObject equipLockMark;
    public GameObject bagLockMark;

    [Header("연결 - 탭 화면")]
    public GameObject tabView;
    public Button requestTabButton;
    public Button equipTabButton;
    public Button bagTabButton;
    public Image[] tabHighlights; // 순서: 의뢰 접수, 장비 강화, 가방 강화
    public GameObject requestContent;
    public GameObject equipContent;
    public GameObject bagContent;
    public TMP_Text equipContentText;
    public TMP_Text bagContentText;

    [Header("연결 - 의뢰 목록 / 상세")]
    public RectTransform listParent;
    public Button rowTemplate;     // 자식: Label. 꺼둔 견본
    public GameObject emptyText;
    public GameObject detailRoot;
    public TMP_Text detailNameText;
    public TMP_Text detailBodyText;
    public Button acceptButton;
    public TMP_Text acceptHintText; // 접수할 수 없을 때 이유를 보여줌

    private static readonly Color RowIdle = new Color(0.88f, 0.88f, 0.88f);
    private static readonly Color RowSelected = new Color(0.98f, 0.8f, 0.8f);
    private static readonly Color TabSelected = new Color(0.82f, 0.82f, 0.82f);
    private static readonly Color TabIdle = new Color(0.93f, 0.93f, 0.93f);

    private readonly List<Button> rows = new List<Button>();
    private readonly List<QuestData> rowQuests = new List<QuestData>();
    private List<QuestData> offered;
    private QuestData selected;
    private bool allowClose = true;
    private bool allowAccept = true;
    private bool holdingPause;
    private int openedFrame = -1;
    private int currentTab = -1;

    public bool IsOpen => panel != null && panel.activeSelf;

    // 의뢰 접수 화면(탭)이 열렸을 때 / 퀘스트를 접수했을 때 / PC를 닫았을 때
    public event Action OnRequestTabOpened;
    public event Action<QuestData> OnQuestAccepted;
    public event Action OnClosed;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);

        if (requestIconButton != null) requestIconButton.onClick.AddListener(() => ShowTab(0));
        if (equipIconButton != null) equipIconButton.onClick.AddListener(() => ShowTab(1));
        if (bagIconButton != null) bagIconButton.onClick.AddListener(() => ShowTab(2));
        if (requestTabButton != null) requestTabButton.onClick.AddListener(() => ShowTab(0));
        if (equipTabButton != null) equipTabButton.onClick.AddListener(() => ShowTab(1));
        if (bagTabButton != null) bagTabButton.onClick.AddListener(() => ShowTab(2));
        if (acceptButton != null) acceptButton.onClick.AddListener(AcceptSelected);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (SaveSlotWindow.BlocksGameplayInput) return;
        // 열린 뒤 Esc로 닫기 (열린 그 프레임의 입력은 무시)
        if (!IsOpen || !allowClose || Time.frameCount == openedFrame) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    void OnDestroy()
    {
        ReleasePause(); // 열린 채로 씬이 바뀌어도 시간이 멈춰 있지 않게
    }

    // offeredQuests가 null이면 QuestCatalog의 PC 퀘스트 목록을 보여줌 (튜토리얼은 튜토리얼 퀘스트 하나만 넘김)
    // allowCloseBoard가 false면 닫기 버튼과 Esc로 닫을 수 없음 (튜토리얼 중)
    // allowAcceptQuest가 false면 접수 버튼을 쓸 수 없음 (스토리에서 NPC가 대신 접수해 줄 때)
    public void Open(IList<QuestData> offeredQuests = null, bool allowCloseBoard = true, bool allowAcceptQuest = true)
    {
        if (panel == null) return;
        allowAccept = allowAcceptQuest;

        if (offeredQuests != null) offered = new List<QuestData>(offeredQuests);
        else
        {
            var catalog = QuestCatalog.Load();
            offered = catalog != null ? new List<QuestData>(catalog.pcQuests) : new List<QuestData>();
        }

        allowClose = allowCloseBoard;
        if (closeButton != null) closeButton.gameObject.SetActive(allowClose);

        panel.SetActive(true);
        openedFrame = Time.frameCount;
        selected = null;
        currentTab = -1;
        SetHint("");
        ShowHome();
        HoldPause();
    }

    // 열려 있는 동안 접수 버튼을 쓸 수 있게 하거나 숨김 (튜토리얼/스토리에서 안내가 끝난 뒤에 열어 줄 때)
    public void SetAcceptAllowed(bool allowed)
    {
        allowAccept = allowed;
        if (IsOpen) ShowDetail(selected);
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);
        ReleasePause();
        OnClosed?.Invoke();
    }

    // ---------- 홈 / 탭 ----------

    void ShowHome()
    {
        if (homeView != null) homeView.SetActive(true);
        if (tabView != null) tabView.SetActive(false);
        RefreshLocks();
    }

    bool IsTabLocked(int tab)
    {
        var manager = QuestManager.Instance;
        if (tab == 1) return manager == null || !manager.HasFlag(QuestManager.FlagEquipmentUpgrade);
        if (tab == 2) return manager == null || !manager.HasFlag(QuestManager.FlagBagUpgrade);
        return false;
    }

    void RefreshLocks()
    {
        if (equipLockMark != null) equipLockMark.SetActive(IsTabLocked(1));
        if (bagLockMark != null) bagLockMark.SetActive(IsTabLocked(2));
    }

    void ShowTab(int tab)
    {
        if (IsTabLocked(tab))
        {
            SetHint("메인 퀘스트 '사라진 기술자'를 완료하면 이용할 수 있습니다.");
            return;
        }
        SetHint("");

        if (homeView != null) homeView.SetActive(false);
        if (tabView != null) tabView.SetActive(true);

        bool changed = currentTab != tab;
        currentTab = tab;

        if (requestContent != null) requestContent.SetActive(tab == 0);
        if (equipContent != null) equipContent.SetActive(tab == 1);
        if (bagContent != null) bagContent.SetActive(tab == 2);
        if (equipContentText != null) equipContentText.text = "장비 강화는 준비 중입니다.";
        if (bagContentText != null) bagContentText.text = "가방 강화는 준비 중입니다.";

        if (tabHighlights != null)
        {
            for (int i = 0; i < tabHighlights.Length; i++)
            {
                if (tabHighlights[i] != null) tabHighlights[i].color = i == tab ? TabSelected : TabIdle;
            }
        }
        RefreshLocks();

        if (tab == 0)
        {
            RefreshList();
            if (changed) OnRequestTabOpened?.Invoke();
        }
    }

    void SetHint(string message)
    {
        if (hintText == null) return;
        hintText.text = message;
        hintText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    // ---------- 의뢰 목록 / 상세 ----------

    void RefreshList()
    {
        foreach (var row in rows) if (row != null) Destroy(row.gameObject);
        rows.Clear();
        rowQuests.Clear();

        var manager = QuestManager.Instance;
        foreach (var quest in offered)
        {
            if (quest == null) continue;
            // 이미 진행 중이거나 완료한 퀘스트는 목록에서 뺌
            if (manager != null && (manager.IsQuestActive(quest) || manager.IsQuestCompleted(quest))) continue;

            var row = Instantiate(rowTemplate, listParent);
            row.gameObject.SetActive(true);
            var label = row.transform.Find("Label");
            if (label != null) label.GetComponent<TMP_Text>().text = quest.questName;

            QuestData captured = quest; // 반복문 변수 캡처 문제 방지
            row.onClick.AddListener(() => Select(captured));
            rows.Add(row);
            rowQuests.Add(quest);
        }

        if (emptyText != null) emptyText.SetActive(rows.Count == 0);

        if (selected != null && !rowQuests.Contains(selected)) selected = null;
        if (selected == null && rowQuests.Count > 0) selected = rowQuests[0];
        ShowDetail(selected);
    }

    void Select(QuestData quest)
    {
        selected = quest;
        ShowDetail(quest);
    }

    void ShowDetail(QuestData quest)
    {
        if (detailRoot != null) detailRoot.SetActive(quest != null);

        for (int i = 0; i < rows.Count; i++)
        {
            var img = rows[i].GetComponent<Image>();
            if (img != null) img.color = rowQuests[i] == quest ? RowSelected : RowIdle;
        }
        if (quest == null) return;

        // 의뢰는 한 번에 하나만 접수할 수 있음 (메인/서브 구분 없이, 진행 중인 의뢰를 끝내야 다음 접수)
        bool blocked = HasActiveQuest();
        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(allowAccept); // 스토리에서 NPC가 대신 접수할 때는 접수 버튼을 숨김
            acceptButton.interactable = !blocked;
        }
        if (acceptHintText != null)
        {
            string reason = allowAccept && blocked ? "진행 중인 의뢰를 끝내야\n새 의뢰를 접수할 수 있습니다." : "";
            acceptHintText.text = reason;
            acceptHintText.gameObject.SetActive(reason.Length > 0);
        }

        if (detailNameText != null) detailNameText.text = quest.questName;
        if (detailBodyText != null)
        {
            var sb = new StringBuilder();
            sb.Append("의뢰인  ").Append(string.IsNullOrEmpty(quest.client) ? "-" : quest.client);
            sb.Append("\n의뢰 물품  ").Append(FormatItems(quest));
            sb.Append("\n의뢰 장소  ").Append(string.IsNullOrEmpty(quest.targetLocation) ? "-" : quest.targetLocation);
            sb.Append("\n의뢰 보수  ").Append(quest.reward.ToString("N0")).Append(" 골드");
            sb.Append("\n\n").Append(quest.description);
            detailBodyText.text = sb.ToString();
        }
    }

    static string FormatItems(QuestData quest)
    {
        if (quest.requiredItems == null || quest.requiredItems.Count == 0) return "-";

        var sb = new StringBuilder();
        foreach (var req in quest.requiredItems)
        {
            if (req.item == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(req.item.itemName);
            if (req.quantity > 1) sb.Append(" x").Append(req.quantity);
            string trait = ItemData.GetTraitLabel(req.item.itemType);
            if (req.item.itemType != ItemType.Normal && !string.IsNullOrEmpty(trait)) sb.Append(" (").Append(trait).Append(')');
        }
        return sb.Length > 0 ? sb.ToString() : "-";
    }

    static bool HasActiveQuest()
    {
        return QuestManager.Instance != null && QuestManager.Instance.activeQuests.Count > 0;
    }

    void AcceptSelected()
    {
        if (selected == null || QuestManager.Instance == null) return;
        if (HasActiveQuest() || !allowAccept) return; // 한 번에 하나만 접수

        var inventory = FindFirstObjectByType<Inventory>();
        QuestData accepted = selected;
        if (!QuestManager.Instance.StartQuest(accepted, inventory)) return;

        selected = null;
        RefreshList();
        OnQuestAccepted?.Invoke(accepted);
    }

    // ---------- 일시정지 ----------

    void HoldPause()
    {
        if (holdingPause) return;
        holdingPause = true;
        GamePause.Push();
    }

    void ReleasePause()
    {
        if (!holdingPause) return;
        holdingPause = false;
        GamePause.Pop();
    }
}
