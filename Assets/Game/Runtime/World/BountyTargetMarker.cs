using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class BountyTargetMarker : MonoBehaviour
    {
        [SerializeField] private string bountyId = string.Empty;
        [SerializeField] private string bountyTitle = string.Empty;
        [SerializeField] private string targetName = string.Empty;

        public string BountyId => bountyId;
        public string BountyTitle => bountyTitle;
        public string TargetName => targetName;

        public void Configure(string id, string title, string name)
        {
            bountyId = id ?? string.Empty;
            bountyTitle = title ?? string.Empty;
            targetName = string.IsNullOrWhiteSpace(name) ? bountyTitle : name;
            CreateLabel();
        }

        private void CreateLabel()
        {
            if (string.IsNullOrWhiteSpace(targetName) || transform.Find("BountyTargetLabel") != null)
            {
                return;
            }

            WorldLabelBillboard.Create(
                transform,
                "BountyTargetLabel",
                targetName,
                Vector3.up * 2.65f,
                new Color(1f, 0.32f, 0.22f, 1f),
                28f,
                true);
        }
    }
}
