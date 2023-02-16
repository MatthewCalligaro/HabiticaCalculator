using HabiticaCalculator.Classes;

namespace HabiticaCalculator
{
    /// <summary>
    /// Base player implementation shared between all classes.
    /// </summary>
    public abstract class Player
    {
        #region Abstract
        /// <summary>
        /// The mana cost of casting this class's attack spell.
        /// </summary>
        public abstract int AttackCost { get; }

        /// <summary>
        /// The mana cost of casting this class's team buff spell.
        /// </summary>
        public abstract int BuffCost { get; }

        /// <summary>
        /// The color used to identify this class.
        /// </summary>
        public abstract ConsoleColor Color { get; }

        /// <summary>
        /// The increase in team stats which occur when this player casts their team buff spell.
        /// </summary>
        public abstract Stats Buff { get; }

        /// <summary>
        /// The damage dealt when this player casts their attack spell.
        /// </summary>
        /// <param name="buffs">Any team buffs present when the attack spell is cast.</param>
        /// <returns>The damage dealt by the attack spell.</returns>
        public abstract double AttackDamage(Stats buffs);
        #endregion

        #region Static
        /// <summary>
        /// The lowest multiplier which can be applied to task damage based on task value.
        /// </summary>
        private static readonly double minTaskValueDelta = Math.Pow(0.9747, 21.27);

        /// <summary>
        /// Creates a player based on a line from an input file.
        /// </summary>
        /// <param name="playerInfo">One line from an input file, split by column.</param>
        /// <returns>A player with the information specified in the line</returns>
        /// <exception cref="ArgumentException">Thrown if the player info contains impossible or incorrect information.</exception>
        public static Player Create(string[] playerInfo)
        {
            const int expectedColumns = 11;
            if (playerInfo.Length != expectedColumns)
            {
                throw new ArgumentException($"playerInfo does not contain the correct number of columns (expected {expectedColumns}, but was {playerInfo.Length})");
            }

            return playerInfo[1].ToLower() switch
            {
                "warrior" => new Warrior(playerInfo),
                "mage" => new Mage(playerInfo),
                "healer" => new Healer(playerInfo),
                "rogue" => new Rogue(playerInfo),
                _ => throw new ArgumentException($"The provided class of '{playerInfo[1]}' is not a Habitica class. Please see https://habitica.fandom.com/wiki/Class_System for the possible classes."),
            };
        }
        #endregion

        #region Fields
        /// <summary>
        /// The level of the player.
        /// </summary>
        private readonly int level;

        /// <summary>
        /// The stats provided by the player's equipment.
        /// </summary>
        private readonly Stats equipment;

        /// <summary>
        /// The player's allocated stat points (from leveling).
        /// </summary>
        private readonly Stats allocation;

        /// <summary>
        /// True if the player has a perfect day bonus.
        /// </summary>
        private readonly bool dayBonus;

        /// <summary>
        /// The amount of mana the player has at the beginning of the day.
        /// </summary>
        private readonly int startingMana;

        /// <summary>
        /// The estimated number of habits the player will complete today.
        /// </summary>
        private readonly int projectedHabits;

        /// <summary>
        /// The estimated number of dailies and to dos the player will complete today.
        /// </summary>
        private readonly int projectedDailiesAndToDos;
        #endregion

        #region Properties
        /// <summary>
        /// The maximum number of team buff spells the player can cast (before completing any tasks).
        /// </summary>
        public int MaxBuffCount
        {
            get
            {
                return this.startingMana / this.BuffCost;
            }
        }

        /// <summary>
        /// Stats before any buffs are applied.
        /// </summary>
        /// <remarks>These stats are used when calculating the team buff spell.</remarks>
        protected Stats UnbuffedStats
        {
            get
            {
                return this.level / 2 + this.equipment + this.allocation;
            }
        }

        /// <summary>
        /// Stats before any team buffs are cast.
        /// </summary>
        protected Stats StartingStats
        {
            get
            {
                return this.UnbuffedStats + (this.dayBonus ? (level + 1) / 2 : 0);
            }
        }

