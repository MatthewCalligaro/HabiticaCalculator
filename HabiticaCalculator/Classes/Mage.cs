namespace HabiticaCalculator.Classes
{
    public class Mage : Player
    {
        public override int AttackCost => 10;

        public override int BuffCost => 35;

        public override ConsoleColor Color => ConsoleColor.Cyan;

        public override Stats Buff => new(UnbuffedStats.Int * 30 / (UnbuffedStats.Int + 200), 0);

        public override double AttackDamage(Stats buffs)
        {
            return Math.Ceiling((StartingStats.Int + buffs.Int) / 10);
        }

        public Mage(string[] info) : base(info)
        {
            // Intentionally left blank
        }
    }
}
