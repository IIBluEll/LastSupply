using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 날짜 정산과 치료의 다음 값만 계산합니다. 상태 반영과 난수 소비는 하지 않습니다.
    public static class SurvivalRules
    {
        public static int CalculateHealedHp(int currentHp, int maxHp, int healingAmount)
        {
            if ( maxHp < 1 || currentHp < 1 || currentHp > maxHp )
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            if ( healingAmount < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(healingAmount));
            }

            return (int)Math.Min((long)maxHp, (long)currentHp + healingAmount);
        }

        public static DaySummary CalculateDaySummary(long id, GameDefinition definition, int completedDay,
            ResourceAmount resources, IReadOnlyList<SurvivorState> survivors,
            ExpeditionState expedition, ResourceAmount lostLoot)
        {
            if ( definition == null )
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if ( id < 1 || completedDay < 1 || completedDay > definition.Rules.MaxDays )
            {
                throw new ArgumentOutOfRangeException(nameof(completedDay));
            }

            if ( survivors == null || survivors.Count != definition.Survivors.Count )
            {
                throw new ArgumentException("생존자 상태와 정의의 구성이 일치해야 합니다.", nameof(survivors));
            }

            if ( expedition == null )
            {
                throw new ArgumentNullException(nameof(expedition));
            }

            var before = new SurvivorState[survivors.Count];
            int expeditionIndex = -1;
            for ( int i = 0; i < survivors.Count; i++ )
            {
                SurvivorState survivor = survivors[i];
                SurvivorDefinition survivorDefinition = definition.Survivors[i];
                if ( survivor == null || survivor.SurvivorId != survivorDefinition.ID || survivor.CurrentHp > survivorDefinition.MaxHP )
                {
                    throw new ArgumentException("생존자 상태의 ID, 순서 또는 HP가 올바르지 않습니다.", nameof(survivors));
                }

                before[i] = survivor;
                if ( survivor.SurvivorId == expedition.SurvivorId )
                {
                    expeditionIndex = i;
                }
            }

            if ( expeditionIndex < 0 )
            {
                throw new ArgumentException("파견 생존자가 상태 목록에 없습니다.", nameof(expedition));
            }

            bool returnedAlive = before[expeditionIndex].IsAlive;
            if ( (returnedAlive && !lostLoot.IsEmpty) || (!returnedAlive && !expedition.Loot.IsEmpty) )
            {
                throw new ArgumentException("귀환·사망과 전리품 기록이 일치하지 않습니다.", nameof(lostLoot));
            }

            ResourceAmount recoveredLoot = returnedAlive ? expedition.Loot : default;
            ResourceAmount afterResources = resources.Add(recoveredLoot);
            var after = (SurvivorState[])before.Clone();
            GameRulesDefinition rules = definition.Rules;
            RUN_END_REASON endReason = RUN_END_REASON.NONE;
            int foodConsumed = 0;
            bool foodShortage = false;
            int starvationDamage = 0;
            int? nextDay = null;

            if ( !HasLivingSurvivor(after) )
            {
                endReason = RUN_END_REASON.ALL_DEAD;
            }
            else if ( afterResources.Parts >= rules.TargetParts )
            {
                endReason = RUN_END_REASON.RESCUED;
            }
            else if ( completedDay == rules.MaxDays )
            {
                endReason = RUN_END_REASON.TIME_EXPIRED;
            }
            else
            {
                foodConsumed = Math.Min(afterResources.Food, rules.DailyFoodConsumption);
                foodShortage = afterResources.Food < rules.DailyFoodConsumption;
                afterResources = new ResourceAmount(afterResources.Food - foodConsumed, afterResources.Medicine, afterResources.Parts);

                if ( foodShortage )
                {
                    starvationDamage = rules.StarvationDamage;
                    for ( int i = 0; i < after.Length; i++ )
                    {
                        if ( after[i].IsAlive )
                        {
                            int hp = Math.Max(0, after[i].CurrentHp - starvationDamage);
                            after[i] = new SurvivorState(definition.Survivors[i], hp);
                        }
                    }
                }

                if ( HasLivingSurvivor(after) )
                {
                    nextDay = completedDay + 1;
                }
                else
                {
                    endReason = RUN_END_REASON.ALL_DEAD;
                }
            }

            return new DaySummary(id, completedDay, nextDay, endReason, expedition, recoveredLoot, lostLoot,
                resources, afterResources, before, after, foodConsumed, foodShortage, starvationDamage);
        }

        private static bool HasLivingSurvivor(IReadOnlyList<SurvivorState> survivors)
        {
            for ( int i = 0; i < survivors.Count; i++ )
            {
                if ( survivors[i].IsAlive )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
