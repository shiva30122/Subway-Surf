using UnityEditor;
using UnityEngine;
using SubwayDash.Collectables;

namespace SubwayDash.Editor
{
    [CustomEditor(typeof(GoldCoin))]
    public class GoldCoinEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GoldCoin coin = (GoldCoin)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Test", EditorStyles.boldLabel);

            // Preview button - tests rotation/bob tweaks live in Edit mode
            if (GUILayout.Button("▶ Preview Rotation + Bob (Editor)", GUILayout.Height(30)))
            {
                coin.PreviewAnimation();
            }
            if (GUILayout.Button("⏸ Reset Coin Transform"))
            {
                coin.ResetPreview();
            }

            EditorGUILayout.HelpBox("Tweak rotationSpeed (90), bobSpeed (2), bobHeight (0.15) above. Click Preview to spin coin in Scene view. Reset to restore.", MessageType.Info);

            // Live sliders for quick tweak
            EditorGUILayout.Space();
            coin.rotationSpeed = EditorGUILayout.Slider("Rotation Speed", coin.rotationSpeed, 0f, 360f);
            coin.bobSpeed = EditorGUILayout.Slider("Bob Speed", coin.bobSpeed, 0f, 10f);
            coin.bobHeight = EditorGUILayout.Slider("Bob Height", coin.bobHeight, 0f, 0.5f);

            if (GUI.changed)
                EditorUtility.SetDirty(coin);
        }
    }
}
