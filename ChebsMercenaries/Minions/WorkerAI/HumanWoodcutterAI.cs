using ChebsValheimLibrary.Minions.AI;

namespace ChebsMercenaries.Minions.WorkerAI
{
    public class HumanWoodcutterAI : WoodcutterAI
    {
        public override float UpdateDelay => HumanWoodcutterMinion.UpdateDelay.Value;
        public override float LookRadius => HumanWoodcutterMinion.LookRadius.Value;
        public override float RoamRange => MercenaryMinion.RoamRange.Value;
        public override float ToolDamage => HumanWoodcutterMinion.ToolDamage.Value;
        public override short ToolTier => HumanWoodcutterMinion.ToolTier.Value;
        public override float ChatInterval => HumanWoodcutterMinion.ChatInterval.Value;
        public override float ChatDistance => HumanWoodcutterMinion.ChatDistance.Value;
    }
}