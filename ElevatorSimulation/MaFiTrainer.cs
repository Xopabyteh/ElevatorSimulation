using System.Globalization;
using ElevatorSimulation.Strategies;

namespace ElevatorSimulation;

/// <summary>
/// Simple machine learning framework that trains MaFiStrategy by testing random bias configurations.
/// </summary>
public class MaFiTrainer
{
    private readonly Building _building;
    private readonly int[] _trainingSeeds;
    private readonly string _resultFilePath;
    private readonly SimulationRunner _runner;
    
    // Current best configuration
    private MaFiBiasConfig _bestConfig;
    private double _bestScore = double.MaxValue;
    
    public MaFiTrainer(Building building, int[] trainingSeeds, string resultFilePath = @"C:\Result.txt")
    {
        _building = building;
        _trainingSeeds = trainingSeeds;
        _resultFilePath = resultFilePath;
        _runner = new SimulationRunner(building);
        
        // Initialize with default configuration
        _bestConfig = MaFiBiasConfig.CreateDefault();
    }
    
    /// <summary>
    /// Trains the strategy by testing random bias configurations.
    /// </summary>
    /// <param name="iterations">Number of configurations to test</param>
    /// <param name="saveEveryN">Save results every N iterations</param>
    public void Train(int iterations, int saveEveryN = 1)
    {
        Console.WriteLine("🎓 STARTING MAFI STRATEGY TRAINING");
        Console.WriteLine($"   Testing {iterations} random configurations");
        Console.WriteLine($"   Using {_trainingSeeds.Length} seeds for evaluation");
        Console.WriteLine($"   Saving best results to: {_resultFilePath}");
        Console.WriteLine();
        
        var random = new Random();
        
        // Test initial default configuration
        Console.WriteLine("Testing default configuration...");
        var defaultScore = EvaluateConfiguration(_bestConfig);
        _bestScore = defaultScore;
        SaveBestConfiguration(0, iterations);
        Console.WriteLine($"Default score: {defaultScore:F2}");
        Console.WriteLine();
        
        for (int i = 1; i <= iterations; i++)
        {
            Console.WriteLine($"[{i}/{iterations}] Testing random configuration...");
            
            // Generate random configuration
            var config = MaFiBiasConfig.CreateRandom(random);
            
            // Evaluate configuration
            var score = EvaluateConfiguration(config);
            
            Console.WriteLine($"  Score: {score:F2}");
            
            // Check if this is the best configuration
            if (score < _bestScore)
            {
                _bestScore = score;
                _bestConfig = config;
                Console.WriteLine($"  🎉 NEW BEST SCORE! {score:F2}");
            }
            
            // Save periodically
            if (i % saveEveryN == 0)
            {
                SaveBestConfiguration(i, iterations);
                Console.WriteLine($"  💾 Saved best configuration");
            }
            
            Console.WriteLine();
        }
        
        // Final save
        SaveBestConfiguration(iterations, iterations);
        
        Console.WriteLine("✅ TRAINING COMPLETE");
        Console.WriteLine($"   Best score: {_bestScore:F2}");
        Console.WriteLine($"   Results saved to: {_resultFilePath}");
    }
    
    /// <summary>
    /// Evaluates a configuration by running simulations across all training seeds.
    /// Returns the average total time (lower is better).
    /// </summary>
    private double EvaluateConfiguration(MaFiBiasConfig config)
    {
        var strategy = CreateStrategyFromConfig(config);
        var allStats = new List<Statistics>();
        
        foreach (var seed in _trainingSeeds)
        {
            var stats = _runner.RunSimulation(
                strategy,
                seed,
                Program.TimeForRequests,
                Program.RequestDensityPercent,
                silentMode: true);
            
            allStats.Add(stats);
        }
        
        // Return average total time across all seeds
        int totalCompleted = allStats.Sum(s => s.CompletedCount);
        double totalTotalTime = allStats.Sum(s => s.AverageTotalTime * s.CompletedCount);
        
        return totalCompleted > 0 ? totalTotalTime / totalCompleted : double.MaxValue;
    }
    
