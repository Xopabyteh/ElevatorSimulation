using ElevatorSimulation.Strategies;

namespace ElevatorSimulation;

public static class Program
{
	public const int TimeForRequests = 20;
	public const int MaxFloor = 9;
	public const double RequestDensityPercent = 0.30;

	// Single simulation seed
	public const int SingleRandomSeed = 42017;

	// Tournament configuration
	public const bool TournamentMode = true; // Set to false for single strategy testing
	public static readonly int[] TournamentSeeds = { 42017, 12345, 99999, 54321, 77777 };

	public static void Main()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		var building = new Building(minFloor: 0, maxFloor: MaxFloor);
		
		if (TournamentMode)
		{
			RunTournament(building);
		}
		else
		{
			// Test single strategy
			RunSingleSimulation("MaFi", new MaFiStrategy(), building);
			Console.WriteLine("\n");
			RunSingleSimulation("FIFO STRATEGY", new FifoStrategy(), building);
		}
	}

	/// <summary>
	/// Runs a tournament with all discovered strategies.
	/// </summary>
	private static void RunTournament(Building building)
	{
		Console.WriteLine("🏁 STARTING STRATEGY TOURNAMENT");
		Console.WriteLine($"   Testing with {TournamentSeeds.Length} different scenarios (seeds)");
		Console.WriteLine();

		// Discover all strategies automatically
		var strategies = StrategyTournament.DiscoverStrategies();

		if (strategies.Count == 0)
		{
			Console.WriteLine("❌ No strategies found! Make sure you have classes implementing IElevatorStrategy.");
			return;
		}

		Console.WriteLine($"📋 Found {strategies.Count} strategies:");
		foreach (var (name, _) in strategies)
		{
			Console.WriteLine($"   - {name}");
		}
		Console.WriteLine();

		// Run tournament
		var tournament = new StrategyTournament(building, TournamentSeeds);
		var results = tournament.RunTournament(strategies);

		// Print results
		StrategyTournament.PrintTournamentResults(results);
	}

	private static void RunSingleSimulation(string strategyName, IElevatorStrategy strategy, Building building)
	{
		var runner = new SimulationRunner(building);
		runner.RunSimulation(
			strategy,
			SingleRandomSeed,
			TimeForRequests,
			RequestDensityPercent,
			silentMode: false,
			strategyName: strategyName);
	}
}
