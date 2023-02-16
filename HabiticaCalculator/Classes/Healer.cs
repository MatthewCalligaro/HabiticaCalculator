namespace HabiticaCalculator.Classes
{
    public class Healer : Player
    {
        public override int AttackCost => 15;

        public override int BuffCost => 30;

        public override ConsoleColor Color => ConsoleColor.Yellow;

        public override Stats Buff => Stats.Zero;

        public override double AttackDamage(Stats buffs)
        {
            return 0;
        }

        public Healer(string[] info) : base(info)
        {
            // Intentionally left blank
        }
    }
}
