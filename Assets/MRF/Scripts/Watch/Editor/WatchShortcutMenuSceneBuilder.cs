#if UNITY_EDITOR
using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class WatchShortcutMenuSceneBuilder
{
    private const string ScenePath = "Assets/MRF/Scenes/Watch/002Watch.unity";
    private const string ShaderPath = "Assets/MRF/Scripts/Watch/WatchShortcutPanel.shader";
    private const string MaterialFolder = "Assets/MRF/Materials/Watch";
    private const string PrefabFolder = "Assets/MRF/Prefabs/Watch";
    private const string MenuPrefabPath = PrefabFolder + "/WatchShortcutMenu.prefab";
    private const string ButtonPrefabPath =
        "Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/UISet/Prefabs/Button/UnityUIButtonBased/TextTileButton_IconAndLabel_Regular_UnityUIButton.prefab";
    private const string MarkerName = "ShortcutMenuWorldRoot";

    [MenuItem("Tools/MRF/Watch/Rebuild Shortcut Menu in 002Watch")]
    private static void RebuildFromMenu()
    {
        BuildScene(true);
    }

    public static void RebuildFromCommandLine()
    {
        BuildScene(true);
    }

    public static void UpdateExistingFromCommandLine()
    {
        Scene scene = GetLoadedScene();
        bool openedTemporarily = !scene.IsValid();
        if (openedTemporarily) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            WatchShortcutMenuController controller = FindComponent<WatchShortcutMenuController>(scene);
            if (controller == null) throw new InvalidOperationException("002Watch has no shortcut menu controller.");
            RepairShortcutReferences(scene, controller);
            EnsureSettingsToggle(scene, controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (openedTemporarily && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void BuildScene(bool rebuild)
    {
        if (!System.IO.File.Exists(ScenePath)) return;

        Scene scene = GetLoadedScene();
        bool openedTemporarily = !scene.IsValid();
        if (openedTemporarily) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject existing = FindGameObject(scene, MarkerName);
            WatchShortcutMenuController existingController = FindComponent<WatchShortcutMenuController>(scene);
            if (existing != null && existingController != null && !rebuild)
            {
                RepairShortcutReferences(scene, existingController);
                EnsureSettingsToggle(scene, existingController);
                GameObject existingAttachment = FindGameObject(scene, "ShortcutMenuAttachment");
                EnsureMenuPrefabInstance(scene, existingAttachment, existing);
                return;
            }
            if ((existing != null || existingController != null) && !rebuild) return;
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            if (existingController != null) UnityEngine.Object.DestroyImmediate(existingController);
            GameObject oldAttachment = FindGameObject(scene, "ShortcutMenuAttachment");
            if (oldAttachment != null) UnityEngine.Object.DestroyImmediate(oldAttachment);

            WatchController watch = FindComponent<WatchController>(scene);
            if (watch == null) throw new InvalidOperationException("002Watch has no WatchController.");
            Transform screen = FindChild(watch.transform, "Screen");
            if (screen == null) throw new InvalidOperationException("002Watch WatchVisual has no Screen child.");
            Renderer screenRenderer = screen.GetComponent<Renderer>();
            if (screenRenderer == null) throw new InvalidOperationException("The watch Screen has no Renderer.");

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) shader = Shader.Find("MRF/WatchShortcutPanel");
            if (shader == null) throw new InvalidOperationException("WatchShortcutPanel shader has not imported yet.");

            Material backgroundMaterial = GetOrCreateMaterial("WatchShortcutPanelBackground.mat", shader,
                new Color(0.018f, 0.025f, 0.035f, 0.94f), Color.clear, 0);
            Material borderMaterial = GetOrCreateMaterial("WatchShortcutPanelBorder.mat", shader,
                Color.clear, new Color(0.08f, 1f, 0.28f, 1), 0.65f);

            Bounds bounds = screenRenderer.localBounds;
            Transform wristAnchor = CreateAnchor(scene, screen, "WristMenuAnchor",
                new Vector3(bounds.center.x, bounds.max.y + 0.004f, bounds.center.z), Quaternion.Euler(-90, 0, 0));
            Transform leftAnchor = CreateControllerAnchor(scene, "LeftControllerAnchor", "LeftControllerMenuAnchor");
            Transform rightAnchor = CreateControllerAnchor(scene, "RightControllerAnchor", "RightControllerMenuAnchor");

            GameObject attachmentObject = CreateObject(scene, "ShortcutMenuAttachment", typeof(Transform));
            Transform attachment = attachmentObject.transform;
            attachment.SetParent(wristAnchor, false);
            attachment.localPosition = Vector3.forward * 0.08f;

            GameObject menuObject = CreateObject(scene, MarkerName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            RectTransform menuRoot = (RectTransform)menuObject.transform;
            menuRoot.SetParent(attachment, false);
            SetRect(menuRoot, new Vector2(560, 270), Vector2.zero);
            SetPhysicalPanelScale(menuRoot, 0.12f);

            Canvas canvas = menuObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = menuObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1;
            CanvasGroup canvasGroup = menuObject.GetComponent<CanvasGroup>();

            RectTransform surfaceRect = CreateRect(scene, "InteractionSurface", menuRoot, new Vector2(560, 270), Vector2.zero);
            PlaneSurface plane = surfaceRect.gameObject.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Forward, false);
            BoundsClipper clipper = surfaceRect.gameObject.AddComponent<BoundsClipper>();
            clipper.Size = new Vector3(560, 270, 0.01f);
            ClippedPlaneSurface clipped = surfaceRect.gameObject.AddComponent<ClippedPlaneSurface>();
            clipped.InjectAllClippedPlaneSurface(plane, new[] { clipper });
            RectTransformBoundsClipperDriver driver = surfaceRect.gameObject.AddComponent<RectTransformBoundsClipperDriver>();
            SerializedObject driverObject = new SerializedObject(driver);
            driverObject.FindProperty("_boundsClipper").objectReferenceValue = clipper;
            driverObject.ApplyModifiedPropertiesWithoutUndo();

            PointableCanvas pointableCanvas = menuObject.AddComponent<PointableCanvas>();
            pointableCanvas.InjectAllPointableCanvas(canvas);
            RayInteractable ray = menuObject.AddComponent<RayInteractable>();
            ray.InjectAllRayInteractable(clipped);
            PokeInteractable poke = menuObject.AddComponent<PokeInteractable>();
            poke.InjectAllPokeInteractable(clipped);

            RectTransform animatedContent = CreateStretchRect(scene, "AnimatedContent", menuRoot);
            Image background = CreateImage(scene, "PanelBackground", animatedContent, backgroundMaterial);
            Image border = CreateImage(scene, "SubtleGreenBorder", animatedContent, borderMaterial);
            TMP_Text titleText = CreateText(scene, "Title", animatedContent, "Shortcuts", 36,
                new Vector2(520, 52), new Vector2(0, 96), FontStyles.Bold);

            RectTransform buttonsRoot = CreateRect(scene, "Buttons", animatedContent, new Vector2(520, 170), new Vector2(0, -33));
            Button[] buttons = new Button[3];
            TMP_Text[] labels = new TMP_Text[3];
            TMP_Text[] descriptions = new TMP_Text[3];
            Image[] icons = new Image[3];
            GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
            if (buttonPrefab == null) throw new InvalidOperationException("Meta UI button prefab was not found.");

            for (int i = 0; i < 3; i++)
            {
                RectTransform item = CreateRect(scene, $"ShortcutButton0{i + 1}", buttonsRoot,
                    new Vector2(164, 160), new Vector2((i - 1) * 174, 0));
                GameObject buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, scene);
                buttonObject.name = "MetaUIButton";
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.SetParent(item, false);
                SetRect(buttonRect, new Vector2(150, 112), new Vector2(0, 18));

                buttons[i] = buttonObject.GetComponentInChildren<Button>(true);
                labels[i] = buttonObject.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
                icons[i] = buttonObject.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(image => image.gameObject.name.Equals("Icon", StringComparison.OrdinalIgnoreCase));
                if (buttons[i] == null || labels[i] == null)
                    throw new InvalidOperationException("The Meta UI button prefab is missing Button or TMP label components.");
                labels[i].text = $"Shortcut {i + 1}";
                descriptions[i] = CreateText(scene, "Description", item, "Text try", 22,
                    new Vector2(150, 34), new Vector2(0, -61), FontStyles.Normal);
            }

            WatchShortcutMenuController controller = watch.gameObject.AddComponent<WatchShortcutMenuController>();
            ConfigureController(controller, watch, menuRoot, canvasGroup, background, border,
                titleText, attachment, wristAnchor, leftAnchor, rightAnchor, buttons, labels, descriptions, icons);
            EnsureSettingsToggle(scene, controller);
            menuObject.SetActive(true);
            EnsureMenuPrefabInstance(scene, attachmentObject, menuObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Watch shortcut menu prefab created and instantiated in 002Watch.", controller);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (openedTemporarily && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void EnsureMenuPrefabInstance(Scene scene, GameObject attachment, GameObject menuRoot)
    {
        if (attachment == null || menuRoot == null)
            throw new InvalidOperationException("The shortcut menu hierarchy is incomplete.");

        EnsureFolder("Assets/MRF", "Prefabs");
        EnsureFolder("Assets/MRF/Prefabs", "Watch");
        menuRoot.SetActive(true);

        string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(attachment);
        if (!string.Equals(sourcePath, MenuPrefabPath, StringComparison.Ordinal))
        {
            GameObject connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                attachment, MenuPrefabPath, InteractionMode.AutomatedAction);
            if (connected == null)
                throw new InvalidOperationException("The shortcut menu prefab could not be created.");
        }

        RepairPrefabMaterials();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }
    private static void RepairPrefabMaterials()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MenuPrefabPath);
        try
        {
            Image background = FindChild(prefabRoot.transform, "PanelBackground")?.GetComponent<Image>();
            Image border = FindChild(prefabRoot.transform, "SubtleGreenBorder")?.GetComponent<Image>();
            if (background == null || border == null)
                throw new InvalidOperationException("The shortcut menu prefab has no panel images.");

            background.material = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/WatchShortcutPanelBackground.mat");
            border.material = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/WatchShortcutPanelBorder.mat");
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MenuPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
    private static void SetPhysicalPanelScale(RectTransform menuRoot, float widthMeters)
    {
        float canvasWidth = Mathf.Max(1f, menuRoot.rect.width);
        float targetWorldScale = widthMeters / canvasWidth;
        Vector3 parentScale = menuRoot.parent != null ? menuRoot.parent.lossyScale : Vector3.one;

        menuRoot.localScale = new Vector3(
            targetWorldScale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.x)),
            targetWorldScale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.y)),
            targetWorldScale / Mathf.Max(0.00001f, Mathf.Abs(parentScale.z)));
    }
    private static void ConfigureController(WatchShortcutMenuController controller, WatchController watch,
        RectTransform menuRoot, CanvasGroup canvasGroup, Graphic background,
        Graphic border, TMP_Text title, Transform attachment, Transform wrist, Transform left, Transform right,
        Button[] buttons, TMP_Text[] labels, TMP_Text[] descriptions, Image[] icons)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "watch", watch);
        SetReference(serialized, "menuRoot", menuRoot);

        SetReference(serialized, "canvasGroup", canvasGroup);
        SetReference(serialized, "panelBackground", background);
        SetReference(serialized, "subtleGreenBorder", border);
        SetReference(serialized, "titleText", title);
        SetReference(serialized, "menuAttachment", attachment);
        SetReference(serialized, "wristMenuAnchor", wrist);
        SetReference(serialized, "leftControllerMenuAnchor", left);
        SetReference(serialized, "rightControllerMenuAnchor", right);
        serialized.FindProperty("rotationOffset").vector3Value = new Vector3(90f, 180f, 0f);
        serialized.FindProperty("panelWidthMeters").floatValue = 0.12f;


        SerializedProperty entries = serialized.FindProperty("shortcuts");
        entries.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("label").stringValue = $"Shortcut {i + 1}";
            entry.FindPropertyRelative("description").stringValue = "Text try";
            entry.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            entry.FindPropertyRelative("labelText").objectReferenceValue = labels[i];
            entry.FindPropertyRelative("descriptionText").objectReferenceValue = descriptions[i];
            entry.FindPropertyRelative("iconImage").objectReferenceValue = icons[i];
            if (icons[i] != null) entry.FindPropertyRelative("icon").objectReferenceValue = icons[i].sprite;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void RepairShortcutReferences(Scene scene, WatchShortcutMenuController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty entries = serialized.FindProperty("shortcuts");
        entries.arraySize = 3;

        for (int i = 0; i < entries.arraySize; i++)
        {
            GameObject item = FindGameObject(scene, $"ShortcutButton0{i + 1}");
            if (item == null) throw new InvalidOperationException($"ShortcutButton0{i + 1} was not found.");

            Button button = item.GetComponentInChildren<Button>(true);
            TMP_Text description = FindChild(item.transform, "Description")?.GetComponent<TMP_Text>();
            TMP_Text label = item.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text != description);
            Image icon = item.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.gameObject.name.Equals("Icon", StringComparison.OrdinalIgnoreCase));

            if (button == null || label == null || description == null)
                throw new InvalidOperationException($"ShortcutButton0{i + 1} has incomplete UI references.");

            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("button").objectReferenceValue = button;
            entry.FindPropertyRelative("labelText").objectReferenceValue = label;
            entry.FindPropertyRelative("descriptionText").objectReferenceValue = description;
            entry.FindPropertyRelative("iconImage").objectReferenceValue = icon;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void EnsureSettingsToggle(Scene scene, WatchShortcutMenuController controller)
    {
        GameObject settingsRoot = FindGameObject(scene, "SettingsMenuWorldRoot_TwoColumns");
        if (settingsRoot == null)
            throw new InvalidOperationException("SettingsMenuWorldRoot_TwoColumns was not found.");

        InspectorGameObjectToggle toggle = controller.GetComponent<InspectorGameObjectToggle>();
        if (toggle == null) toggle = controller.gameObject.AddComponent<InspectorGameObjectToggle>();

        SerializedObject serializedToggle = new SerializedObject(toggle);
        serializedToggle.FindProperty("target").objectReferenceValue = settingsRoot;
        serializedToggle.FindProperty("startInactive").boolValue = true;
        serializedToggle.ApplyModifiedPropertiesWithoutUndo();

        UnityEvent shortcutEvent = controller.Shortcuts[0].OnInvoked;
        bool alreadyConnected = Enumerable.Range(0, shortcutEvent.GetPersistentEventCount()).Any(index =>
            shortcutEvent.GetPersistentTarget(index) == toggle &&
            shortcutEvent.GetPersistentMethodName(index) == nameof(InspectorGameObjectToggle.OnToggle));
        if (!alreadyConnected) UnityEventTools.AddPersistentListener(shortcutEvent, toggle.OnToggle);

        settingsRoot.SetActive(false);
        EditorUtility.SetDirty(toggle);
        EditorUtility.SetDirty(controller);
    }
    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        serialized.FindProperty(name).objectReferenceValue = value;
    }

    private static Material GetOrCreateMaterial(string fileName, Shader shader, Color panel, Color border, float glow)
    {
        EnsureFolder("Assets/MRF", "Materials");
        EnsureFolder("Assets/MRF/Materials", "Watch");
        string path = $"{MaterialFolder}/{fileName}";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(fileName) };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_PanelColor", panel);
        material.SetColor("_BorderColor", border);
        material.SetFloat("_BorderWidth", 0.014f);
        material.SetFloat("_CornerRadius", 0.12f);
        material.SetFloat("_GlowStrength", glow);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, child);
    }

    private static Transform CreateControllerAnchor(Scene scene, string rigAnchorName, string menuAnchorName)
    {
        Transform rigAnchor = FindTransform(scene, rigAnchorName);
        if (rigAnchor == null) throw new InvalidOperationException($"002Watch has no {rigAnchorName}.");
        return CreateAnchor(scene, rigAnchor, menuAnchorName, new Vector3(0, 0.055f, 0.025f),
            Quaternion.Euler(55, 0, 0));
    }

    private static Transform CreateAnchor(Scene scene, Transform parent, string name, Vector3 position, Quaternion rotation)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null) return existing;
        GameObject anchorObject = CreateObject(scene, name, typeof(Transform));
        Transform anchor = anchorObject.transform;
        anchor.SetParent(parent, false);
        anchor.localPosition = position;
        anchor.localRotation = rotation;
        return anchor;
    }

    private static RectTransform CreateStretchRect(Scene scene, string name, Transform parent)
    {
        RectTransform rect = CreateRect(scene, name, parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform CreateRect(Scene scene, string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject gameObject = CreateObject(scene, name, typeof(RectTransform));
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        SetRect(rect, size, position);
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private static Image CreateImage(Scene scene, string name, RectTransform parent, Material material)
    {
        RectTransform rect = CreateStretchRect(scene, name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        image.material = material;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(Scene scene, string name, Transform parent, string value, float size,
        Vector2 rectSize, Vector2 position, FontStyles style)
    {
        RectTransform rect = CreateRect(scene, name, parent, rectSize, position);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = new Color(0.94f, 0.98f, 0.96f, 1);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateObject(Scene scene, string name, params Type[] components)
    {
        GameObject gameObject = new GameObject(name, components);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static Scene GetLoadedScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == ScenePath) return scene;
        }
        return default;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }

    private static GameObject FindGameObject(Scene scene, string name)
    {
        Transform transform = FindTransform(scene, name);
        return transform != null ? transform.gameObject : null;
    }

    private static Transform FindTransform(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChild(root.transform, name);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return child;
        return null;
    }
}
#endif