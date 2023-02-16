namespace HabiticaCalculator.Classes
{
    public class Rogue : Player
    {
        public override int AttackCost => 10;

        public override int BuffCost => 25;

        public override ConsoleColor Color => ConsoleColor.Magenta;

        public override Stats Buff => Stats.Zero;

        public override double AttackDamage(Stats buffs)
        {
            return 0;
        }

        public Rogue(string[] info) : base(info)
        {
            // Intentionally left blank
        }
    }
}
