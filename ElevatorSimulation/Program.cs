using ElevatorSimulation.Strategies;

namespace ElevatorSimulation;

public static class Program
{
	public const int TimeForRequests = 20;
	public const int MaxFloor = 9;
	public const double RequestDensityPercent = 0.30;

	// Single simulation seed
	public const int RandomSeed = 42017;

	// Tournament configuration
	public const bool TournamentMode = true; // Set to false for single strategy testing
	public const int TournamentScenariosCount = 10000;

	public static void Main()
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		var building = new Building(minFloor: 0, maxFloor: MaxFloor);

		// Run MaFi Trainer
		//RunMaFiTrainer(building);
		//return;
		// Uncomment below to run tournament

		if (TournamentMode)
		{
			RunTournament(building);
		}
		else
		{
			// Test single strategy
			RunSingleSimulation("JENDA STRATEGY", new JendaChatGPTStrategy(), building);
			Console.WriteLine("\n");
			//RunSingleSimulation("NEAREST FIRST STRATEGY", new NearestFirstStrategy(), building);
		}

	}

	/// <summary>
	/// Runs the MaFi strategy trainer.
	/// </summary>
	private static void RunMaFiTrainer(Building building)
	{
		// Generate evaluation seeds
		var random = new Random(RandomSeed);
		var evaluationSeeds = Enumerable.Range(1, 500).Select(_ => random.Next()).ToArray();
		
		var trainer = new MaFiTrainer(building, evaluationSeeds, @"C:\users\xopab\desktop\MaFi_Training_Results.txt");
		trainer.Train(printEveryN: 10);
	}

	/// <summary>
	/// Runs a tournament with all discovered strategies.
	/// </summary>
	private static void RunTournament(Building building)
	{
		var random = new Random(RandomSeed);
		var TournamentSeeds = Enumerable.Range(1, TournamentScenariosCount).Select(_ => random.Next()).ToArray();

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
			RandomSeed,
			TimeForRequests,
			RequestDensityPercent,
			silentMode: false,
			strategyName: strategyName);
	}
}
