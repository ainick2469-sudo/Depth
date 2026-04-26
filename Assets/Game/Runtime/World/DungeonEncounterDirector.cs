using System;
using System.Collections.Generic;
using FrontierDepths.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.World
{
    public sealed class DungeonEncounterDirector
    {
        public const string EncounterEnemiesRootName = "EncounterEnemies";
        public const string SpawnSourceEncounterDirector = "EncounterDirectorLite";
        private const int FloorOneBatCap = 3;
        private const float BruteMinimumRoomFootprint = 1600f;

        private readonly Transform runtimeRoot;
        private readonly EncounterDropService dropService;
        private readonly List<EnemyHealth> livingEnemies = new List<EnemyHealth>();
        private DungeonEncounterSummary summary = DungeonEncounterSummary.Empty;

        public DungeonEncounterDirector(Transform runtimeRoot)
        {
            this.runtimeRoot = runtimeRoot;
            dropService = new EncounterDropService(runtimeRoot);
        }

        public DungeonEncounterSummary Summary => summary;
        public EncounterDropService DropService => dropService;

        public DungeonEncounterSummary Spawn(DungeonBuildResult buildResult, IList<Vector3> occupiedPositions, bool clearExisting)
        {
            if (runtimeRoot == null || buildResult == null || buildResult.isEmergencyDebugBuild)
            {
                summary = DungeonEncounterSummary.Empty;
                return summary;
            }

            if (clearExisting)
            {
                Clear(true);
            }

            DungeonEncounterPlan plan = BuildPlan(buildResult, occupiedPositions, buildResult.seed);
            summary = plan.ToSummary();
            Transform enemyRoot = GetOrCreateEnemyRoot(runtimeRoot);
            for (int i = 0; i < plan.spawns.Count; i++)
            {
                DungeonEncounterSpawn spawn = plan.spawns[i];
                GameObject enemy = CreateEnemy(enemyRoot, spawn.position, spawn.definition);
                EnemyHealth enemyHealth = enemy != null ? enemy.GetComponent<EnemyHealth>() : null;
                if (enemyHealth == null)
                {
                    continue;
                }

                RegisterEnemy(enemyHealth);
                dropService.RegisterEnemy(enemyHealth);
            }

            summary.livingEnemyCount = livingEnemies.Count;
            return summary;
        }

        public void Clear(bool immediate)
        {
            dropService.SuppressDrops = true;
            for (int i = livingEnemies.Count - 1; i >= 0; i--)
            {
                if (livingEnemies[i] != null)
                {
                    livingEnemies[i].Died -= HandleEnemyDied;
                    dropService.UnregisterEnemy(livingEnemies[i]);
                }
            }

            livingEnemies.Clear();
            Transform enemyRoot = runtimeRoot != null ? runtimeRoot.Find(EncounterEnemiesRootName) : null;
            if (enemyRoot != null)
            {
                for (int i = enemyRoot.childCount - 1; i >= 0; i--)
                {
                    GameObject child = enemyRoot.GetChild(i).gameObject;
                    if (immediate)
                    {
                        Object.DestroyImmediate(child);
                    }
                    else
                    {
                        Object.Destroy(child);
                    }
                }
            }

            dropService.Clear(immediate);
            dropService.SuppressDrops = false;
            summary.livingEnemyCount = 0;
        }

        public static DungeonEncounterPlan BuildPlan(DungeonBuildResult buildResult, IList<Vector3> occupiedPositions, int seed)
        {
            DungeonEncounterPlan plan = new DungeonEncounterPlan
            {
                floorIndex = buildResult != null ? buildResult.floorIndex : 0,
                spawnSource = SpawnSourceEncounterDirector
            };

            if (buildResult == null || buildResult.floorIndex > 5)
            {
                plan.warning = buildResult == null
                    ? "Encounter Director skipped: dungeon build missing."
                    : $"Encounter Director Lite has no floor {buildResult.floorIndex} budget yet.";
                return plan;
            }

            System.Random random = new System.Random(seed == 0 ? buildResult.floorIndex * 7919 : seed);
            EncounterBudget budget = GetBudget(buildResult.floorIndex, random);
            plan.requestedBudget = budget.requested;
            Dictionary<string, List<DungeonSpawnPointRecord>> safeSpawnsByRoom = CollectSafeSpawns(buildResult, occupiedPositions);
            List<DungeonEncounterRoomCandidate> candidates = BuildRoomCandidates(buildResult, safeSpawnsByRoom);
            plan.eligibleRoomCount = candidates.Count;
            if (candidates.Count == 0)
            {
                plan.warning = "Encounter Director underfilled: no safe EnemyMelee spawn points were available.";
                return plan;
            }

            List<EnemyDefinition> definitions = EnemyCatalog.CreateDefinitionsForFloor(buildResult.floorIndex);
            AllocateRooms(plan, candidates, budget, definitions, random);
            plan.emptyEligibleRoomCount = Mathf.Max(0, candidates.Count - plan.assignments.Count);
            for (int assignmentIndex = 0; assignmentIndex < plan.assignments.Count; assignmentIndex++)
            {
                DungeonEncounterRoomAssignment assignment = plan.assignments[assignmentIndex];
                DungeonEncounterRoomCandidate candidate = candidates.Find(item => item.room.nodeId == assignment.roomId);
                if (candidate == null)
                {
                    continue;
                }

                int spawnCount = Mathf.Min(assignment.plannedCount, Mathf.Min(candidate.safeSpawns.Count, assignment.archetypes.Count));
                List<DungeonSpawnPointRecord> spreadSpawns = SelectSpreadSpawns(candidate.safeSpawns, spawnCount);
                if (spreadSpawns.Count == 0)
                {
                    continue;
                }

                for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
                {
                    EnemyDefinition definition = FindDefinition(definitions, assignment.archetypes[spawnIndex]);
                    if (definition == null)
                    {
                        continue;
                    }

                    DungeonSpawnPointRecord spawnPoint = spreadSpawns[spawnIndex];
                    assignment.spawnedCount++;
                    plan.spawns.Add(new DungeonEncounterSpawn
                    {
                        roomId = assignment.roomId,
                        templateId = assignment.templateId,
                        role = assignment.role,
                        position = spawnPoint.position,
                        definition = definition
                    });
                    plan.AddArchetype(definition.archetype);
                }
            }

            plan.spawnedCount = plan.spawns.Count;
            if (plan.spawnedCount < budget.min)
            {
                AppendWarning(plan, $"Encounter Director underfilled floor {buildResult.floorIndex}: spawned {plan.spawnedCount}/{budget.requested}.");
            }

            return plan;
        }

        public static int GetRoomCapacity(DungeonRoomBuildRecord room, int safeSpawnCount)
        {
            if (room == null || safeSpawnCount <= 0)
            {
                return 0;
            }

            int footprintCapacity = room.footprintArea >= 1600f ? 3 : (room.footprintArea >= 900f ? 2 : 1);
            if (room.roomType == DungeonNodeKind.Landmark)
            {
                footprintCapacity = Mathf.Max(2, footprintCapacity + 1);
            }

            if (room.roomType == DungeonNodeKind.Secret)
            {
                footprintCapacity = Mathf.Min(1, footprintCapacity);
            }

            return Mathf.Clamp(Mathf.Min(footprintCapacity, safeSpawnCount), 0, room.roomType == DungeonNodeKind.Landmark ? 4 : 3);
        }

        public static bool IsSafeRoomType(DungeonNodeKind roomType)
        {
            return roomType == DungeonNodeKind.EntryHub ||
                   roomType == DungeonNodeKind.TransitUp ||
                   roomType == DungeonNodeKind.TransitDown;
        }

        private static Dictionary<string, List<DungeonSpawnPointRecord>> CollectSafeSpawns(DungeonBuildResult buildResult, IList<Vector3> occupiedPositions)
        {
            Dictionary<string, List<DungeonSpawnPointRecord>> result = new Dictionary<string, List<DungeonSpawnPointRecord>>();
            List<DungeonSpawnPointRecord> selected = new List<DungeonSpawnPointRecord>();
            for (int i = 0; i < buildResult.spawnPoints.Count; i++)
            {
                DungeonSpawnPointRecord spawnPoint = buildResult.spawnPoints[i];
                if (spawnPoint.category != DungeonSpawnPointCategory.EnemyMelee ||
                    !DungeonSceneController.IsCombatTestEnemySpawnSafe(buildResult, spawnPoint, occupiedPositions, selected, true))
                {
                    continue;
                }

                selected.Add(spawnPoint);
                if (!result.TryGetValue(spawnPoint.nodeId, out List<DungeonSpawnPointRecord> roomSpawns))
                {
                    roomSpawns = new List<DungeonSpawnPointRecord>();
                    result.Add(spawnPoint.nodeId, roomSpawns);
                }

                roomSpawns.Add(spawnPoint);
            }

            foreach (KeyValuePair<string, List<DungeonSpawnPointRecord>> pair in result)
            {
                pair.Value.Sort((left, right) => right.score.CompareTo(left.score));
            }

            return result;
        }

        private static List<DungeonEncounterRoomCandidate> BuildRoomCandidates(
            DungeonBuildResult buildResult,
            Dictionary<string, List<DungeonSpawnPointRecord>> safeSpawnsByRoom)
        {
            List<DungeonEncounterRoomCandidate> candidates = new List<DungeonEncounterRoomCandidate>();
            for (int i = 0; i < buildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = buildResult.rooms[i];
                if (room == null || IsSafeRoomType(room.roomType) || !safeSpawnsByRoom.TryGetValue(room.nodeId, out List<DungeonSpawnPointRecord> safeSpawns))
                {
                    continue;
                }

                int capacity = GetRoomCapacity(room, safeSpawns.Count);
                if (capacity <= 0)
                {
                    continue;
                }

                candidates.Add(new DungeonEncounterRoomCandidate
                {
                    room = room,
                    safeSpawns = safeSpawns,
                    capacity = capacity,
                    distanceFromPlayer = Vector3.Distance(room.bounds.center, buildResult.playerSpawn)
                });
            }

            candidates.Sort((left, right) =>
            {
                int roleCompare = GetRoomPriority(left.room).CompareTo(GetRoomPriority(right.room));
                if (roleCompare != 0)
                {
                    return roleCompare;
                }

                return left.distanceFromPlayer.CompareTo(right.distanceFromPlayer);
            });

            return candidates;
        }

        private static void AllocateRooms(
            DungeonEncounterPlan plan,
            List<DungeonEncounterRoomCandidate> candidates,
            EncounterBudget budget,
            List<EnemyDefinition> definitions,
            System.Random random)
        {
            int remaining = budget.requested;
            if (remaining <= 0)
            {
                return;
            }

            HashSet<string> usedRooms = new HashSet<string>();
            int floorOneBatCount = 0;
            int soloCount = 0;
            int emptyRoomReserve = GetEmptyRoomReserve(candidates.Count);
            int targetPopulatedRooms = Mathf.Max(1, candidates.Count - emptyRoomReserve);

            if (plan.floorIndex == 1)
            {
                DungeonEncounterRoomCandidate firstLight = FindFirstCombatCandidate(candidates, usedRooms);
                if (firstLight != null)
                {
                    EncounterTemplate firstTemplate = FindBestTemplate(
                        firstLight,
                        definitions,
                        plan.floorIndex,
                        remaining,
                        1,
                        2,
                        floorOneBatCount,
                        false,
                        random);
                    if (TryAddTemplateAssignment(plan, firstLight, firstTemplate, usedRooms, ref remaining, ref floorOneBatCount, ref soloCount))
                    {
                        // Floor 1 gets a readable first fight before heavier groups claim budget.
                    }
                }

                TryAssignLandmarkGroup(plan, candidates, definitions, usedRooms, ref remaining, ref floorOneBatCount, ref soloCount, random);

                bool hasThreePack = plan.assignments.Exists(assignment => assignment.plannedCount >= 3);
                if (!hasThreePack)
                {
                    bool addedThreePack = TryAssignRequiredGroup(
                        plan,
                        candidates,
                        definitions,
                        usedRooms,
                        ref remaining,
                        ref floorOneBatCount,
                        ref soloCount,
                        3,
                        random);
                    if (!addedThreePack)
                    {
                        AppendWarning(plan, "Floor 1 3-pack underfilled: no safe room capacity or budget remained.");
                    }
                }

                bool hasTwoPack = plan.assignments.Exists(assignment => assignment.plannedCount == 2);
                if (!hasTwoPack)
                {
                    bool addedTwoPack = TryAssignRequiredGroup(
                        plan,
                        candidates,
                        definitions,
                        usedRooms,
                        ref remaining,
                        ref floorOneBatCount,
                        ref soloCount,
                        2,
                        random);
                    if (!addedTwoPack)
                    {
                        AppendWarning(plan, "Floor 1 2-pack underfilled: no safe room capacity or budget remained.");
                    }
                }
            }
            else
            {
                TryAssignLandmarkGroup(plan, candidates, definitions, usedRooms, ref remaining, ref floorOneBatCount, ref soloCount, random);

                bool addedThreePack = TryAssignRequiredGroup(
                    plan,
                    candidates,
                    definitions,
                    usedRooms,
                    ref remaining,
                    ref floorOneBatCount,
                    ref soloCount,
                    3,
                    random);
                if (!addedThreePack)
                {
                    AppendWarning(plan, $"Floor {plan.floorIndex} 3-pack underfilled: no safe room capacity or budget remained.");
                }
            }

            List<DungeonEncounterRoomCandidate> fillOrder = new List<DungeonEncounterRoomCandidate>(candidates);
            fillOrder.Sort((left, right) =>
            {
                int capacityCompare = right.capacity.CompareTo(left.capacity);
                if (capacityCompare != 0)
                {
                    return capacityCompare;
                }

                int typeCompare = GetRoomPriority(left.room).CompareTo(GetRoomPriority(right.room));
                if (typeCompare != 0)
                {
                    return typeCompare;
                }

                return left.distanceFromPlayer.CompareTo(right.distanceFromPlayer);
            });

            for (int i = 0; i < fillOrder.Count && remaining > 0; i++)
            {
                if (usedRooms.Contains(fillOrder[i].room.nodeId))
                {
                    continue;
                }

                if (usedRooms.Count >= targetPopulatedRooms && plan.spawnedCount >= budget.min)
                {
                    break;
                }

                int minSize = remaining >= 2 ? 2 : 1;
                EncounterTemplate template = FindBestTemplate(
                    fillOrder[i],
                    definitions,
                    plan.floorIndex,
                    remaining,
                    minSize,
                    fillOrder[i].capacity,
                    floorOneBatCount,
                    false,
                    random);
                if (template == null &&
                    (remaining == 1 || plan.spawnedCount < budget.min) &&
                    soloCount < GetSoloLimit(plan.floorIndex, targetPopulatedRooms))
                {
                    template = FindBestTemplate(
                        fillOrder[i],
                        definitions,
                        plan.floorIndex,
                        remaining,
                        1,
                        1,
                        floorOneBatCount,
                        true,
                        random);
                }

                if (TryAddTemplateAssignment(plan, fillOrder[i], template, usedRooms, ref remaining, ref floorOneBatCount, ref soloCount))
                {
                    continue;
                }
            }
        }

        private static bool TryAssignRequiredGroup(
            DungeonEncounterPlan plan,
            List<DungeonEncounterRoomCandidate> candidates,
            List<EnemyDefinition> definitions,
            HashSet<string> usedRooms,
            ref int remaining,
            ref int floorOneBatCount,
            ref int soloCount,
            int groupSize,
            System.Random random)
        {
            List<DungeonEncounterRoomCandidate> sorted = new List<DungeonEncounterRoomCandidate>(candidates);
            sorted.Sort((left, right) =>
            {
                int landmarkCompare = (right.room.roomType == DungeonNodeKind.Landmark ? 1 : 0).CompareTo(left.room.roomType == DungeonNodeKind.Landmark ? 1 : 0);
                if (landmarkCompare != 0)
                {
                    return landmarkCompare;
                }

                int capacityCompare = right.capacity.CompareTo(left.capacity);
                if (capacityCompare != 0)
                {
                    return capacityCompare;
                }

                return right.distanceFromPlayer.CompareTo(left.distanceFromPlayer);
            });

            for (int i = 0; i < sorted.Count; i++)
            {
                DungeonEncounterRoomCandidate candidate = sorted[i];
                if (usedRooms.Contains(candidate.room.nodeId) || candidate.capacity < groupSize || remaining < groupSize)
                {
                    continue;
                }

                EncounterTemplate template = FindBestTemplate(
                    candidate,
                    definitions,
                    plan.floorIndex,
                    remaining,
                    groupSize,
                    groupSize,
                    floorOneBatCount,
                    false,
                    random);
                if (TryAddTemplateAssignment(plan, candidate, template, usedRooms, ref remaining, ref floorOneBatCount, ref soloCount))
                {
                    return true;
                }
            }

            return false;
        }

        private static void TryAssignLandmarkGroup(
            DungeonEncounterPlan plan,
            List<DungeonEncounterRoomCandidate> candidates,
            List<EnemyDefinition> definitions,
            HashSet<string> usedRooms,
            ref int remaining,
            ref int floorOneBatCount,
            ref int soloCount,
            System.Random random)
        {
            DungeonEncounterRoomCandidate landmark = candidates.Find(candidate =>
                candidate.room.roomType == DungeonNodeKind.Landmark &&
                !usedRooms.Contains(candidate.room.nodeId) &&
                candidate.capacity >= 2);
            if (landmark == null || remaining < 2)
            {
                return;
            }

            EncounterTemplate template = FindBestTemplate(
                landmark,
                definitions,
                plan.floorIndex,
                remaining,
                2,
                Mathf.Min(landmark.capacity, 4),
                floorOneBatCount,
                false,
                random);
            TryAddTemplateAssignment(plan, landmark, template, usedRooms, ref remaining, ref floorOneBatCount, ref soloCount);
        }

        private static bool TryAddTemplateAssignment(
            DungeonEncounterPlan plan,
            DungeonEncounterRoomCandidate candidate,
            EncounterTemplate template,
            HashSet<string> usedRooms,
            ref int remaining,
            ref int floorOneBatCount,
            ref int soloCount)
        {
            if (template == null ||
                candidate == null ||
                usedRooms.Contains(candidate.room.nodeId) ||
                template.Count <= 0 ||
                template.Count > remaining ||
                template.Count > candidate.capacity)
            {
                return false;
            }

            int batCount = template.CountArchetype(EnemyArchetype.Bat);
            if (plan.floorIndex == 1 && (batCount > 2 || floorOneBatCount + batCount > FloorOneBatCap))
            {
                return false;
            }

            DungeonEncounterRoomAssignment assignment = new DungeonEncounterRoomAssignment
            {
                roomId = candidate.room.nodeId,
                roomType = candidate.room.roomType,
                role = GetRole(candidate.room, template.Count),
                plannedCount = template.Count,
                roomCapacity = candidate.capacity,
                templateId = template.id
            };
            assignment.archetypes.AddRange(template.archetypes);
            plan.assignments.Add(assignment);
            plan.AddTemplate(template.id);
            if (template.Count == 1)
            {
                plan.soloRoomCount++;
                soloCount++;
            }
            else
            {
                plan.groupFightCount++;
            }

            usedRooms.Add(candidate.room.nodeId);
            floorOneBatCount += batCount;
            remaining -= template.Count;
            plan.spawnedCount += template.Count;
            return true;
        }

        private static DungeonEncounterRoomCandidate FindFirstCombatCandidate(List<DungeonEncounterRoomCandidate> candidates, HashSet<string> usedRooms)
        {
            DungeonEncounterRoomCandidate best = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                DungeonEncounterRoomCandidate candidate = candidates[i];
                if (candidate.room.roomType != DungeonNodeKind.Ordinary || usedRooms.Contains(candidate.room.nodeId))
                {
                    continue;
                }

                if (best == null || candidate.distanceFromPlayer < best.distanceFromPlayer)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static EncounterTemplate FindBestTemplate(
            DungeonEncounterRoomCandidate candidate,
            List<EnemyDefinition> definitions,
            int floorIndex,
            int remainingBudget,
            int minSize,
            int maxSize,
            int floorOneBatCount,
            bool allowSolo,
            System.Random random)
        {
            List<EncounterTemplate> templates = CreateEncounterTemplates();
            List<EncounterTemplate> valid = new List<EncounterTemplate>();
            int safeMaxSize = Mathf.Min(maxSize, Mathf.Min(candidate.capacity, remainingBudget));
            for (int i = 0; i < templates.Count; i++)
            {
                EncounterTemplate template = templates[i];
                if (!allowSolo && template.Count == 1)
                {
                    continue;
                }

                if (template.Count < minSize || template.Count > safeMaxSize)
                {
                    continue;
                }

                if (!IsTemplateValidForRoom(template, candidate, definitions, floorIndex, floorOneBatCount))
                {
                    continue;
                }

                valid.Add(template);
            }

            if (valid.Count == 0)
            {
                return null;
            }

            valid.Sort((left, right) =>
            {
                int countCompare = right.Count.CompareTo(left.Count);
                if (countCompare != 0)
                {
                    return countCompare;
                }

                int weightCompare = right.weight.CompareTo(left.weight);
                if (weightCompare != 0)
                {
                    return weightCompare;
                }

                return string.CompareOrdinal(left.id, right.id);
            });

            int largestCount = valid[0].Count;
            List<EncounterTemplate> largestValid = valid.FindAll(template => template.Count == largestCount);
            int topCount = Mathf.Min(3, largestValid.Count);
            return largestValid[Mathf.Clamp(random.Next(0, topCount), 0, largestValid.Count - 1)];
        }

        private static bool IsTemplateValidForRoom(
            EncounterTemplate template,
            DungeonEncounterRoomCandidate candidate,
            List<EnemyDefinition> definitions,
            int floorIndex,
            int floorOneBatCount)
        {
            if (template == null || candidate == null || !template.IsEligibleForFloor(floorIndex))
            {
                return false;
            }

            if (template.Count > candidate.capacity)
            {
                return false;
            }

            int bats = template.CountArchetype(EnemyArchetype.Bat);
            if (floorIndex == 1 && (bats > 2 || floorOneBatCount + bats > FloorOneBatCap))
            {
                return false;
            }

            bool hasBrute = template.Contains(EnemyArchetype.GoblinBrute);
            if (hasBrute && (candidate.capacity < 2 || candidate.room.footprintArea < BruteMinimumRoomFootprint))
            {
                return false;
            }

            if (candidate.room.roomType == DungeonNodeKind.Secret && template.Count > 1)
            {
                return false;
            }

            for (int i = 0; i < template.archetypes.Length; i++)
            {
                EnemyDefinition definition = FindDefinition(definitions, template.archetypes[i]);
                if (definition == null || !definition.IsEligibleForFloor(floorIndex))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<EncounterTemplate> CreateEncounterTemplates()
        {
            return new List<EncounterTemplate>
            {
                new EncounterTemplate("SoloSlime", 1, 3, 1, EnemyArchetype.Slime),
                new EncounterTemplate("SoloBat", 1, 4, 1, EnemyArchetype.Bat),
                new EncounterTemplate("SoloGoblinGrunt", 1, 5, 1, EnemyArchetype.GoblinGrunt),
                new EncounterTemplate("Easy_2Slimes", 1, 3, 7, EnemyArchetype.Slime, EnemyArchetype.Slime),
                new EncounterTemplate("Easy_SlimeBat", 1, 4, 7, EnemyArchetype.Slime, EnemyArchetype.Bat),
                new EncounterTemplate("Medium_GoblinSlime", 1, 5, 6, EnemyArchetype.GoblinGrunt, EnemyArchetype.Slime),
                new EncounterTemplate("Medium_2BatsSlime", 1, 4, 4, EnemyArchetype.Bat, EnemyArchetype.Bat, EnemyArchetype.Slime),
                new EncounterTemplate("Medium_2SlimesBat", 1, 4, 5, EnemyArchetype.Slime, EnemyArchetype.Slime, EnemyArchetype.Bat),
                new EncounterTemplate("Hard_2GoblinGrunts", 2, 5, 4, EnemyArchetype.GoblinGrunt, EnemyArchetype.GoblinGrunt),
                new EncounterTemplate("Hard_Goblin2Slimes", 2, 5, 5, EnemyArchetype.GoblinGrunt, EnemyArchetype.Slime, EnemyArchetype.Slime),
                new EncounterTemplate("Depth_BruteBat", 3, 0, 4, EnemyArchetype.GoblinBrute, EnemyArchetype.Bat),
                new EncounterTemplate("Depth_Brute2Slimes", 3, 0, 5, EnemyArchetype.GoblinBrute, EnemyArchetype.Slime, EnemyArchetype.Slime)
            };
        }

        private static EnemyDefinition FindDefinition(List<EnemyDefinition> definitions, EnemyArchetype archetype)
        {
            return definitions != null ? definitions.Find(definition => definition != null && definition.archetype == archetype) : null;
        }

        private static List<DungeonSpawnPointRecord> SelectSpreadSpawns(List<DungeonSpawnPointRecord> safeSpawns, int count)
        {
            List<DungeonSpawnPointRecord> result = new List<DungeonSpawnPointRecord>();
            if (safeSpawns == null || safeSpawns.Count == 0 || count <= 0)
            {
                return result;
            }

            List<DungeonSpawnPointRecord> remaining = new List<DungeonSpawnPointRecord>(safeSpawns);
            remaining.Sort((left, right) => right.score.CompareTo(left.score));
            result.Add(remaining[0]);
            remaining.RemoveAt(0);
            while (result.Count < count && remaining.Count > 0)
            {
                int bestIndex = 0;
                float bestDistance = -1f;
                for (int i = 0; i < remaining.Count; i++)
                {
                    float nearestDistance = float.MaxValue;
                    for (int selectedIndex = 0; selectedIndex < result.Count; selectedIndex++)
                    {
                        float distance = (remaining[i].position - result[selectedIndex].position).sqrMagnitude;
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                        }
                    }

                    if (nearestDistance > bestDistance)
                    {
                        bestDistance = nearestDistance;
                        bestIndex = i;
                    }
                }

                result.Add(remaining[bestIndex]);
                remaining.RemoveAt(bestIndex);
            }

            return result;
        }

        private static int GetEmptyRoomReserve(int eligibleRoomCount)
        {
            return eligibleRoomCount >= 4 ? Mathf.Max(1, Mathf.RoundToInt(eligibleRoomCount * 0.25f)) : 0;
        }

        private static int GetSoloLimit(int floorIndex, int targetPopulatedRooms)
        {
            if (floorIndex <= 1)
            {
                return 1;
            }

            return Mathf.Max(1, Mathf.FloorToInt(Mathf.Max(1, targetPopulatedRooms) * 0.25f));
        }

        private static void AppendWarning(DungeonEncounterPlan plan, string message)
        {
            if (plan == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            plan.warning = string.IsNullOrWhiteSpace(plan.warning)
                ? message
                : $"{plan.warning} {message}";
        }

        private static List<EnemyDefinition> ChooseArchetypesForGroup(
            List<EnemyDefinition> definitions,
            int floorIndex,
            int count,
            System.Random random)
        {
            List<EnemyDefinition> selected = new List<EnemyDefinition>();
            if (definitions == null || definitions.Count == 0 || count <= 0)
            {
                return selected;
            }

            for (int i = 0; i < count; i++)
            {
                EnemyDefinition next = ChooseWeightedDefinition(definitions, floorIndex, random, selected);
                selected.Add(next);
            }

            return selected;
        }

        private static EnemyDefinition ChooseWeightedDefinition(
            List<EnemyDefinition> definitions,
            int floorIndex,
            System.Random random,
            List<EnemyDefinition> currentGroup)
        {
            float totalWeight = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                totalWeight += GetSpawnWeight(definitions[i], floorIndex, currentGroup);
            }

            if (totalWeight <= 0f)
            {
                return definitions[0];
            }

            double roll = random.NextDouble() * totalWeight;
            float cursor = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                cursor += GetSpawnWeight(definitions[i], floorIndex, currentGroup);
                if (roll <= cursor)
                {
                    return definitions[i];
                }
            }

            return definitions[definitions.Count - 1];
        }

        private static float GetSpawnWeight(EnemyDefinition definition, int floorIndex, List<EnemyDefinition> currentGroup = null)
        {
            if (definition == null || !definition.IsEligibleForFloor(floorIndex))
            {
                return 0f;
            }

            float weight = definition.archetype switch
            {
                EnemyArchetype.Slime => floorIndex <= 1 ? 55f : (floorIndex == 2 ? 35f : (floorIndex == 3 ? 20f : 10f)),
                EnemyArchetype.Bat => floorIndex <= 1 ? 25f : (floorIndex == 2 ? 30f : (floorIndex == 3 ? 30f : 25f)),
                EnemyArchetype.GoblinBrute => floorIndex < 3 ? 0f : (floorIndex == 3 ? 10f : 20f),
                _ => floorIndex <= 1 ? 20f : (floorIndex == 2 ? 35f : (floorIndex == 3 ? 40f : 45f))
            };

            if (currentGroup != null && currentGroup.Count > 0)
            {
                for (int i = 0; i < currentGroup.Count; i++)
                {
                    if (currentGroup[i] != null && currentGroup[i].archetype == definition.archetype)
                    {
                        weight *= 0.35f;
                    }
                }
            }

            return Mathf.Max(0f, weight);
        }

        private static EncounterBudget GetBudget(int floorIndex, System.Random random)
        {
            int min;
            int max;
            if (floorIndex <= 1)
            {
                min = 6;
                max = 10;
            }
            else if (floorIndex == 2)
            {
                min = 8;
                max = 12;
            }
            else if (floorIndex == 3)
            {
                min = 10;
                max = 14;
            }
            else
            {
                min = 12;
                max = 18;
            }

            return new EncounterBudget
            {
                min = min,
                max = max,
                requested = random.Next(min, max + 1)
            };
        }

        private static int GetRoomPriority(DungeonRoomBuildRecord room)
        {
            return room.roomType switch
            {
                DungeonNodeKind.Ordinary => 0,
                DungeonNodeKind.Landmark => 1,
                DungeonNodeKind.Secret => 2,
                _ => 9
            };
        }

        private static DungeonEncounterRoomRole GetRole(DungeonRoomBuildRecord room, int count)
        {
            if (room.roomType == DungeonNodeKind.Secret)
            {
                return DungeonEncounterRoomRole.OptionalDanger;
            }

            if (room.roomType == DungeonNodeKind.Landmark)
            {
                return DungeonEncounterRoomRole.LandmarkFight;
            }

            if (count >= 3)
            {
                return DungeonEncounterRoomRole.HeavyCombat;
            }

            return count <= 1 ? DungeonEncounterRoomRole.LightCombat : DungeonEncounterRoomRole.StandardCombat;
        }

        private void RegisterEnemy(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null || livingEnemies.Contains(enemyHealth))
            {
                return;
            }

            livingEnemies.Add(enemyHealth);
            enemyHealth.Died += HandleEnemyDied;
        }

        private void HandleEnemyDied(EnemyHealth enemyHealth)
        {
            if (enemyHealth != null)
            {
                enemyHealth.Died -= HandleEnemyDied;
                livingEnemies.Remove(enemyHealth);
                dropService.UnregisterEnemy(enemyHealth);
            }

            summary.livingEnemyCount = livingEnemies.Count;
        }

        private static Transform GetOrCreateEnemyRoot(Transform runtimeRoot)
        {
            Transform existing = runtimeRoot.Find(EncounterEnemiesRootName);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(EncounterEnemiesRootName);
            root.transform.SetParent(runtimeRoot, false);
            return root.transform;
        }

        internal static GameObject CreateEnemy(Transform parent, Vector3 position, EnemyDefinition definition)
        {
            EnemyDefinition safeDefinition = definition != null
                ? definition
                : EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt);

            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = safeDefinition.displayName;
            enemy.transform.SetParent(parent, true);
            enemy.transform.position = position;
            enemy.transform.localScale = safeDefinition.visualScale;

            CapsuleCollider capsuleCollider = enemy.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(capsuleCollider);
                }
                else
                {
                    Object.DestroyImmediate(capsuleCollider);
                }
            }

            CharacterController characterController = enemy.AddComponent<CharacterController>();
            float visualHeight = Mathf.Max(1.1f, safeDefinition.visualScale.y * 2f);
            float visualRadius = Mathf.Clamp(Mathf.Max(safeDefinition.visualScale.x, safeDefinition.visualScale.z) * 0.42f, 0.32f, 0.82f);
            characterController.height = visualHeight;
            characterController.radius = visualRadius;
            characterController.center = Vector3.zero;

            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            health.Configure(safeDefinition);
            SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();
            melee.Configure(safeDefinition);

            int defaultLayer = LayerMask.NameToLayer("Default");
            DungeonSceneController.SetLayerRecursively(enemy, defaultLayer >= 0 ? defaultLayer : 0);
            return enemy;
        }

        private sealed class DungeonEncounterRoomCandidate
        {
            public DungeonRoomBuildRecord room;
            public List<DungeonSpawnPointRecord> safeSpawns;
            public int capacity;
            public float distanceFromPlayer;
        }

        private sealed class EncounterTemplate
        {
            public readonly string id;
            public readonly int minFloor;
            public readonly int maxFloor;
            public readonly int weight;
            public readonly EnemyArchetype[] archetypes;

            public EncounterTemplate(string id, int minFloor, int maxFloor, int weight, params EnemyArchetype[] archetypes)
            {
                this.id = id;
                this.minFloor = minFloor;
                this.maxFloor = maxFloor;
                this.weight = weight;
                this.archetypes = archetypes ?? System.Array.Empty<EnemyArchetype>();
            }

            public int Count => archetypes.Length;

            public bool IsEligibleForFloor(int floorIndex)
            {
                int clampedFloor = Mathf.Max(1, floorIndex);
                return clampedFloor >= Mathf.Max(1, minFloor) &&
                       (maxFloor <= 0 || clampedFloor <= maxFloor);
            }

            public bool Contains(EnemyArchetype archetype)
            {
                for (int i = 0; i < archetypes.Length; i++)
                {
                    if (archetypes[i] == archetype)
                    {
                        return true;
                    }
                }

                return false;
            }

            public int CountArchetype(EnemyArchetype archetype)
            {
                int count = 0;
                for (int i = 0; i < archetypes.Length; i++)
                {
                    if (archetypes[i] == archetype)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private struct EncounterBudget
        {
            public int min;
            public int max;
            public int requested;
        }
    }

    public enum DungeonEncounterRoomRole
    {
        Empty,
        SafeStart,
        LightCombat,
        StandardCombat,
        HeavyCombat,
        LandmarkFight,
        OptionalDanger,
        FutureStairGuardian
    }

    public sealed class DungeonEncounterPlan
    {
        public int floorIndex;
        public int requestedBudget;
        public int spawnedCount;
        public int eligibleRoomCount;
        public int emptyEligibleRoomCount;
        public int soloRoomCount;
        public int groupFightCount;
        public string spawnSource = string.Empty;
        public string warning = string.Empty;
        public readonly List<DungeonEncounterRoomAssignment> assignments = new List<DungeonEncounterRoomAssignment>();
        public readonly List<DungeonEncounterSpawn> spawns = new List<DungeonEncounterSpawn>();
        public readonly Dictionary<EnemyArchetype, int> archetypeCounts = new Dictionary<EnemyArchetype, int>();
        public readonly Dictionary<string, int> templateCounts = new Dictionary<string, int>();

        public void AddArchetype(EnemyArchetype archetype)
        {
            archetypeCounts.TryGetValue(archetype, out int count);
            archetypeCounts[archetype] = count + 1;
        }

        public void AddTemplate(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return;
            }

            templateCounts.TryGetValue(templateId, out int count);
            templateCounts[templateId] = count + 1;
        }

        public DungeonEncounterSummary ToSummary()
        {
            return new DungeonEncounterSummary
            {
                floorIndex = floorIndex,
                requestedBudget = requestedBudget,
                spawnedEnemyCount = spawnedCount,
                livingEnemyCount = spawnedCount,
                eligibleRoomCount = eligibleRoomCount,
                emptyEligibleRoomCount = emptyEligibleRoomCount,
                soloRoomCount = soloRoomCount,
                groupFightCount = groupFightCount,
                spawnSource = spawnSource,
                warning = warning,
                roomAssignments = new List<DungeonEncounterRoomAssignment>(assignments),
                archetypeCounts = new Dictionary<EnemyArchetype, int>(archetypeCounts),
                templateCounts = new Dictionary<string, int>(templateCounts)
            };
        }
    }

    public sealed class DungeonEncounterSpawn
    {
        public string roomId;
        public string templateId;
        public DungeonEncounterRoomRole role;
        public Vector3 position;
        public EnemyDefinition definition;
    }

    public sealed class DungeonEncounterRoomAssignment
    {
        public string roomId;
        public DungeonNodeKind roomType;
        public DungeonEncounterRoomRole role;
        public int plannedCount;
        public int spawnedCount;
        public int roomCapacity;
        public string templateId = string.Empty;
        public readonly List<EnemyArchetype> archetypes = new List<EnemyArchetype>();
    }

    public sealed class DungeonEncounterSummary
    {
        public static readonly DungeonEncounterSummary Empty = new DungeonEncounterSummary
        {
            spawnSource = "None"
        };

        public int floorIndex;
        public int requestedBudget;
        public int spawnedEnemyCount;
        public int livingEnemyCount;
        public int eligibleRoomCount;
        public int emptyEligibleRoomCount;
        public int soloRoomCount;
        public int groupFightCount;
        public string spawnSource = string.Empty;
        public string warning = string.Empty;
        public List<DungeonEncounterRoomAssignment> roomAssignments = new List<DungeonEncounterRoomAssignment>();
        public Dictionary<EnemyArchetype, int> archetypeCounts = new Dictionary<EnemyArchetype, int>();
        public Dictionary<string, int> templateCounts = new Dictionary<string, int>();

        public string ToDebugString()
        {
            return
                $"Encounter Director Lite | Floor {floorIndex} | Source {spawnSource} | " +
                $"Spawned {spawnedEnemyCount}/{requestedBudget} | Living {livingEnemyCount} | " +
                $"Archetypes {FormatArchetypes()} | Rooms {roomAssignments.Count}/{eligibleRoomCount} | " +
                $"Empty {emptyEligibleRoomCount} | Groups {groupFightCount} | Solos {soloRoomCount} | Templates {FormatTemplates()}" +
                (string.IsNullOrWhiteSpace(warning) ? string.Empty : $" | Warning: {warning}");
        }

        private string FormatArchetypes()
        {
            if (archetypeCounts == null || archetypeCounts.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<EnemyArchetype, int> pair in archetypeCounts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            return string.Join(",", parts);
        }

        private string FormatTemplates()
        {
            if (templateCounts == null || templateCounts.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in templateCounts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            return string.Join(",", parts);
        }
    }
}
