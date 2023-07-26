using ChebsValheimLibrary.Minions.AI;

namespace ChebsMercenaries.Minions.WorkerAI
{
    public class HumanMinerAI : MinerAI
    {
        public override float UpdateDelay => HumanMinerMinion.UpdateDelay.Value;
        public override float LookRadius => HumanMinerMinion.LookRadius.Value;
        public override float RoamRange => HumanMinion.RoamRange.Value;
        public override string RockInternalIDsList => HumanMinerMinion.RockInternalIDsList.Value;
        public override float ToolDamage => HumanMinerMinion.ToolDamage.Value;
        public override short ToolTier => HumanMinerMinion.ToolTier.Value;
        public override float ChatInterval => HumanMinerMinion.ChatInterval.Value;
        public override float ChatDistance => HumanMinerMinion.ChatDistance.Value;
    }
}