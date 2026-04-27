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
        private const float MinimumEnemySpawnSeparation = 2.35f;

        private readonly Transform runtimeRoot;
        private readonly EncounterDropService dropService;
        private readonly List<EnemyHealth> livingEnemies = new List<EnemyHealth>();
        private DungeonEncounterSummary summary = DungeonEncounterSummary.Empty;

        public DungeonEncounterDirector(Transform runtimeRoot)
        {
            this.runtimeRoot = runtimeRoot;
            dropService = new EncounterDropService(runtimeRoot);
        }

        public DungeonEncounterSummary Summary
        {
            get
            {
                RefreshSummaryStateCounts();
                return summary;
            }
        }
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
                DungeonRoomBuildRecord room = buildResult.FindRoom(spawn.roomId);
                GameObject enemy = CreateEnemy(
                    enemyRoot,
                    spawn.position,
                    spawn.definition,
                    room,
                    BuildRoomPatrolPoints(buildResult, room),
                    spawn.mobilityRole,
                    spawn.roamingRoute,
                    spawn.behaviorSeed);
                EnemyHealth enemyHealth = enemy != null ? enemy.GetComponent<EnemyHealth>() : null;
                if (enemyHealth == null)
                {
                    continue;
                }

                RegisterEnemy(enemyHealth);
                dropService.RegisterEnemy(enemyHealth);
            }

            summary.livingEnemyCount = livingEnemies.Count;
            RefreshSummaryStateCounts();
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
            summary.enemyStateCounts.Clear();
        }

        public static DungeonEncounterPlan BuildPlan(DungeonBuildResult buildResult, IList<Vector3> occupiedPositions, int seed)
        {
            DungeonEncounterPlan plan = new DungeonEncounterPlan
            {
                floorIndex = buildResult != null ? buildResult.floorIndex : 0,
                spawnSource = SpawnSourceEncounterDirector,
                difficultyBand = GetDifficultyBand(buildResult != null ? buildResult.floorIndex : 0),
                activeCombatCap = GetActiveCombatCap(buildResult != null ? buildResult.floorIndex : 0)
            };

            if (buildResult == null)
            {
                plan.warning = "Encounter Director skipped: dungeon build missing.";
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
            int assignedRoamers = 0;
            int roamerLimit = GetRoamerLimit(buildResult.floorIndex);
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

                if (spreadSpawns.Count < spawnCount)
                {
                    AppendWarning(plan, $"Encounter Director underfilled {assignment.roomId}: spacing allowed {spreadSpawns.Count}/{spawnCount} safe melee spawns.");
                    spawnCount = spreadSpawns.Count;
                }

                for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
                {
                    EnemyDefinition definition = FindDefinition(definitions, assignment.archetypes[spawnIndex]);
                    if (definition == null)
                    {
                        continue;
                    }

                    DungeonSpawnPointRecord spawnPoint = spreadSpawns[spawnIndex];
                    EnemyMobilityRole mobilityRole = ChooseMobilityRole(definition, candidate.room, buildResult.floorIndex, assignedRoamers < roamerLimit, random);
                    List<Vector3> roamingRoute = BuildRoamingRoute(buildResult, candidate.room, spawnPoint.position, mobilityRole, random);
                    if ((mobilityRole == EnemyMobilityRole.Roamer || mobilityRole == EnemyMobilityRole.Hunter) && roamingRoute.Count < 2)
                    {
                        mobilityRole = EnemyMobilityRole.RoomGuard;
                    }

                    EnemyVariantDefinition variant = EnemyVariantCatalog.ChooseVariant(definition.archetype, buildResult.floorIndex, random);
                    EnemyDefinition spawnDefinition = variant != null
                        ? EnemyVariantCatalog.CreateVariantDefinition(definition, variant)
                        : definition;

                    if (mobilityRole == EnemyMobilityRole.Roamer || mobilityRole == EnemyMobilityRole.Hunter)
                    {
                        assignedRoamers++;
                        plan.roamerCount++;
                    }

                    plan.AddMobilityRole(mobilityRole);
                    if (variant != null)
                    {
                        plan.AddVariant(variant.variantId);
                    }

                    assignment.spawnedCount++;
                    plan.spawns.Add(new DungeonEncounterSpawn
                    {
                        roomId = assignment.roomId,
                        templateId = assignment.templateId,
                        role = assignment.role,
                        position = spawnPoint.position,
                        definition = spawnDefinition,
                        mobilityRole = mobilityRole,
                        variantId = variant != null ? variant.variantId : string.Empty,
                        roamingRoute = roamingRoute,
                        behaviorSeed = BuildEnemyBehaviorSeed(seed, buildResult.floorIndex, assignment.roomId, spawnIndex, spawnPoint.position, spawnDefinition.archetype)
                    });
                    plan.AddArchetype(spawnDefinition.archetype);
                }
            }

            plan.spawnedCount = plan.spawns.Count;
            if (plan.spawnedCount < budget.min)
            {
                AppendWarning(plan, $"Encounter Director underfilled floor {buildResult.floorIndex}: spawned {plan.spawnedCount}/{budget.requested}.");
            }

            return plan;
        }

        public static int GetRoomCapacity(DungeonRoomBuildRecord room, int safeSpawnCount, int floorIndex = 1)
        {
            if (room == null || safeSpawnCount <= 0)
            {
                return 0;
            }

            int footprintCapacity = room.footprintArea >= 1600f ? 5 : (room.footprintArea >= 900f ? 3 : 2);
            if (floorIndex <= 1)
            {
                footprintCapacity = Mathf.Min(footprintCapacity, room.footprintArea >= 1600f ? 3 : (room.footprintArea >= 900f ? 2 : 1));
            }
            else if (floorIndex <= 2)
            {
                footprintCapacity = Mathf.Min(footprintCapacity, room.footprintArea >= 1600f ? 4 : (room.footprintArea >= 900f ? 3 : 2));
            }

            if (room.roomType == DungeonNodeKind.Landmark)
            {
                footprintCapacity = Mathf.Max(floorIndex >= 6 ? 6 : 3, footprintCapacity + (floorIndex >= 6 ? 3 : 2));
            }

            if (room.roomType == DungeonNodeKind.Secret)
            {
                footprintCapacity = Mathf.Min(floorIndex >= 3 ? 2 : 1, footprintCapacity);
            }

            int maxCapacity = room.roomType == DungeonNodeKind.Landmark
                ? (floorIndex >= 6 ? 10 : 6)
                : (floorIndex >= 6 ? 5 : 4);
            return Mathf.Clamp(Mathf.Min(footprintCapacity, safeSpawnCount), 0, maxCapacity);
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

                int capacity = GetRoomCapacity(room, safeSpawns.Count, buildResult.floorIndex);
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
                Mathf.Min(landmark.capacity, plan.floorIndex >= 6 ? 10 : 6),
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
                new EncounterTemplate("SoloSlime", 1, 0, 1, EnemyArchetype.Slime),
                new EncounterTemplate("SoloBat", 1, 0, 1, EnemyArchetype.Bat),
                new EncounterTemplate("SoloGoblinGrunt", 1, 0, 1, EnemyArchetype.GoblinGrunt),
                new EncounterTemplate("Easy_2Slimes", 1, 0, 7, EnemyArchetype.Slime, EnemyArchetype.Slime),
                new EncounterTemplate("Easy_SlimeBat", 1, 0, 7, EnemyArchetype.Slime, EnemyArchetype.Bat),
                new EncounterTemplate("Medium_GoblinSlime", 1, 0, 6, EnemyArchetype.GoblinGrunt, EnemyArchetype.Slime),
                new EncounterTemplate("Medium_2BatsSlime", 1, 0, 4, EnemyArchetype.Bat, EnemyArchetype.Bat, EnemyArchetype.Slime),
                new EncounterTemplate("Medium_2SlimesBat", 1, 0, 5, EnemyArchetype.Slime, EnemyArchetype.Slime, EnemyArchetype.Bat),
                new EncounterTemplate("Hard_2GoblinGrunts", 2, 0, 4, EnemyArchetype.GoblinGrunt, EnemyArchetype.GoblinGrunt),
                new EncounterTemplate("Hard_Goblin2Slimes", 2, 0, 5, EnemyArchetype.GoblinGrunt, EnemyArchetype.Slime, EnemyArchetype.Slime),
                new EncounterTemplate("Depth_BruteBat", 3, 0, 4, EnemyArchetype.GoblinBrute, EnemyArchetype.Bat),
                new EncounterTemplate("Depth_Brute2Slimes", 3, 0, 5, EnemyArchetype.GoblinBrute, EnemyArchetype.Slime, EnemyArchetype.Slime),
                new EncounterTemplate("Deep_GoblinBatSlime", 4, 0, 6, EnemyArchetype.GoblinGrunt, EnemyArchetype.Bat, EnemyArchetype.Slime),
                new EncounterTemplate("Deep_BruteEscort", 5, 0, 5, EnemyArchetype.GoblinBrute, EnemyArchetype.GoblinGrunt, EnemyArchetype.Bat),
                new EncounterTemplate("Deep_LargeSkirmish", 6, 0, 4, EnemyArchetype.GoblinGrunt, EnemyArchetype.GoblinGrunt, EnemyArchetype.Bat, EnemyArchetype.Slime),
                new EncounterTemplate("Deep_LandmarkPressure", 6, 0, 3, EnemyArchetype.GoblinBrute, EnemyArchetype.GoblinGrunt, EnemyArchetype.Bat, EnemyArchetype.Slime)
            };
        }

        private static EnemyDefinition FindDefinition(List<EnemyDefinition> definitions, EnemyArchetype archetype)
        {
            return definitions != null ? definitions.Find(definition => definition != null && definition.archetype == archetype) : null;
        }

        private static int BuildEnemyBehaviorSeed(int floorSeed, int floorIndex, string roomId, int spawnIndex, Vector3 spawnPosition, EnemyArchetype archetype)
        {
            unchecked
            {
                int hash = floorSeed != 0 ? floorSeed : 17;
                hash = hash * 397 ^ floorIndex;
                hash = hash * 397 ^ spawnIndex;
                hash = hash * 397 ^ (int)archetype;
                hash = hash * 397 ^ Mathf.RoundToInt(spawnPosition.x * 10f);
                hash = hash * 397 ^ Mathf.RoundToInt(spawnPosition.z * 10f);
                if (!string.IsNullOrEmpty(roomId))
                {
                    for (int i = 0; i < roomId.Length; i++)
                    {
                        hash = hash * 31 + roomId[i];
                    }
                }

                return hash == 0 ? 19 : hash;
            }
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

                if (bestDistance < MinimumEnemySpawnSeparation * MinimumEnemySpawnSeparation)
                {
                    break;
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
                EnemyArchetype.Slime => floorIndex <= 1 ? 55f : (floorIndex == 2 ? 35f : (floorIndex <= 5 ? 18f : 10f)),
                EnemyArchetype.Bat => floorIndex <= 1 ? 25f : (floorIndex == 2 ? 30f : (floorIndex <= 5 ? 30f : 24f)),
                EnemyArchetype.GoblinBrute => floorIndex < 3 ? 0f : (floorIndex == 3 ? 10f : (floorIndex <= 5 ? 22f : 30f)),
                _ => floorIndex <= 1 ? 20f : (floorIndex == 2 ? 35f : (floorIndex <= 5 ? 45f : 52f))
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

        private static int GetRoamerLimit(int floorIndex)
        {
            if (floorIndex <= 1)
            {
                return 1;
            }

            if (floorIndex == 2)
            {
                return 2;
            }

            if (floorIndex <= 5)
            {
                return 4;
            }

            return 6;
        }

        private static int GetActiveCombatCap(int floorIndex)
        {
            if (floorIndex <= 2)
            {
                return 4;
            }

            if (floorIndex <= 5)
            {
                return 6;
            }

            if (floorIndex <= 8)
            {
                return 8;
            }

            return 10;
        }

        private static string GetDifficultyBand(int floorIndex)
        {
            if (floorIndex <= 2)
            {
                return "Recruit";
            }

            if (floorIndex <= 5)
            {
                return "RealDanger";
            }

            if (floorIndex <= 8)
            {
                return "DungeonPushesBack";
            }

            if (floorIndex <= 15)
            {
                return "HostileDepths";
            }

            return "OverrunDepths";
        }

        private static EnemyMobilityRole ChooseMobilityRole(
            EnemyDefinition definition,
            DungeonRoomBuildRecord room,
            int floorIndex,
            bool canUseRoamerSlot,
            System.Random random)
        {
            if (definition == null)
            {
                return EnemyMobilityRole.RoomGuard;
            }

            if (definition.archetype == EnemyArchetype.GoblinBrute)
            {
                return EnemyMobilityRole.Sleeper;
            }

            if (!canUseRoamerSlot || room == null || room.roomType == DungeonNodeKind.Secret)
            {
                return definition.defaultMobilityRole == EnemyMobilityRole.Sleeper
                    ? EnemyMobilityRole.Sleeper
                    : EnemyMobilityRole.RoomGuard;
            }

            double roll = random != null ? random.NextDouble() : 0d;
            return definition.archetype switch
            {
                EnemyArchetype.Bat => roll < (floorIndex <= 1 ? 0.35d : 0.65d) ? EnemyMobilityRole.Roamer : EnemyMobilityRole.RoomGuard,
                EnemyArchetype.GoblinGrunt => floorIndex >= 3 && roll < 0.18d
                    ? EnemyMobilityRole.Hunter
                    : (roll < (floorIndex >= 3 ? 0.48d : 0.25d) ? EnemyMobilityRole.Roamer : EnemyMobilityRole.RoomGuard),
                EnemyArchetype.Slime => floorIndex >= 2 && roll < 0.12d ? EnemyMobilityRole.Roamer : EnemyMobilityRole.RoomGuard,
                _ => EnemyMobilityRole.RoomGuard
            };
        }

        private static List<Vector3> BuildRoamingRoute(
            DungeonBuildResult buildResult,
            DungeonRoomBuildRecord homeRoom,
            Vector3 startPosition,
            EnemyMobilityRole mobilityRole,
            System.Random random)
        {
            List<Vector3> route = new List<Vector3>();
            if (buildResult == null ||
                homeRoom == null ||
                (mobilityRole != EnemyMobilityRole.Roamer && mobilityRole != EnemyMobilityRole.Hunter))
            {
                return route;
            }

            List<DungeonRoomBuildRecord> neighbors = FindRoamingNeighborRooms(buildResult, homeRoom);
            if (neighbors.Count == 0)
            {
                return route;
            }

            DungeonRoomBuildRecord destination = neighbors[Mathf.Clamp(random != null ? random.Next(0, neighbors.Count) : 0, 0, neighbors.Count - 1)];
            string edgeKey = DungeonBuildResult.GetEdgeKey(homeRoom.nodeId, destination.nodeId);
            route.Add(startPosition);
            AddDoorOpeningPoint(route, buildResult, homeRoom.nodeId, edgeKey);
            List<DungeonCorridorBuildRecord> corridors = buildResult.GetCorridorsForEdge(edgeKey);
            for (int i = 0; i < corridors.Count; i++)
            {
                DungeonCorridorBuildRecord corridor = corridors[i];
                route.Add(Vector3.Lerp(corridor.start, corridor.end, 0.5f));
            }

            AddDoorOpeningPoint(route, buildResult, destination.nodeId, edgeKey);
            List<DungeonSpawnPointRecord> destinationSpawns = buildResult.GetSpawnPoints(destination.nodeId, DungeonSpawnPointCategory.EnemyMelee);
            route.Add(destinationSpawns.Count > 0 ? destinationSpawns[0].position : destination.bounds.center);
            return route;
        }

        private static List<DungeonRoomBuildRecord> FindRoamingNeighborRooms(DungeonBuildResult buildResult, DungeonRoomBuildRecord homeRoom)
        {
            List<DungeonRoomBuildRecord> result = new List<DungeonRoomBuildRecord>();
            for (int i = 0; i < buildResult.graphEdges.Count; i++)
            {
                DungeonGraphEdgeRecord edge = buildResult.graphEdges[i];
                string neighborId = edge.a == homeRoom.nodeId ? edge.b : (edge.b == homeRoom.nodeId ? edge.a : string.Empty);
                if (string.IsNullOrWhiteSpace(neighborId))
                {
                    continue;
                }

                DungeonRoomBuildRecord neighbor = buildResult.FindRoom(neighborId);
                if (neighbor == null ||
                    IsSafeRoomType(neighbor.roomType) ||
                    neighbor.roomType == DungeonNodeKind.Secret ||
                    (neighbor.roomType == DungeonNodeKind.Landmark && homeRoom.roomType != DungeonNodeKind.Landmark))
                {
                    continue;
                }

                result.Add(neighbor);
            }

            return result;
        }

        private static void AddDoorOpeningPoint(List<Vector3> route, DungeonBuildResult buildResult, string nodeId, string edgeKey)
        {
            for (int i = 0; i < buildResult.doorOpenings.Count; i++)
            {
                DungeonDoorOpeningRecord opening = buildResult.doorOpenings[i];
                if (opening.nodeId == nodeId && opening.edgeKey == edgeKey)
                {
                    route.Add(opening.center);
                    return;
                }
            }
        }

        private static EncounterBudget GetBudget(int floorIndex, System.Random random)
        {
            int min;
            int max;
            if (floorIndex <= 1)
            {
                min = 8;
                max = 12;
            }
            else if (floorIndex == 2)
            {
                min = 10;
                max = 15;
            }
            else if (floorIndex == 3)
            {
                min = 14;
                max = 20;
            }
            else if (floorIndex <= 5)
            {
                min = 18;
                max = 26;
            }
            else if (floorIndex <= 8)
            {
                min = 24;
                max = 32;
            }
            else if (floorIndex <= 15)
            {
                min = 28;
                max = 40;
            }
            else
            {
                min = 36;
                max = 52;
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
            RefreshSummaryStateCounts();
        }

        private void RefreshSummaryStateCounts()
        {
            summary.enemyStateCounts.Clear();
            for (int i = 0; i < livingEnemies.Count; i++)
            {
                SimpleMeleeEnemyController melee = livingEnemies[i] != null
                    ? livingEnemies[i].GetComponent<SimpleMeleeEnemyController>()
                    : null;
                if (melee == null)
                {
                    continue;
                }

                summary.enemyStateCounts.TryGetValue(melee.State, out int count);
                summary.enemyStateCounts[melee.State] = count + 1;
            }
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
            return CreateEnemy(parent, position, definition, null, null);
        }

        internal static GameObject CreateEnemy(
            Transform parent,
            Vector3 position,
            EnemyDefinition definition,
            DungeonRoomBuildRecord homeRoom,
            IReadOnlyList<Vector3> patrolPoints)
        {
            return CreateEnemy(parent, position, definition, homeRoom, patrolPoints, definition != null ? definition.defaultMobilityRole : EnemyMobilityRole.RoomGuard, null, 0);
        }

        internal static GameObject CreateEnemy(
            Transform parent,
            Vector3 position,
            EnemyDefinition definition,
            DungeonRoomBuildRecord homeRoom,
            IReadOnlyList<Vector3> patrolPoints,
            EnemyMobilityRole mobilityRole,
            IReadOnlyList<Vector3> roamingRoute)
        {
            return CreateEnemy(parent, position, definition, homeRoom, patrolPoints, mobilityRole, roamingRoute, 0);
        }

        internal static GameObject CreateEnemy(
            Transform parent,
            Vector3 position,
            EnemyDefinition definition,
            DungeonRoomBuildRecord homeRoom,
            IReadOnlyList<Vector3> patrolPoints,
            EnemyMobilityRole mobilityRole,
            IReadOnlyList<Vector3> roamingRoute,
            int behaviorSeed)
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
            if (homeRoom != null)
            {
                melee.ConfigureHomeRoom(homeRoom.nodeId, homeRoom.bounds, patrolPoints);
            }
            else
            {
                melee.ConfigureHomeRoom(string.Empty, new Bounds(position, new Vector3(12f, 4f, 12f)), new[] { position });
            }

            melee.ConfigureMobilityRole(mobilityRole);
            melee.ConfigureRoamingRoute(roamingRoute);
            if (behaviorSeed != 0)
            {
                melee.ConfigureBehaviorSeed(behaviorSeed);
            }

            int defaultLayer = LayerMask.NameToLayer("Default");
            DungeonSceneController.SetLayerRecursively(enemy, defaultLayer >= 0 ? defaultLayer : 0);
            return enemy;
        }

        private static List<Vector3> BuildRoomPatrolPoints(DungeonBuildResult buildResult, DungeonRoomBuildRecord room)
        {
            List<Vector3> points = new List<Vector3>();
            if (buildResult == null || room == null)
            {
                return points;
            }

            points.Add(room.bounds.center);
            for (int i = 0; i < buildResult.spawnPoints.Count; i++)
            {
                DungeonSpawnPointRecord spawn = buildResult.spawnPoints[i];
                if (spawn.nodeId == room.nodeId && spawn.category == DungeonSpawnPointCategory.EnemyMelee)
                {
                    points.Add(spawn.position);
                }
            }

            return points;
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
        public int roamerCount;
        public int activeCombatCap;
        public string difficultyBand = string.Empty;
        public string spawnSource = string.Empty;
        public string warning = string.Empty;
        public readonly List<DungeonEncounterRoomAssignment> assignments = new List<DungeonEncounterRoomAssignment>();
        public readonly List<DungeonEncounterSpawn> spawns = new List<DungeonEncounterSpawn>();
        public readonly Dictionary<EnemyArchetype, int> archetypeCounts = new Dictionary<EnemyArchetype, int>();
        public readonly Dictionary<string, int> templateCounts = new Dictionary<string, int>();
        public readonly Dictionary<EnemyMobilityRole, int> mobilityRoleCounts = new Dictionary<EnemyMobilityRole, int>();
        public readonly Dictionary<string, int> variantCounts = new Dictionary<string, int>();

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

        public void AddMobilityRole(EnemyMobilityRole mobilityRole)
        {
            mobilityRoleCounts.TryGetValue(mobilityRole, out int count);
            mobilityRoleCounts[mobilityRole] = count + 1;
        }

        public void AddVariant(string variantId)
        {
            if (string.IsNullOrWhiteSpace(variantId))
            {
                return;
            }

            variantCounts.TryGetValue(variantId, out int count);
            variantCounts[variantId] = count + 1;
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
                roamerCount = roamerCount,
                activeCombatCap = activeCombatCap,
                difficultyBand = difficultyBand,
                spawnSource = spawnSource,
                warning = warning,
                roomAssignments = new List<DungeonEncounterRoomAssignment>(assignments),
                archetypeCounts = new Dictionary<EnemyArchetype, int>(archetypeCounts),
                templateCounts = new Dictionary<string, int>(templateCounts),
                mobilityRoleCounts = new Dictionary<EnemyMobilityRole, int>(mobilityRoleCounts),
                variantCounts = new Dictionary<string, int>(variantCounts),
                enemyStateCounts = new Dictionary<SimpleMeleeEnemyState, int>()
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
        public EnemyMobilityRole mobilityRole = EnemyMobilityRole.RoomGuard;
        public string variantId = string.Empty;
        public List<Vector3> roamingRoute = new List<Vector3>();
        public int behaviorSeed;
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
        public int roamerCount;
        public int activeCombatCap;
        public string difficultyBand = string.Empty;
        public string spawnSource = string.Empty;
        public string warning = string.Empty;
        public List<DungeonEncounterRoomAssignment> roomAssignments = new List<DungeonEncounterRoomAssignment>();
        public Dictionary<EnemyArchetype, int> archetypeCounts = new Dictionary<EnemyArchetype, int>();
        public Dictionary<string, int> templateCounts = new Dictionary<string, int>();
        public Dictionary<EnemyMobilityRole, int> mobilityRoleCounts = new Dictionary<EnemyMobilityRole, int>();
        public Dictionary<string, int> variantCounts = new Dictionary<string, int>();
        public Dictionary<SimpleMeleeEnemyState, int> enemyStateCounts = new Dictionary<SimpleMeleeEnemyState, int>();

        public string ToDebugString()
        {
            return
                $"Encounter Director Lite | Floor {floorIndex} | Source {spawnSource} | " +
                $"Band {difficultyBand} | Spawned {spawnedEnemyCount}/{requestedBudget} | Living {livingEnemyCount} | ActiveCap {activeCombatCap} | " +
                $"Archetypes {FormatArchetypes()} | Rooms {roomAssignments.Count}/{eligibleRoomCount} | " +
                $"Empty {emptyEligibleRoomCount} | Groups {groupFightCount} | Solos {soloRoomCount} | Roamers {roamerCount} | " +
                $"Templates {FormatTemplates()} | Roles {FormatRoles()} | Variants {FormatVariants()} | States {FormatStates()}" +
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

        private string FormatStates()
        {
            if (enemyStateCounts == null || enemyStateCounts.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<SimpleMeleeEnemyState, int> pair in enemyStateCounts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            return string.Join(",", parts);
        }

        private string FormatRoles()
        {
            if (mobilityRoleCounts == null || mobilityRoleCounts.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<EnemyMobilityRole, int> pair in mobilityRoleCounts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            return string.Join(",", parts);
        }

        private string FormatVariants()
        {
            if (variantCounts == null || variantCounts.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in variantCounts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            return string.Join(",", parts);
        }
    }
}
