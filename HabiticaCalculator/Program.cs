using System.Text;

namespace HabiticaCalculator
{
    /// <summary>
    /// Manages the console application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the console application.
        /// </summary>
        public static void Main()
        {
            string partyFilePath = SelectInputFile();
            Player[] party = ReadPlayers(partyFilePath);

            Console.Write("Do you want verbose output? [Y/n]: ");
            bool verbose = Console.ReadLine().ToLower() == "y";

            if (verbose)
            {
                foreach (Player player in party)
                {
                    WriteWithColor(player.VerboseMessage(), player.Color);
                }
            }

            MaxPartyDamage(party, verbose);

            foreach (Player player in party)
            {
                MaxSingleDamage(player, party, verbose);
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Determines the input file from which to read party information.
        /// </summary>
        /// <returns>The relative path to the selected input file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if no potential input files (.csv) are found.</exception>
        private static string SelectInputFile()
        {
            const int maxLevels = 5;
            int curLevels = 0;
            string rootPath = ".";
            string inputFile;

            // Find the root directory of the repo by searching for the .git directory
            while (curLevels < maxLevels &&
                   !Directory.EnumerateDirectories(rootPath).Aggregate(false, (bool isRoot, string dir) => isRoot || dir.Contains(".git")))
            {
                rootPath = "..\\" + rootPath;
                curLevels++;
            }

            // Recursively find all .csv files in the repo
            List<string> possibleInputFiles = new();
            FindPossibleInputFiles(rootPath, possibleInputFiles);

            // Allow the user to choose an input file from this list if there are multiple options
            if (possibleInputFiles.Count == 0)
            {
                string exceptionText = "No party files found. Please save party info in a .csv file using the schema used in Party.csv in the repository.";
                WriteWithColor(exceptionText, ConsoleColor.Red);
                throw new FileNotFoundException(exceptionText);
            }
            else if (possibleInputFiles.Count == 1)
            {
                inputFile = possibleInputFiles.First();
            }
            else
            {
                Console.WriteLine("Found the following potential party files:");
                foreach (string filePath in possibleInputFiles)
                {
                    Console.WriteLine(filePath);
                }
                Console.WriteLine("Please enter the desired input file, or just press enter to use the first option...");

                string consoleInput = Console.ReadLine();
                if (possibleInputFiles.Contains(consoleInput))
                {
                    inputFile = consoleInput;
                }
                else
                {
                    inputFile = possibleInputFiles.First();
                }
            }

            Console.WriteLine($"Using party file {inputFile}.\n");
            return inputFile;
        }

        /// <summary>
        /// Recursively finds all possible input files (.csv) in a directory.
        /// </summary>
        /// <param name="path">The relative path to the directory to explore.</param>
        /// <param name="possibleInputFiles">A list in which to add any discovered .csv files.</param>
        private static void FindPossibleInputFiles(string path, List<string> possibleInputFiles)
        {
            // Add any .csv files in this directory to the list
            possibleInputFiles.AddRange(Directory.EnumerateFiles(path).Where(file => file.Contains(".csv")));

            // Recurse on any subdirectories
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                FindPossibleInputFiles(dir, possibleInputFiles);
            }
        }

        /// <summary>
        /// Reads player information from an input file.
        /// </summary>
        /// <param name="filePath">The relative path to a .csv file containing info about the players in the party.</param>
        /// <returns>An array of all players read from the input file.</returns>
        private static Player[] ReadPlayers(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            return lines.Skip(1).Select(line => Player.Create(line.Split(','))).ToArray();
        }

        /// <summary>
        /// The maximum damage that a single player from the party can deal with full buffs from the rest of the party.
        /// </summary>
        /// <param name="attacker">The player which will deal damage.</param>
        /// <param name="party">The other players in the party (<paramref name="attacker"/> is ignored if present).</param>
        /// <param name="verbose">If true, print a detailed explanation of the calculation.</param>
        /// <returns>The maximum damage that <paramref name="attacker"/> can deal with full buffs from the rest of the party.</returns>
        private static double MaxSingleDamage(Player attacker, Player[] party, bool verbose=false)
        {
            // Calculate max buff from other party members
            Stats teamBuff = Stats.Zero;
            string teamBuffVerboseMessage = string.Empty;
            foreach (Player player in party)
            {
                if (player != attacker)
                {
                    Stats buff = player.MaxBuffCount * player.Buff;
                    teamBuff += buff;
                    teamBuffVerboseMessage += $"{buff} from {player.Name}, ";
                }
            }

            // Identify the optimal number of buffs for attacker to cast
            double prevDamage = 0;
            string verboseMessage = string.Empty;
            for (int p1BuffCount = 0; ; p1BuffCount++)
            {
                Stats attackerBuff = p1BuffCount * attacker.Buff;
                Stats totalBuff = teamBuff + attackerBuff;
                double attackDamage = attacker.MaxAttackDamage(totalBuff, p1BuffCount, out string attackDamageVerboseMessage);
                double taskDamage = attacker.TaskDamage(totalBuff);
                double curDamage = attackDamage + taskDamage;
                if (curDamage <= prevDamage)
                {
                    WriteWithColor($"Max {attacker.Name} Damage = {prevDamage:0.0} with {p1BuffCount - 1} buffs cast", attacker.Color);
                    if (verbose)
                    {
                        WriteWithColor(verboseMessage, ConsoleColor.Gray);
                    }

                    return prevDamage;
                }
                else
                {
                    prevDamage = curDamage;
                    if (verbose)
                    {
                        verboseMessage =
                            $"Attack Damage = {attackDamage:0.0} ({attackDamageVerboseMessage})\n" +
                            $"Task Damage = {taskDamage:0.0}\n" +
                            $"Buff = {totalBuff} ({teamBuffVerboseMessage}{attackerBuff} from {attacker.Name})\n";
                    }
                }
            }
        }

        /// <summary>
        /// The maximum damage that a party can deal.
        /// </summary>
        /// <param name="party">The players in the party.</param>
        /// <param name="verbose">If true, print a detailed explanation of the calculation.</param>
        /// <returns>(The maximum damage the party can deal, the number of buffs cast by each player).</returns>
        private static (double, int[]) MaxPartyDamage(Player[] party, bool verbose=false)
        {
            int[] numBuffs = new int[party.Length];

            // Use the recursive helper function to find optimal buffs per player via brute force
            double maxDamage = OptimizePlayerBuffs(party, numBuffs, 0);

            // Print core information: total damage and buffs for each player
            StringBuilder message = new($"Max Party Damage = {maxDamage:0.0}; Buffs = {party[0].Name}: {numBuffs[0]}");
            for (int i = 1; i < party.Length; i++)
            {
                message.Append($", {party[i].Name}: {numBuffs[i]}");
            }
            WriteWithColor(message.ToString(), ConsoleColor.White);

            // Optionally print verbose information: damage dealt by each player and total buff
            if (verbose)
            {
                message.Clear();
                Stats buff = Stats.Zero;
                for (int i = 0; i < party.Length; i++)
                {
                    buff += party[i].Buff * numBuffs[i];
                }

                for (int i = 0; i < party.Length; i++)
                {
                    message.Append($"- {party[i].Name} deals {party[i].TaskDamage(buff):0.0} task and {party[i].MaxAttackDamage(buff, numBuffs[i], out string verboseMessage):0.0} attack damage ({verboseMessage})\n");
                }
                message.Append($"Total Buff = {buff}\n");
                WriteWithColor(message.ToString(), ConsoleColor.Gray);
            }

            return (maxDamage, numBuffs);
        }

        /// <summary>
        /// Optimizes the number of buffs cast by a subset of players in the party.
        /// </summary>
        /// <param name="party">The players in the party.</param>
        /// <param name="numBuffs">
        /// The number of buffs cast by each player; overwritten for index <paramref name="playerToOptimize"/> and beyond.
        /// </param>
        /// <param name="playerToOptimize"><paramref name="numBuffs"/> for every player of this index and beyond will be optimized</param>
        /// <returns>
        /// The maximum damage which can be dealt with the buffs already selected for players before <paramref name="playerToOptimize"/>.
        /// </returns>
        /// <remarks>
        /// Strategy: brute force all possible configurations of buff count per player.
        /// As an optimization, do not continue increasing buff count if it is past the peak for a given player.
        /// </remarks>
        private static double OptimizePlayerBuffs(Player[] party, int[] numBuffs, int playerToOptimize)
        {
            // Base case: numBuffs has been determined, so calculate the max damage with this buff structure.
            if (playerToOptimize >= party.Length)
            {
                return TeamDamage(party, numBuffs);
            }

            double bestDamage = 0;

            // Create a local copy of numBuffs so that we do not overwrite the optimal buffs in later calculations
            int[] curNumBuffs = new int[numBuffs.Length];
            Array.Copy(numBuffs, curNumBuffs, numBuffs.Length);

            // Increase the buffs cast by playerToOptimize until we find the maximum party damage
            bool noGain = false;
            for (int playerBuffs = 0; playerBuffs <= party[playerToOptimize].MaxBuffCount; playerBuffs++)
            {
                curNumBuffs[playerToOptimize] = playerBuffs;

                // Recurse to optimize the buffs for the remaining players ("right" of playerToOptimize)
                double curDamage = OptimizePlayerBuffs(party, curNumBuffs, playerToOptimize + 1);

                if (curDamage > bestDamage)
                {
                    // Only overwrite numBuffs if we see an improvement
                    noGain = false;
                    bestDamage = curDamage;
                    for (int i = playerToOptimize; i < numBuffs.Length; i++)
                    {
                        numBuffs[i] = curNumBuffs[i];
                    }
                }
                else
                {
                    // Optimization: if we do not improve twice in a row, we have found the maximum
                    //
                    // In a continuous world, damage is concave down along buff count for a given player, implying we can stop as soon as
                    // damage does not increase
                    //
                    // However, because spells are discrete and the Mage buff cost (35 mana) is not divisible by attack cost (10 mana),
                    // damage can decrease a single time while we are still on the "up" side of the concave down
                    if (noGain)
                    {
                        break;
                    }
                    noGain = true;
                }
            }

            return bestDamage;
        }

        /// <summary>
        /// Calculate the maximum damage the party can deal with the specified buffs per player.
        /// </summary>
        /// <param name="party">The players in the party.</param>
        /// <param name="numBuffs">The number of buffs cast by each player.</param>
        /// <returns>The maximum damage dealt.</returns>
        private static double TeamDamage(Player[] party, int[] numBuffs)
        {
            Stats buff = Stats.Zero;
            for (int i = 0; i < party.Length; i++)
            {
                buff += party[i].Buff * numBuffs[i];
            }

            double damage = 0;
            for (int i = 0; i < party.Length; i++)
            {
                damage += party[i].MaxAttackDamage(buff, numBuffs[i], out _);
                damage += party[i].TaskDamage(buff);
            }

            return damage;
        }

        /// <summary>
        /// Prints a message to the console with the specified color.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="color">The color of the text.</param>
        private static void WriteWithColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}