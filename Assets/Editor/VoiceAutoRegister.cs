
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class VoiceAutoRegister : Editor
{
    private const string RootFolder = "Assets/Audio/CV";

    public override void OnInspectorGUI()
    {
        // 通常のAudioManager Inspectorを表示
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // VOICE自動登録ボタン
        if (GUILayout.Button("VOICEを自動登録"))
        {
            RegisterVoices();
        }
    }

    private void RegisterVoices()
    {
        AudioManager manager = (AudioManager)target;

        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            Debug.LogError(
                $"VOICEフォルダが見つかりません: {RootFolder}"
            );
            return;
        }

        // 指定フォルダ内のAudioClipを検索
        string[] guids = AssetDatabase.FindAssets(
            "t:AudioClip",
            new[] { RootFolder }
        );

        List<SoundEntry> entries = new List<SoundEntry>();
        HashSet<string> usedKeys = new HashSet<string>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // Assets/Audio/CV/キャラクター名/音声ファイル
            string[] parts = assetPath.Split('/');

            // キャラクター別フォルダにある音声だけを対象にする
            if (parts.Length != 5 ||
                parts[0] != "Assets" ||
                parts[1] != "Audio" ||
                parts[2] != "CV")
            {
                continue;
            }

            string characterName = parts[3];
            string fileName = Path.GetFileNameWithoutExtension(
                assetPath
            );

            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip == null)
            {
                continue;
            }

            // 例：マインガール_roll1
            string key = characterName + "_" + fileName;

            // 同じキーの重複登録を防止
            if (!usedKeys.Add(key))
            {
                Debug.LogWarning(
                    $"VOICEキーが重複しています: {key}"
                );
                continue;
            }

            SoundEntry entry = new SoundEntry();
            entry.key = key;
            entry.clip = clip;

            entries.Add(entry);
        }

        // キー順に並べてInspector上で見やすくする
        entries.Sort((a, b) =>
            string.Compare(a.key, b.key, System.StringComparison.Ordinal)
        );

        // VOICEリストだけを更新。SEリストには触れない
        Undo.RecordObject(manager, "Auto Register Voices");
        manager.voiceSounds = entries.ToArray();

        EditorUtility.SetDirty(manager);

        Debug.Log(
            $"VOICE自動登録完了：{entries.Count}件"
        );
    }
}
