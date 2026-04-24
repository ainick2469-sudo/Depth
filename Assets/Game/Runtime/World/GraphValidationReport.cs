using System.Collections.Generic;
using System.Text;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class GraphValidationAttemptResult
    {
        public int attemptNumber;
        public int attemptSeed;
        public string layoutSignature;
        public DungeonLayoutGraph graph;
        public readonly List<string> failures = new List<string>();
        public readonly List<string> warnings = new List<string>();

        public bool IsValid => failures.Count == 0;
    }

    public sealed class GraphValidationReport
    {
        public int floorIndex;
        public int seed;
        public int attemptCount;
        public string layoutSignature;
        public readonly List<string> failures = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public readonly List<GraphValidationAttemptResult> attempts = new List<GraphValidationAttemptResult>();

        public bool IsValid => failures.Count == 0;
        public bool HasWarnings => warnings.Count > 0;

        public void AdoptAttempt(GraphValidationAttemptResult attempt)
        {
            layoutSignature = attempt != null ? attempt.layoutSignature : string.Empty;
            failures.Clear();
            warnings.Clear();

            if (attempt == null)
            {
                return;
            }

            failures.AddRange(attempt.failures);
            warnings.AddRange(attempt.warnings);
        }

        public string ToSummaryString(int maxReasons = 5)
        {
            string state = IsValid ? "VALID" : "INVALID";
            StringBuilder builder = new StringBuilder();
            builder.Append("Graph generation ");
            builder.Append(state);
            builder.Append(" | Floor ");
            builder.Append(floorIndex);
            builder.Append(" | Seed ");
            builder.Append(seed);
            builder.Append(" | Attempts ");
            builder.Append(attemptCount);

            if (!string.IsNullOrWhiteSpace(layoutSignature))
            {
                builder.Append(" | LayoutSig ");
                builder.Append(layoutSignature);
            }

            List<string> reasons = failures.Count > 0 ? failures : CollectReasons(attempt => attempt.failures);
            if (reasons.Count > 0)
            {
                builder.Append(" | Reasons ");
                builder.Append(GetGroupedSummary(reasons, maxReasons));
            }

            List<string> warningReasons = warnings.Count > 0 ? warnings : CollectReasons(attempt => attempt.warnings);
            if (warningReasons.Count > 0)
            {
                builder.Append(" | WarningReasons ");
                builder.Append(GetGroupedSummary(warningReasons, maxReasons));
            }

            return builder.ToString();
        }

        public void Log(string prefix)
        {
            string message = string.IsNullOrWhiteSpace(prefix)
                ? ToSummaryString()
                : $"{prefix} {ToSummaryString()}";

            if (IsValid)
            {
                if (HasWarnings)
                {
                    Debug.LogWarning(message);
                }
                else
                {
                    Debug.Log(message);
                }

                return;
            }

            Debug.LogWarning(message);
            for (int i = 0; i < attempts.Count; i++)
            {
                GraphValidationAttemptResult attempt = attempts[i];
                Debug.LogWarning(
                    $"Graph attempt {attempt.attemptNumber} seed {attempt.attemptSeed} | " +
                    $"LayoutSig {attempt.layoutSignature} | " +
                    $"{GetGroupedSummary(attempt.failures, 5)}");
            }
        }

        private List<string> CollectReasons(System.Func<GraphValidationAttemptResult, List<string>> selector)
        {
            List<string> reasons = new List<string>();
            for (int i = 0; i < attempts.Count; i++)
            {
                reasons.AddRange(selector(attempts[i]));
            }

            return reasons;
        }

        private static string GetGroupedSummary(List<string> reasons, int maxReasons)
        {
            if (reasons == null || reasons.Count == 0)
            {
                return "None";
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int i = 0; i < reasons.Count; i++)
            {
                string reason = string.IsNullOrWhiteSpace(reasons[i]) ? "Unknown reason." : reasons[i];
                if (!counts.TryAdd(reason, 1))
                {
                    counts[reason]++;
                }
            }

            List<KeyValuePair<string, int>> ordered = new List<KeyValuePair<string, int>>(counts);
            ordered.Sort((left, right) =>
            {
                int countCompare = right.Value.CompareTo(left.Value);
                return countCompare != 0
                    ? countCompare
                    : string.CompareOrdinal(left.Key, right.Key);
            });

            StringBuilder builder = new StringBuilder();
            int limit = Mathf.Min(maxReasons, ordered.Count);
            for (int i = 0; i < limit; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(ordered[i].Key);
                if (ordered[i].Value > 1)
                {
                    builder.Append(" x");
                    builder.Append(ordered[i].Value);
                }
            }

            return builder.ToString();
        }
    }

    internal static class DungeonLayoutSignatureUtility
    {
        public static string BuildSignature(DungeonLayoutGraph graph, FloorState floorState)
        {
            int floorIndex = floorState != null ? Mathf.Max(1, floorState.floorIndex) : 1;
            int seed = floorState != null ? floorState.floorSeed : 0;
            return BuildSignature(graph, floorIndex, seed);
        }

        public static string BuildSignature(DungeonLayoutGraph graph, int floorIndex, int seed)
        {
            if (graph == null)
            {
                return $"F{floorIndex}|S{seed}|NullGraph";
            }

            List<string> nodeParts = new List<string>(graph.nodes.Count);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                nodeParts.Add(
                    $"{node.nodeKind}:{node.nodeId}@{node.gridPosition.x},{node.gridPosition.y}:{node.roomTemplate}:{node.rotationQuarterTurns}");
            }

            nodeParts.Sort();
            return
                $"F{floorIndex}|S{seed}|N{graph.nodes.Count}|E{graph.edges.Count}|" +
                $"Entry={DescribeSpecial(graph, graph.entryHubNodeId)}|" +
                $"Up={DescribeSpecial(graph, graph.transitUpNodeId)}|" +
                $"Down={DescribeSpecial(graph, graph.transitDownNodeId)}|" +
                string.Join("|", nodeParts);
        }

        private static string DescribeSpecial(DungeonLayoutGraph graph, string nodeId)
        {
            DungeonNode node = graph != null ? graph.GetNode(nodeId) : null;
            return node == null
                ? $"{nodeId}@missing"
                : $"{node.nodeId}@{node.gridPosition.x},{node.gridPosition.y}";
        }
    }
}