        /// <summary>
        /// The mana which will be regenerated on the next cron.
        /// </summary>
        private double ManaRegenFromCron
        {
            get
            {
                return (this.StartingStats.Int * 2 + 30) / 10;
            }
        }

        /// <summary>
        /// The maximum number of team buff spells the player can cast and start tomorrow with equal or more mana.
        /// </summary>
        private int SustainableBuffCount
        {
            get
            {
                int prevNumBuffs = 0;
                int numBuffs = (int)((this.ManaRegenFromTasks(Stats.Zero) + this.ManaRegenFromCron) / this.BuffCost);

                // The buff itself may increase mana regen and allow more to be sustainably cast
                while (numBuffs > prevNumBuffs)
                {
                    prevNumBuffs = numBuffs;
                    numBuffs = (int)((this.ManaRegenFromTasks(this.Buff * numBuffs) + this.ManaRegenFromCron) / this.BuffCost);
                }

                return numBuffs;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// The name used to identify this player.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Creates a player based on a line from an input file.
        /// </summary>
        /// <param name="playerInfo">One line from an input file, split by column.</param>
        /// <exception cref="ArgumentException">Thrown if the player info contains impossible or incorrect information.</exception>
        public Player(string[] playerInfo)
        {
            this.Name = playerInfo[0];
            this.level = int.Parse(playerInfo[2]);
            this.equipment = new Stats(int.Parse(playerInfo[3]), int.Parse(playerInfo[4]));
            this.allocation = new Stats(int.Parse(playerInfo[5]), int.Parse(playerInfo[6]));
            this.dayBonus = bool.Parse(playerInfo[7]);
            this.startingMana = int.Parse(playerInfo[8]);
            this.projectedDailiesAndToDos = int.Parse(playerInfo[9]);
            this.projectedHabits = int.Parse(playerInfo[10]);

            if (this.level < 1
                || this.startingMana < 0
                || this.equipment.Int < 0
                || this.equipment.Str < 0
                || this.allocation.Int < 0
                || this.allocation.Str < 0
                || this.projectedDailiesAndToDos < 0
                || this.projectedHabits < 0)
            {
                throw new ArgumentException("All numeric inputs cannot be negative.");
            }
            if (this.allocation.Int + this.allocation.Str > level)
            {
                throw new ArgumentException($"Total allocation cannot exceed level. Allocation={this.allocation}, Level={this.level}.");
            }
            if (this.startingMana > this.StartingStats.Int * 2 + 30)
            {
                throw new ArgumentException($"Starting mana exceeds limit. Mana={this.startingMana}, Int={this.StartingStats.Int}");
            }
        }

        /// <summary>
        /// The damage dealt from completing the projected tasks.
        /// </summary>
        /// <param name="buffs">Any team buffs present when the player completes their tasks.</param>
        /// <returns>The damage dealt by tasks.</returns>
        /// <remarks>To be conservative, we assume the minimum task value possible and no crits.</remarks>
        public double TaskDamage(Stats buffs)
        {
            double strMultiplier = 1 + 0.005 * (this.StartingStats.Str + buffs.Str);
            return (this.projectedDailiesAndToDos + this.projectedHabits / 2.0) * strMultiplier * minTaskValueDelta;
        }

        /// <summary>
        /// The maximum amount of damage which can be dealt from attack spells.
        /// </summary>
        /// <param name="buffs">Any team buffs present when the player casts their attack spells.</param>
        /// <param name="numBuffs">The number of buffs cast by this player.</param>
        /// <param name="verboseMessage">A message detailing how this the attack damage is dealt.</param>
        /// <returns>The maximum attack damage this player can deal.</returns>
        /// <exception cref="ArgumentException">Thrown if the player does not have enough mana to cast numBuffs.</exception>
        /// <remarks>To be conservative, we assume no crits.</remarks>
        public double MaxAttackDamage(Stats buffs, int numBuffs, out string verboseMessage)
        {
            if (numBuffs * this.BuffCost > this.startingMana)
            {
                throw new ArgumentException($"Not enough mana to cast {numBuffs} buffs.", nameof(numBuffs));
            }

            double manaRegen = this.ManaRegenFromTasks(buffs);
            double totalMana = this.startingMana + manaRegen;
            double attackDamage = this.AttackDamage(buffs);
            int numAttacks = (int)((totalMana - numBuffs * this.BuffCost) / this.AttackCost);

            verboseMessage = $"{numAttacks} attacks at {attackDamage:0.0} damage each; mana regenerated = {manaRegen:0.0}";
            return attackDamage * numAttacks;
        }

        /// <summary>
        /// Detailed information about the player and their capabilities.
        /// </summary>
        /// <returns>A verbose message containing detailed player information.</returns>
        public string VerboseMessage()
        {
            double manaRegenFromTasks = this.ManaRegenFromTasks(Stats.Zero);
            double totalMana = this.startingMana + manaRegenFromTasks;

            double sustainableMana = manaRegenFromTasks + this.ManaRegenFromCron;
            int sustainableAttacks = (int)(sustainableMana / this.AttackCost);
            double attackDamage = this.AttackDamage(Stats.Zero);
            double sustainableAttackDamage = sustainableAttacks * attackDamage;

            return $"{this.Name} Info (before team buffs):\n" +
                $"Starting Stats = {this.StartingStats}\n" +
                $"Crit Chance = {100 * this.CritChance(Stats.Zero):0.0}% for a {100 * this.CritBonus(Stats.Zero):0}% bonus\n" +
                $"Buff Effect = {this.Buff}, for a max of {this.MaxBuffCount * this.Buff} ({this.MaxBuffCount} casts)\n" +
                $"Total Mana = {totalMana:0.0} ({this.startingMana:0.0} at start, {manaRegenFromTasks:0.0} regenerated from tasks)\n" +
                $"Task Damage = {this.TaskDamage(Stats.Zero):0.0}\n" +
                $"Max Attack Damage = {this.MaxAttackDamage(Stats.Zero, 0, out string verboseAttackMessage):0.0} ({verboseAttackMessage})\n" +
                $"Sustainable Mana = {sustainableMana:0.0} ({manaRegenFromTasks:0.0} from tasks, {this.ManaRegenFromCron:0.0} from cron)\n" +
                $"Sustainable Attack Damage = {sustainableAttackDamage:0.0} ({sustainableAttacks} attacks at {attackDamage:0.0} damage each)\n" +
                $"Sustainable Buff = {this.Buff * this.SustainableBuffCount} ({this.SustainableBuffCount} casts)\n";
        }

        /// <summary>
        /// The chance of achieving a critical hit when completing a task.
        /// </summary>
        /// <param name="buffs">Any team buffs present when the player completes their tasks.</param>
        /// <returns>The crit chance as a value in [0, 1].</returns>
        private double CritChance(Stats buffs)
        {
            return 0.03 * (1 + (this.StartingStats + buffs).Str / 100);
        }

        /// <summary>
        /// The bonus rewards received if a critical hit is achieved when completing a task.
        /// </summary>
        /// <param name="buffs">Any team buffs present when the player completes their tasks.</param>
        /// <returns>The critical hit bonus as a factor of the non-crit task rewards.</returns>
        private double CritBonus(Stats buffs)
        {
            double str = (this.StartingStats + buffs).Str;
            return 0.5 + 4 * str / (str + 200);
        }

        /// <summary>
        /// The mana regenerated from completing the projected tasks.
        /// </summary>
        /// <param name="buffs">Any team buffs present when the player completes their tasks.</param>
        /// <returns>The mana regenerated.</returns>
        /// <remarks>To be conservative, we assume no crits.</remarks>
        private double ManaRegenFromTasks(Stats buffs)
        {
            double maxMana = (this.StartingStats + buffs).Int * 2 + 30;
            return maxMana * (this.projectedDailiesAndToDos * 0.01 + this.projectedHabits * 0.0025);
        }
        #endregion
    }
}
