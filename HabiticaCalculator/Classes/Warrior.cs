namespace HabiticaCalculator.Classes
{
    public class Warrior : Player
    {
        public override int AttackCost => 10;

        public override int BuffCost => 20;

        public override ConsoleColor Color => ConsoleColor.Red;

        public override Stats Buff => new(0, UnbuffedStats.Str * 20 / (UnbuffedStats.Str + 200));

        public override double AttackDamage(Stats buffs)
        {
            double str = StartingStats.Str + buffs.Str;
            return 55 * str / (str + 70);
        }

        public Warrior(string[] info) : base(info)
        {
            // Intentionally left blank
        }
    }
}
