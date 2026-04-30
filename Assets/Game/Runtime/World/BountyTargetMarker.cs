using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class BountyTargetMarker : MonoBehaviour, IBountyTargetMarker
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
                $"BOUNTY\n{targetName}",
                Vector3.up * 2.65f,
                new Color(1f, 0.32f, 0.22f, 1f),
                34f,
                true);

            WorldLabelBillboard.Create(
                transform,
                "BountyTargetIcon",
                "!",
                Vector3.up * 3.35f,
                new Color(1f, 0.82f, 0.18f, 1f),
                38f,
                true);
        }
    }
}
