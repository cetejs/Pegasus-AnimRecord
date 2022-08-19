using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AnimRecordWizard : ScriptableWizard {
    [Tooltip("需要录制的游戏物体")]
    public Transform animTrans;
    [Tooltip("每隔多少秒保存关键帧")]
    public float timeInterval = 0.5f;
    [Tooltip("是否需要记录缩放，是相机就不要勾选")]
    public bool isRecordScale;
    [Tooltip("保存的文件目录（Assets/AnimRecord/xxx）")]
    public string fileName = "test";
    [Tooltip("是否开启录制（默认为 True）")]
    public bool isRecording = true;
    private float timer;

    private readonly List<Vector3> posList = new List<Vector3>(1024);
    private readonly List<Vector3> rotList = new List<Vector3>(1024);
    private readonly List<Vector3> sclList = new List<Vector3>(1024);

    [MenuItem("Animator/动画录制工具")]
    static void CreateWizard() {
        var wizard = DisplayWizard<AnimRecordWizard>("动画录制工具（仅支持选中的 Transform 录制）");
        wizard.minSize = new Vector2(600, 250);
    }

    protected override bool DrawWizardGUI() {
        GUILayout.Label("【工具说明 ： 把需要录制动画的对象拖入 animTrans，在游戏运行（暂停不算）的情况下，开启 isRecording】");
        var isDrawWizardGUI = base.DrawWizardGUI();
        GUILayout.Label($"已经录制：{posList.Count} 个关键帧");
        return isDrawWizardGUI;
    }

    private void OnWizardCreate() {
        var clip = new AnimationClip();
        SetTransformCurve(clip, posList, "m_LocalPosition");
        SetTransformCurve(clip, rotList, "m_LocalEulerAnglesRaw");
        if (isRecordScale) {
            SetTransformCurve(clip, sclList, "m_LocalScale");
        }

        var dirPath = Path.Combine(Application.dataPath, "AnimRecord");
        var filePath = $"{dirPath}/{fileName}.anim";
        var animPath = $"Assets/AnimRecord/{fileName}.anim";
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        if (File.Exists(filePath)) {
            File.Delete(filePath);
        }

        AssetDatabase.CreateAsset(clip, animPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"动画录制完成：{animPath}，共计关键帧 {posList.Count}个， 共计时长：{posList.Count * timeInterval}s");
    }

    private void SetTransformCurve(AnimationClip clip, List<Vector3> dataList, string propertyName) {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();

        for (var i = 0; i < dataList.Count; i++) {
            var data = dataList[i];
            x.AddKey(timeInterval * i, data.x);
            y.AddKey(timeInterval * i, data.y);
            z.AddKey(timeInterval * i, data.z);
        }

        clip.SetCurve("", typeof(Transform), string.Concat(propertyName, ".x"), x);
        clip.SetCurve("", typeof(Transform), string.Concat(propertyName, ".y"), y);
        clip.SetCurve("", typeof(Transform), string.Concat(propertyName, ".z"), z);
    }

    private float WrapPi(float a) {
        if (Mathf.Abs(a) > 180) {
            return a - 360 * Mathf.Floor((a + 180) / 360f);
        }

        return a;
    }

    private float WrapMinDiff(float a, float b) {
        var diff = (b - a) % 360;
        if (diff > 180) {
            diff = 360 - diff;
        } else if (diff < -180) {
            diff = 360 + diff;
        }

        return a + diff;
    }

    private void Update() {
        if (!Application.isPlaying) {
            return;
        }

        if (!isRecording) {
            return;
        }

        if (animTrans == null) {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= timeInterval) {
            timer -= timeInterval;
            var pos = animTrans.localPosition;
            var rot = animTrans.localEulerAngles;
            var scl = animTrans.localScale;
            rot.x = WrapPi(rot.x);
            rot.z = WrapPi(rot.z);

            if (rotList.Count > 0) {
                rot.y = WrapMinDiff(rotList[rotList.Count - 1].y, rot.y);
            } else {
                rot.y = WrapPi(rot.y);
            }

            posList.Add(pos);
            rotList.Add(rot);
            if (isRecordScale) {
                sclList.Add(scl);
            }
            
            Repaint();
        }
    }
}