    /// <summary>
    /// Creates a MaFiStrategy instance with the given bias configuration.
    /// </summary>
    private static MaFiStrategy CreateStrategyFromConfig(MaFiBiasConfig config)
    {
        return new MaFiStrategy
        {
            MPickUpBias = config.MPickUpBias,
            MDropOffBias = config.MDropOffBias,
            MOpenDoorBias = config.MOpenDoorBias,
            AMHeatMapBias = config.AMHeatMapBias,
            MPrioritizeCurrentDirectionBias = config.MPrioritizeCurrentDirectionBias
        };
    }
    
    /// <summary>
    /// Saves the best configuration to the result file.
    /// </summary>
    private void SaveBestConfiguration(int currentIteration, int totalIterations)
    {
        var content = $@"MaFi Strategy Training Results
===============================
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Progress: {currentIteration}/{totalIterations} iterations

BEST CONFIGURATION FOUND:
-------------------------
Score (Avg Total Time): {_bestScore:F4}

Bias Parameters:
  MPickUpBias:                      {_bestConfig.MPickUpBias.ToString("F4", CultureInfo.InvariantCulture)}
  MDropOffBias:                     {_bestConfig.MDropOffBias.ToString("F4", CultureInfo.InvariantCulture)}
  MOpenDoorBias:                    {_bestConfig.MOpenDoorBias.ToString("F4", CultureInfo.InvariantCulture)}
  AMHeatMapBias:                    {_bestConfig.AMHeatMapBias.ToString("F4", CultureInfo.InvariantCulture)}
  MPrioritizeCurrentDirectionBias:  {_bestConfig.MPrioritizeCurrentDirectionBias.ToString("F4", CultureInfo.InvariantCulture)}

C# CODE TO USE:
---------------
public double MPickUpBias = {_bestConfig.MPickUpBias.ToString("F1", CultureInfo.InvariantCulture)};
public double MDropOffBias = {_bestConfig.MDropOffBias.ToString("F1", CultureInfo.InvariantCulture)};
public double MOpenDoorBias = {_bestConfig.MOpenDoorBias.ToString("F1", CultureInfo.InvariantCulture)};
public double AMHeatMapBias = {_bestConfig.AMHeatMapBias.ToString("F2", CultureInfo.InvariantCulture)};
public double MPrioritizeCurrentDirectionBias = {_bestConfig.MPrioritizeCurrentDirectionBias.ToString("F1", CultureInfo.InvariantCulture)};

Training Configuration:
-----------------------
Seeds used: {string.Join(", ", _trainingSeeds)}
Time for requests: {Program.TimeForRequests}
Request density: {Program.RequestDensityPercent:F2}
Building floors: {_building.MinFloor} to {_building.MaxFloor}
";
        
        try
        {
            File.WriteAllText(_resultFilePath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️ Error saving results: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a configuration of bias parameters for MaFiStrategy.
/// </summary>
public class MaFiBiasConfig
{
    public double MPickUpBias { get; set; }
    public double MDropOffBias { get; set; }
    public double MOpenDoorBias { get; set; }
    public double AMHeatMapBias { get; set; }
    public double MPrioritizeCurrentDirectionBias { get; set; }
    
    /// <summary>
    /// Creates the default configuration from MaFiStrategy defaults.
    /// </summary>
    public static MaFiBiasConfig CreateDefault()
    {
        return new MaFiBiasConfig
        {
            MPickUpBias = 2.0,
            MDropOffBias = 1.0,
            MOpenDoorBias = 3.0,
            AMHeatMapBias = 1.0,
            MPrioritizeCurrentDirectionBias = 3.0
        };
    }
    
    /// <summary>
    /// Creates a random configuration with reasonable parameter ranges.
    /// </summary>
    public static MaFiBiasConfig CreateRandom(Random random)
    {
        return new MaFiBiasConfig
        {
            // Pickup bias: 0.5 to 5.0
            MPickUpBias = random.NextDouble() * 1 + 0.5,
            
            // Dropoff bias: 0.5 to 5.0
            MDropOffBias = random.NextDouble() * 1 + 0.5,
            
            // Open door bias: 1.0 to 10.0
            MOpenDoorBias = random.NextDouble() * 5.0 + 1.0,

            // Heatmap bias: 0.0 to 2.0
            AMHeatMapBias = random.NextDouble() * 1.0,

            // Current direction bias: 1.0 to 5.0
            //MPrioritizeCurrentDirectionBias = random.NextDouble() * 4.0 + 1.0
            MPrioritizeCurrentDirectionBias = 1,
        };
    }
}
