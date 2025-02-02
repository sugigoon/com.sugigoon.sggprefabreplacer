using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SGGPrefabReplacerTool : EditorWindow
{
    public GameObject[] sourcePrefabs;
    public GameObject[] targetPrefabs;

    private Vector2 scrollPosition;

    private bool copyAll = true;
    private bool copyMaterials = true;
    private bool copyActiveState = true;
    private bool copyBlendShapes = true;
    private bool copyBounds = true;
    private bool copyNames = true;

    private string[] languages = { "한국어", "English", "日本語" };
    private int selectedLanguage = 0;

    private string namePrefix = ""; // 이름 앞에 추가
    private string nameSuffix = ""; // 이름 뒤에 추가

    [MenuItem("Tools/SGG Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<SGGPrefabReplacerTool>("SGG Prefab Replacer").minSize = new Vector2(450, 400);
    }

    private SerializedObject serializedObject;

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
    }

    private void OnGUI()
    {
        // 🔹 전체 UI 스크롤 가능 영역
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label(GetText("Title"), EditorStyles.boldLabel);
        serializedObject.Update();

        // 🔹 소스 프리팹과 타겟 프리팹 입력
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sourcePrefabs"), new GUIContent(GetText("SourcePrefabs")), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPrefabs"), new GUIContent(GetText("TargetPrefabs")), true);

        GUILayout.Space(10);

        // 🔹 데이터 복사 체크박스
        GUILayout.BeginVertical("box");

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal,
        };

        GUILayout.BeginHorizontal();
        bool previousCopyAll = copyAll;
        copyAll = EditorGUILayout.Toggle(copyAll, GUILayout.Width(20));
        GUILayout.Label(GetText("CopyAll"), labelStyle);
        GUILayout.EndHorizontal();

        if (copyAll && !previousCopyAll)
        {
            copyMaterials = true;
            copyActiveState = true;
            copyBlendShapes = true;
            copyBounds = true;
            copyNames = true;
        }

        DrawCheckbox(ref copyMaterials, GetText("CopyMaterials"), labelStyle);
        DrawCheckbox(ref copyActiveState, GetText("CopyActiveState"), labelStyle);
        DrawCheckbox(ref copyBlendShapes, GetText("CopyBlendShapes"), labelStyle);
        DrawCheckbox(ref copyBounds, GetText("CopyBounds"), labelStyle);
        DrawCheckbox(ref copyNames, GetText("CopyNames"), labelStyle);

        GUILayout.EndVertical();

        // 🔹 추가 이름 옵션 (프리팹 이름 복사가 체크된 경우에만 표시)
        if (copyNames)
        {
            GUILayout.Space(10);
            GUILayout.Label(GetText("CustomNameLabel"));
            namePrefix = EditorGUILayout.TextField(GetText("NamePrefix"), namePrefix); // 이름 앞
            nameSuffix = EditorGUILayout.TextField(GetText("NameSuffix"), nameSuffix); // 이름 뒤
        }

        GUILayout.Space(10);

        // 🔹 프리팹 교체 버튼
        if (GUILayout.Button(GetText("ReplaceMaterials"), GUILayout.Height(30)))
        {
            ReplaceMaterials();
        }

        GUILayout.Space(10);

        // 🔹 언어 선택
        GUILayout.BeginHorizontal();
        GUILayout.Label(GetText("Language"), GUILayout.Width(100));
        selectedLanguage = EditorGUILayout.Popup(selectedLanguage, languages);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView(); // 스크롤 종료
    }

    private void DrawCheckbox(ref bool value, string label, GUIStyle labelStyle)
    {
        GUILayout.BeginHorizontal();
        value = EditorGUILayout.Toggle(value, GUILayout.Width(20));
        GUILayout.Label(label, labelStyle);
        GUILayout.EndHorizontal();
    }

    private void ReplaceMaterials()
    {
        if (sourcePrefabs == null || targetPrefabs == null || sourcePrefabs.Length != targetPrefabs.Length)
        {
            Debug.LogError(GetText("ErrorPrefab"));
            return;
        }

        Debug.Log($"{GetText("StartProcess")} {sourcePrefabs.Length}");

        for (int i = 0; i < sourcePrefabs.Length; i++)
        {
            GameObject sourcePrefab = sourcePrefabs[i];
            GameObject targetPrefab = targetPrefabs[i];

            if (sourcePrefab == null || targetPrefab == null)
            {
                Debug.LogWarning($"{GetText("WarningNull")} {i}");
                continue;
            }

            if (copyActiveState) CopyActiveState(sourcePrefab, targetPrefab);
            if (copyMaterials) CopyMaterials(sourcePrefab, targetPrefab);
            if (copyBlendShapes) CopyBlendShapes(sourcePrefab, targetPrefab);
            if (copyBounds) CopyBounds(sourcePrefab, targetPrefab);
            if (copyNames) CopyPrefabName(sourcePrefab, targetPrefab);

            Debug.Log($"{GetText("SavePrefab")} {targetPrefab.name}");
            PrefabUtility.SaveAsPrefabAsset(targetPrefab, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetPrefab));
        }

        Debug.Log(GetText("ProcessComplete"));
    }

    private void CopyPrefabName(GameObject source, GameObject target)
    {
        string targetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
        if (!string.IsNullOrEmpty(targetPath))
        {
            string newName = source.name;

            if (!string.IsNullOrEmpty(namePrefix))
            {
                newName = $"{namePrefix}{newName}";
            }
            if (!string.IsNullOrEmpty(nameSuffix))
            {
                newName = $"{newName}{nameSuffix}";
            }

            AssetDatabase.RenameAsset(targetPath, newName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void CopyActiveState(GameObject source, GameObject target)
    {
        foreach (Transform sourceTransform in source.GetComponentsInChildren<Transform>(true))
        {
            Transform targetTransform = target.transform.Find(sourceTransform.name);
            if (targetTransform != null)
            {
                targetTransform.gameObject.SetActive(sourceTransform.gameObject.activeSelf);
            }
        }
    }

    private void CopyMaterials(GameObject source, GameObject target)
    {
        var sourceRenderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var targetRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (i >= sourceRenderers.Length) break;
            targetRenderers[i].sharedMaterials = sourceRenderers[i].sharedMaterials;
        }
    }

    private void CopyBlendShapes(GameObject source, GameObject target)
    {
        foreach (var targetRenderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            foreach (var sourceRenderer in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (targetRenderer.name == sourceRenderer.name)
                {
                    for (int i = 0; i < sourceRenderer.sharedMesh.blendShapeCount; i++)
                    {
                        string shapeName = sourceRenderer.sharedMesh.GetBlendShapeName(i);
                        int targetIndex = targetRenderer.sharedMesh.GetBlendShapeIndex(shapeName);
                        if (targetIndex != -1)
                        {
                            targetRenderer.SetBlendShapeWeight(targetIndex, sourceRenderer.GetBlendShapeWeight(i));
                        }
                    }
                }
            }
        }
    }

    private void CopyBounds(GameObject source, GameObject target)
    {
        foreach (var sourceRenderer in source.GetComponentsInChildren<Renderer>(true))
        {
            var targetRenderer = target.transform.Find(sourceRenderer.name)?.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                if (sourceRenderer is SkinnedMeshRenderer sourceSMR && targetRenderer is SkinnedMeshRenderer targetSMR)
                {
                    targetSMR.localBounds = sourceSMR.localBounds;
                    targetSMR.transform.position = sourceSMR.transform.position;
                    targetSMR.transform.localScale = sourceSMR.transform.localScale; // 수정됨
                }
                else
                {
                    targetRenderer.transform.position = sourceRenderer.transform.position;
                    targetRenderer.transform.localScale = sourceRenderer.transform.localScale; // 수정됨
                }
            }
        }
    }

    private string GetText(string key)
    {
        Dictionary<string, string[]> localization = new Dictionary<string, string[]>()
        {
            { "Title", new[] { "SGG 프리팹 복사기", "SGG Prefab Replacer", "SGG プレハブコピー" } },
            { "Language", new[] { "언어 선택", "Language", "言語選択" } },
            { "SourcePrefabs", new[] { "소스 프리팹", "Source Prefabs", "ソースプレハブ" } },
            { "TargetPrefabs", new[] { "타겟 프리팹", "Target Prefabs", "ターゲットプレハブ" } },
            { "CopyAll", new[] { "전체 선택", "Select All", "すべて選択" } },
            { "CopyMaterials", new[] { "마테리얼 복사", "Copy Materials", "マテリアルコピー" } },
            { "CopyActiveState", new[] { "오브젝트 활성화 상태 복사", "Copy Active State", "オブジェクトの状態コピー" } },
            { "CopyBlendShapes", new[] { "쉐이프키 복사", "Copy BlendShapes", "ブレンドシェイプコピー" } },
            { "CopyBounds", new[] { "바운드 박스 복사", "Copy Bounds", "バウンドコピー" } },
            { "CopyNames", new[] { "프리팹 이름 복사", "Copy Prefab Names", "プレハブ名コピー" } },
            { "ReplaceMaterials", new[] { "프리팹 교체", "Replace Prefabs", "プレハブ交換" } },
            { "CustomNameLabel", new[] { "추가 이름 옵션", "Custom Name Options", "カスタムネームオプション" } },
            { "NamePrefix", new[] { "이름 앞에 추가", "Name Prefix", "名前の前に追加" } },
            { "NameSuffix", new[] { "이름 뒤에 추가", "Name Suffix", "名前の後に追加" } },
            { "StartProcess", new[] { "작업 시작: ", "Starting process: ", "処理開始: " } },
            { "WarningNull", new[] { "프리팹이 null입니다. Index: ", "Prefab is null. Index: ", "プレハブがnullです。Index: " } },
            { "SavePrefab", new[] { "프리팹 저장 완료: ", "Prefab saved: ", "プレハブ保存完了: " } },
            { "ProcessComplete", new[] { "작업 완료!", "Process Completed!", "処理完了!" } }
        };

        return localization[key][selectedLanguage];
    }
}
