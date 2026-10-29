using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentPanelController : MonoBehaviour
{
    [Header("4개 고정 슬롯")]
    public EquipmentSlotUI weaponSlot;
    public EquipmentSlotUI headSlot;
    public EquipmentSlotUI bodySlot;
    public EquipmentSlotUI shoesSlot;

    [Header("캐릭터 미리보기 (PC 전신/반신)")]
    public Image characterPreview; // 선택한 장비에 맞게 캐릭터 이미지를 바꾸고 싶으면 사용, 아니면 비워둬도 됨

    [Header("상세 정보")]
    public GameObject detailRoot;
    public GameObject emptyDetailText;
    public TMP_Text categoryLabel; // "무기"
    public TMP_Text nameText;      // "야구 배트"
    public Image iconImage;
    public TMP_Text gradeText;        // "등급: 최하급"
    public TMP_Text attackPowerText;  // "기본 효과: 공격력 10"
    public TMP_Text magicEffectText;  // "마법 효과: 화염"
    public TMP_Text descriptionText;

    private PlayerEquipment playerEquipment;
    private EquipSlotType selectedSlot = EquipSlotType.Weapon;

    static readonly string[] SlotLabel = { "무기", "머리", "상의", "신발" };

    void OnEnable()
    {
        FindPlayerEquipment();
        Refresh();
    }

    void OnDisable()
    {
        if (playerEquipment != null) playerEquipment.OnEquipmentChanged -= Refresh;
        playerEquipment = null;
    }

    void FindPlayerEquipment()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerEquipment = player != null ? player.GetComponent<PlayerEquipment>() : null;
        if (playerEquipment != null) playerEquipment.OnEquipmentChanged += Refresh;
    }

    void Refresh()
    {
        if (playerEquipment == null) { ShowDetail(null); return; }

        weaponSlot.Setup(playerEquipment.GetEquipped(EquipSlotType.Weapon), EquipSlotType.Weapon, this);
        headSlot.Setup(playerEquipment.GetEquipped(EquipSlotType.Head), EquipSlotType.Head, this);
        bodySlot.Setup(playerEquipment.GetEquipped(EquipSlotType.Body), EquipSlotType.Body, this);
        shoesSlot.Setup(playerEquipment.GetEquipped(EquipSlotType.Shoes), EquipSlotType.Shoes, this);

        ShowDetail(playerEquipment.GetEquipped(selectedSlot));
    }

    public void Select(EquipmentSlotUI slot, EquipSlotType type)
    {
        selectedSlot = type;
        weaponSlot.SetSelected(type == EquipSlotType.Weapon);
        headSlot.SetSelected(type == EquipSlotType.Head);
        bodySlot.SetSelected(type == EquipSlotType.Body);
        shoesSlot.SetSelected(type == EquipSlotType.Shoes);
        ShowDetail(slot.Item);
    }

    void ShowDetail(EquipmentData item)
    {
        bool has = item != null;
        detailRoot.SetActive(has);
        emptyDetailText.SetActive(!has);
        if (!has) return;

        categoryLabel.text = SlotLabel[(int)selectedSlot];
        nameText.text = item.equipName;
        iconImage.enabled = item.icon != null;
        iconImage.sprite = item.icon;
        gradeText.text = EquipmentData.GetGradeLabel(item.grade);
        attackPowerText.text = "공격력 " + item.attackPower;
        magicEffectText.text = string.IsNullOrEmpty(item.magicEffect) ? "없음" : item.magicEffect;
        descriptionText.text = string.IsNullOrEmpty(item.description) ? "설명이 없습니다." : item.description;
    }
}