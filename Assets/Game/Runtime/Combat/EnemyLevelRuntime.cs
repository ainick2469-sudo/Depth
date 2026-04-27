using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public sealed class EnemyLevelRuntime : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private string displayName = string.Empty;

        public int Level => level;
        public string DisplayName => displayName;

        public void Configure(int enemyLevel, string name, Color labelColor)
        {
            level = Mathf.Max(1, enemyLevel);
            displayName = string.IsNullOrWhiteSpace(name) ? $"Enemy Lv. {level}" : name;
            WorldLabelBillboard label = WorldLabelBillboard.Create(transform, "EnemyLevelLabel", displayName, new Vector3(0f, 2.3f, 0f), labelColor, 18f, true);
            label.ConfigureOcclusionRoot(transform);
            TextMesh text = label.GetComponent<TextMesh>();
            if (text != null)
            {
                text.characterSize = 0.18f;
                text.fontSize = 34;
            }
        }
    }
}
