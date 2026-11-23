using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public static class DeliveryCompletePanelPrefabBuilder
{
    const string TutorialScenePath = "Assets/Scenes/TownScene.unity";
    const string PanelPrefabPath = "Assets/Prefab/UI/Delivery/DeliveryCompletePanel.prefab";
    const string BaseUiClosePath = "Assets/Prefab/UI/Tutorial/BaseUi_Close.prefab";
    const string BaseUiNavigationPath = "Assets/Prefab/UI/Tutorial/BaseUi_Navigation.prefab";
    const string KylePrefabPath = "Assets/Prefab/Kyle.prefab";

    [MenuItem("Dungeon Express/Prefab/Extract Tutorial BaseUi Frames and Apply Delivery")]
    public static void ExtractTutorialBaseUiFramesAndApplyDelivery()
    {
        EnsureFolder("Assets/Prefab/UI/Tutorial");
        EnsureFolder("Assets/Prefab/UI/Delivery");

        Scene sourceScene = SceneManager.GetSceneByPath(TutorialScenePath);
        bool openedScene = !sourceScene.IsValid() || !sourceScene.isLoaded;
        if (openedScene)
            sourceScene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);

        try
        {
            var tutorialPanel = FindInScene(sourceScene, "TutorialSlidePanel");
            if (tutorialPanel == null)
                throw new System.InvalidOperationException("TownScene에서 TutorialSlidePanel을 찾지 못했습니다.");

            var background = tutorialPanel.GetComponent<Image>();
            var closeSource = tutorialPanel.transform.Find("CloseBtn");
            var leftSource = tutorialPanel.transform.Find("left");
            var rightSource = tutorialPanel.transform.Find("right");
            if (background == null || background.sprite == null || closeSource == null || leftSource == null || rightSource == null)
                throw new System.InvalidOperationException("TutorialSlidePanel의 BaseUi 배경 또는 버튼(left/right/CloseBtn)을 찾지 못했습니다.");

            var closePrefab = CreateBaseUiClosePrefab(background, closeSource.gameObject);
            CreateBaseUiNavigationPrefab(closePrefab, leftSource.gameObject, rightSource.gameObject);
            ApplyCloseFrameToDeliveryPanel(closePrefab);
            ApplyDeliveryPanelPrefabToKyle();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DeliveryCompletePanelPrefabBuilder] TutorialSlidePanel의 BaseUi 오브젝트에서 Close/Navigation 프리팹을 추출하고 배송 완료 패널에 Close 버전을 적용했습니다.");
        }
        finally
        {
            if (openedScene && sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    static GameObject CreateBaseUiClosePrefab(Image sourceBackground, GameObject sourceCloseButton)
    {
        var root = new GameObject("BaseUi_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            var rootRect = (RectTransform)root.transform;
            SetFullStretch(rootRect);
            CopyImage(sourceBackground, root.GetComponent<Image>());
            CloneButton(sourceCloseButton, root.transform, "CloseButton");

            return PrefabUtility.SaveAsPrefabAsset(root, BaseUiClosePath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void CreateBaseUiNavigationPrefab(GameObject closePrefab, GameObject sourceLeft, GameObject sourceRight)
    {
        var root = PrefabUtility.LoadPrefabContents(BaseUiClosePath);
        try
        {
            CloneButton(sourceLeft, root.transform, "PreviousButton");
            CloneButton(sourceRight, root.transform, "NextButton");
            PrefabUtility.SaveAsPrefabAsset(root, BaseUiNavigationPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void CloneButton(GameObject source, Transform parent, string name)
    {
        var clone = Object.Instantiate(source, parent, false);
        clone.name = name;
        var sourceRect = source.GetComponent<RectTransform>();
        var cloneRect = clone.GetComponent<RectTransform>();
        if (sourceRect != null && cloneRect != null)
        {
            cloneRect.anchorMin = sourceRect.anchorMin;
            cloneRect.anchorMax = sourceRect.anchorMax;
            cloneRect.pivot = sourceRect.pivot;
            cloneRect.anchoredPosition = sourceRect.anchoredPosition * 0.75f;
            cloneRect.sizeDelta = sourceRect.sizeDelta * 0.75f;
            cloneRect.localScale = sourceRect.localScale;
            cloneRect.localRotation = sourceRect.localRotation;
        }

        var button = clone.GetComponent<Button>();
        if (button != null)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
        }
    }

    static void CopyImage(Image source, Image target)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.material = source.material;
        target.type = source.type;
        target.preserveAspect = source.preserveAspect;
        target.fillCenter = source.fillCenter;
        target.fillMethod = source.fillMethod;
        target.fillAmount = source.fillAmount;
        target.fillClockwise = source.fillClockwise;
        target.fillOrigin = source.fillOrigin;
        target.useSpriteMesh = source.useSpriteMesh;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        target.raycastTarget = false;
        target.maskable = source.maskable;
    }

    static void ApplyCloseFrameToDeliveryPanel(GameObject closePrefab)
    {
        var panel = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
        try
        {
            var oldFrame = panel.transform.Find("Frame");
            if (oldFrame != null) Object.DestroyImmediate(oldFrame.gameObject);

            var frame = PrefabUtility.InstantiatePrefab(closePrefab, panel.transform) as GameObject;
            if (frame == null)
                throw new System.InvalidOperationException("BaseUi_Close 프리팹을 배송 완료 패널에 넣지 못했습니다.");

            frame.name = "Frame";
            SetFullStretch((RectTransform)frame.transform);
            frame.transform.SetAsFirstSibling();
            PrefabUtility.SaveAsPrefabAsset(panel, PanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(panel);
        }
    }

    static void ApplyDeliveryPanelPrefabToKyle()
    {
        var kyle = PrefabUtility.LoadPrefabContents(KylePrefabPath);
        try
        {
            var window = kyle.GetComponentInChildren<DeliveryCompleteWindow>(true);
            if (window == null || window.panel == null)
                throw new System.InvalidOperationException("Kyle.prefab에서 배송 완료 패널 참조를 찾지 못했습니다.");

            var oldPanel = window.panel;
            var oldRect = oldPanel.GetComponent<RectTransform>();
            var parent = oldPanel.transform.parent;
            int siblingIndex = oldPanel.transform.GetSiblingIndex();
            bool wasActive = oldPanel.activeSelf;
            if (oldRect == null || parent == null)
                throw new System.InvalidOperationException("Kyle.prefab의 기존 배송 완료 패널 계층이 예상과 다릅니다.");

            var panelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var replacement = PrefabUtility.InstantiatePrefab(panelAsset, parent) as GameObject;
            if (replacement == null)
                throw new System.InvalidOperationException("DeliveryCompletePanel 프리팹을 Kyle.prefab에 넣지 못했습니다.");

            var rows = FindInTree(replacement.transform, "Rows");
            var rowTemplate = FindInTree(replacement.transform, "RowTemplate");
            var baseValue = FindInTree(replacement.transform, "BaseValue");
            var bonusValue = FindInTree(replacement.transform, "BonusValue");
            var finalValue = FindInTree(replacement.transform, "FinalValue");
            if (rows == null || rowTemplate == null || baseValue == null || bonusValue == null || finalValue == null)
            {
                Object.DestroyImmediate(replacement);
                throw new System.InvalidOperationException("배송 완료 프리팹에서 Rows/RowTemplate/보상 텍스트를 모두 찾지 못했습니다.");
            }

            replacement.name = "Panel";
            CopyRectTransform(oldRect, replacement.GetComponent<RectTransform>());
            replacement.transform.SetSiblingIndex(siblingIndex);
            replacement.SetActive(wasActive);

            window.panel = replacement;
            window.rowsParent = rows as RectTransform;
            window.rowTemplate = rowTemplate.gameObject;
            window.baseRewardText = baseValue.GetComponent<TMP_Text>();
            window.gradeBonusText = bonusValue.GetComponent<TMP_Text>();
            window.finalRewardText = finalValue.GetComponent<TMP_Text>();

            Object.DestroyImmediate(oldPanel);
            PrefabUtility.SaveAsPrefabAsset(kyle, KylePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(kyle);
        }
    }

    static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindInTree(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    static Transform FindInTree(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindInTree(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        string folder = path.Substring(split + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
