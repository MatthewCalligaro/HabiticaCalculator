namespace HabiticaCalculator
{
    /// <summary>
    /// Represents a collection of Habitica player stats which can affect damage.
    /// </summary>
    /// <remarks>Perception and constitution are not included because they do not affect player/party damage in any way.</remarks>
    public readonly struct Stats
    {
        /// <summary>
        /// Intelligence.
        /// </summary>
        public readonly double Int;

        /// <summary>
        /// Strength.
        /// </summary>
        public readonly double Str;

        public Stats(double @int, double str)
        {
            this.Int = @int;
            this.Str = str;
        }

        /// <summary>
        /// Stats containing a zero value for each component.
        /// </summary>
        public static Stats Zero { get { return new Stats(0, 0); } }

        /// <summary>
        /// Adds two stats component-wise.
        /// </summary>
        public static Stats operator +(Stats left, Stats right)
        {
            return new Stats(left.Int + right.Int, left.Str + right.Str);
        }

        /// <summary>
        /// Adds a scalar to all components.
        /// </summary>
        public static Stats operator +(Stats left, double right)
        {
            return new Stats(left.Int + right, left.Str + right);
        }

        /// <summary>
        /// Adds a scalar to all components.
        /// </summary>
        public static Stats operator +(double left, Stats right)
        {
            return right + left;
        }

        /// <summary>
        /// Multiplies all components by a scalar.
        /// </summary>
        public static Stats operator *(Stats left, double right)
        {
            return new Stats(left.Int * right, left.Str * right);
        }

        /// <summary>
        /// Multiplies all components by a scalar.
        /// </summary>
        public static Stats operator *(double left, Stats right)
        {
            return right * left;
        }

        /// <summary>
        /// Returns all components in a string.
        /// </summary>
        /// <returns>A string representation of the stats.</returns>
        public override string ToString()
        {
            return $"[Int: {this.Int:0.0}, Str: {this.Str:0.0}]";
        }
    }
}
