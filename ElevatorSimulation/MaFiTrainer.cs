using System.Globalization;
using System.Text.Json;
using ElevatorSimulation.Strategies;

namespace ElevatorSimulation;

/// <summary>
/// Genetic/mutation-based trainer for MaFiStrategy parameters.
/// Continuously mutates and tests configurations to find optimal bias values.
/// </summary>
public class MaFiTrainer
{
    private readonly Building _building;
    private readonly int[] _evaluationSeeds;
    private readonly SimulationRunner _runner;
    private readonly string _outputPath;
    private readonly string _configPath;
    
    // Current best configuration
    private MaFiBiasConfig _bestConfig;
    private double _bestScore = double.MaxValue;
    private int _iterationsSinceImprovement = 0;
    private int _totalIterations = 0;
    
    public MaFiTrainer(Building building, int[] evaluationSeeds, string outputPath = @"C:\MaFi_Training_Results.txt")
    {
        _building = building;
        _evaluationSeeds = evaluationSeeds;
        _runner = new SimulationRunner(building);
        _outputPath = outputPath;
        _configPath = Path.ChangeExtension(outputPath, ".json");
        
        // Try to load previous configuration first
        var loadedConfig = LoadPreviousConfiguration();
        
        if (loadedConfig != null)
        {
            // Use loaded configuration
            _bestConfig = loadedConfig;
        }
        else
        {
            // Initialize with current default configuration from MaFiStrategy
            _bestConfig = new MaFiBiasConfig
            {
                MPickUpBias = 2.0,
                MDropOffBias = 2.5,
                MOpenDoorBias = 10.0,
                AMHeatMapBias = 0.0,
                MPrioritizeCurrentDirectionBias = 2.2,
                MStarvationMultiplier = 1.3,
                StarvationThreshold = 15,
                TravelCostPerFloor = 0.05,
                HeatMapRadius = 1
            };
        }
    }
    
    /// <summary>
    /// Runs continuous training for specified number of iterations.
    /// Prints progress only every N iterations to reduce console spam.
    /// </summary>
    public void Train(int printEveryN = 10)
    {
        Console.WriteLine("🎓 MAFI STRATEGY GENETIC TRAINER");
        Console.WriteLine($"   Evaluation seeds: {_evaluationSeeds.Length}");
        Console.WriteLine($"   Results will be saved to: {_outputPath}");
        Console.WriteLine($"   Configuration will be saved to: {_configPath}");
        Console.WriteLine();
        
        var random = new Random();
        
        // Evaluate initial/loaded configuration
        if (_totalIterations == 0)
        {
            Console.WriteLine("Evaluating initial configuration...");
            _bestScore = EvaluateConfiguration(_bestConfig);
            Console.WriteLine($"Initial score: {_bestScore:F2}");
        }
        else
        {
            Console.WriteLine($"Resuming from previous session (iteration {_totalIterations})...");
            Console.WriteLine($"Previous best score: {_bestScore:F2}");
            Console.WriteLine($"Iterations since last improvement: {_iterationsSinceImprovement}");
        }
        
        SaveResults(0);
        SaveConfiguration();
        Console.WriteLine();
        
        while (true)
        {
            _totalIterations++;
            
            // Generate mutated configuration based on current best
            var mutationRate = CalculateMutationRate();
            var candidateConfig = MaFiBiasConfig.Mutate(_bestConfig, random, mutationRate);
            
            // Evaluate candidate
            var score = EvaluateConfiguration(candidateConfig);
            
            // Check if this is an improvement
            if (score < _bestScore)
            {
                var improvement = _bestScore - score;
                var improvementPercent = (improvement / _bestScore) * 100;
                
                _bestScore = score;
                _bestConfig = candidateConfig;
                _iterationsSinceImprovement = 0;
                
                Console.WriteLine($"[{_totalIterations}] 🎉 NEW BEST! Score: {score:F2} (↓ {improvement:F2}, {improvementPercent:F1}%)");
                SaveResults(_totalIterations);
                SaveConfiguration();
            }
            else
            {
                _iterationsSinceImprovement++;
                
                // Print progress periodically
                if (_totalIterations % printEveryN == 0)
                {
                    Console.WriteLine($"[{_totalIterations}] Current best: {_bestScore:F2} | Last improvement: {_iterationsSinceImprovement} iterations ago | Mutation rate: {mutationRate:P0}");
                    SaveConfiguration(); // Save progress periodically
                }
            }
        }
    }
    
    /// <summary>
    /// Calculates mutation rate based on progress.
    /// Uses higher mutation when stuck, lower when making progress.
    /// </summary>
    private double CalculateMutationRate()
    {
        // Start with base rate of 0.2 (20%)
        const double baseRate = 0.2;
        
        // Increase mutation rate if stuck (no improvement for many iterations)
        if (_iterationsSinceImprovement > 50)
            return 0.5; // 50% mutation for exploration
        else if (_iterationsSinceImprovement > 20)
            return 0.35; // 35% mutation
        else
            return baseRate; // 20% mutation for fine-tuning
    }
    
    /// <summary>
    /// Evaluates a configuration by running simulations across all evaluation seeds.
    /// Returns the average total time (lower is better).
    /// </summary>
    private double EvaluateConfiguration(MaFiBiasConfig config)
    {
        var strategy = CreateStrategyFromConfig(config);
        var allStats = new List<Statistics>();
        
        foreach (var seed in _evaluationSeeds)
        {
            var stats = _runner.RunSimulation(
                strategy,
                seed,
                Program.TimeForRequests,
                Program.RequestDensityPercent,
                silentMode: true);
            
            allStats.Add(stats);
        }
        
        // Return average total time across all seeds (primary optimization metric)
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
            MPrioritizeCurrentDirectionBias = config.MPrioritizeCurrentDirectionBias,
            MStarvationMultiplier = config.MStarvationMultiplier,
            StarvationThreshold = config.StarvationThreshold,
            TravelCostPerFloor = config.TravelCostPerFloor,
            HeatMapRadius = config.HeatMapRadius
        };
    }
    
    /// <summary>
    /// Loads the previous best configuration from JSON file if it exists.
    /// </summary>
    private MaFiBiasConfig? LoadPreviousConfiguration()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                var savedState = JsonSerializer.Deserialize<TrainingState>(json);
                
                if (savedState != null)
                {
                    _bestScore = savedState.BestScore;
                    _iterationsSinceImprovement = savedState.IterationsSinceImprovement;
                    _totalIterations = savedState.TotalIterations;
                    
                    Console.WriteLine($"✅ Loaded previous configuration from {_configPath}");
                    return savedState.BestConfig;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not load previous configuration: {ex.Message}");
            Console.WriteLine("   Starting with default configuration...");
        }
        
        return null;
    }
    
    /// <summary>
    /// Saves the current best configuration and training state to JSON file.
    /// </summary>
    private void SaveConfiguration()
    {
        try
        {
            var state = new TrainingState
            {
                BestConfig = _bestConfig,
                BestScore = _bestScore,
                IterationsSinceImprovement = _iterationsSinceImprovement,
                TotalIterations = _totalIterations,
                LastUpdated = DateTime.Now
            };
            
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            string json = JsonSerializer.Serialize(state, options);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️ Error saving configuration: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Saves current best configuration to file.
    /// </summary>
    private void SaveResults(int currentIteration)
    {
        var content = $@"MaFi Strategy Training Results
===============================
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Iteration: {currentIteration}
Iterations since last improvement: {_iterationsSinceImprovement}

BEST CONFIGURATION FOUND:
-------------------------
Score (Avg Total Time): {_bestScore:F4}

Bias Parameters:
  MPickUpBias:                      {_bestConfig.MPickUpBias.ToString("F4", CultureInfo.InvariantCulture)}
  MDropOffBias:                     {_bestConfig.MDropOffBias.ToString("F4", CultureInfo.InvariantCulture)}
  MOpenDoorBias:                    {_bestConfig.MOpenDoorBias.ToString("F4", CultureInfo.InvariantCulture)}
  AMHeatMapBias:                    {_bestConfig.AMHeatMapBias.ToString("F4", CultureInfo.InvariantCulture)}
  MPrioritizeCurrentDirectionBias:  {_bestConfig.MPrioritizeCurrentDirectionBias.ToString("F4", CultureInfo.InvariantCulture)}
  MStarvationMultiplier:            {_bestConfig.MStarvationMultiplier.ToString("F4", CultureInfo.InvariantCulture)}
  StarvationThreshold:              {_bestConfig.StarvationThreshold}
  TravelCostPerFloor:               {_bestConfig.TravelCostPerFloor.ToString("F4", CultureInfo.InvariantCulture)}
  HeatMapRadius:                    {_bestConfig.HeatMapRadius}

C# CODE TO USE IN MaFiStrategy.cs:
-----------------------------------
public double MPickUpBias = {_bestConfig.MPickUpBias.ToString("F1", CultureInfo.InvariantCulture)};
public double MDropOffBias = {_bestConfig.MDropOffBias.ToString("F1", CultureInfo.InvariantCulture)};
public double MOpenDoorBias = {_bestConfig.MOpenDoorBias.ToString("F1", CultureInfo.InvariantCulture)};
public double AMHeatMapBias = {_bestConfig.AMHeatMapBias.ToString("F2", CultureInfo.InvariantCulture)};
public double MPrioritizeCurrentDirectionBias = {_bestConfig.MPrioritizeCurrentDirectionBias.ToString("F1", CultureInfo.InvariantCulture)};
public double MStarvationMultiplier = {_bestConfig.MStarvationMultiplier.ToString("F1", CultureInfo.InvariantCulture)};
public int StarvationThreshold = {_bestConfig.StarvationThreshold};
public double TravelCostPerFloor = {_bestConfig.TravelCostPerFloor.ToString("F2", CultureInfo.InvariantCulture)};
public int HeatMapRadius = {_bestConfig.HeatMapRadius};

Training Configuration:
-----------------------
Time for requests: {Program.TimeForRequests}
Request density: {Program.RequestDensityPercent:F2}
Building floors: {_building.MinFloor} to {_building.MaxFloor}
";
        
        try
        {
            File.WriteAllText(_outputPath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️ Error saving results: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents the training state that can be saved and loaded.
/// </summary>
public class TrainingState
{
    public MaFiBiasConfig BestConfig { get; set; } = new();
    public double BestScore { get; set; }
    public int IterationsSinceImprovement { get; set; }
    public int TotalIterations { get; set; }
    public DateTime LastUpdated { get; set; }
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
    
    // New optimization parameters
    public double MStarvationMultiplier { get; set; }
    public int StarvationThreshold { get; set; }
    public double TravelCostPerFloor { get; set; }
    public int HeatMapRadius { get; set; }
    
    /// <summary>
    /// Creates a mutated version of the given configuration.
    /// </summary>
    public static MaFiBiasConfig Mutate(MaFiBiasConfig original, Random random, double mutationRate = 0.2)
    {
        return new MaFiBiasConfig
        {
            // Pickup bias: 0.5 to 3.0
            MPickUpBias = MutateValue(random, original.MPickUpBias, mutationRate, 0.5, 3.0),
            
            // Dropoff bias: 0.5 to 3.0
            MDropOffBias = MutateValue(random, original.MDropOffBias, mutationRate, 0.5, 3.0),
            
            // Open door bias: 1.0 to 10.0
            MOpenDoorBias = MutateValue(random, original.MOpenDoorBias, mutationRate, 1.0, 10.0),
            
            // Heatmap bias: 0.0 to 2.0
            AMHeatMapBias = MutateValue(random, original.AMHeatMapBias, mutationRate, 0.0, 2.0),
            
            // Current direction bias: 1.0 to 5.0
            MPrioritizeCurrentDirectionBias = MutateValue(random, original.MPrioritizeCurrentDirectionBias, mutationRate, 1.0, 5.0),
            
            // Starvation multiplier: 1.1 to 2.0
            MStarvationMultiplier = MutateValue(random, original.MStarvationMultiplier, mutationRate, 1.1, 2.0),
            
            // Starvation threshold: 5 to 30 time units
            StarvationThreshold = (int)MutateValue(random, original.StarvationThreshold, mutationRate, 5, 30),
            
            // Travel cost per floor: 0.0 to 0.2
            TravelCostPerFloor = MutateValue(random, original.TravelCostPerFloor, mutationRate, 0.0, 0.2),
            
            // Heatmap radius: 1 to 3
            HeatMapRadius = (int)MutateValue(random, original.HeatMapRadius, mutationRate, 1, 3),
        };
    }
    
    /// <summary>
    /// Mutates a single value by adding random noise proportional to its valid range.
    /// </summary>
    private static double MutateValue(Random random, double currentValue, double mutationRate, double min, double max)
    {
        // Calculate the range for mutation
        double range = max - min;
        double maxDeviation = range * mutationRate;
        
        // Add random deviation (can be positive or negative)
        double deviation = (random.NextDouble() * 2 - 1) * maxDeviation;
        double newValue = currentValue + deviation;
        
        // Clamp to valid range
        return Math.Clamp(newValue, min, max);
    }
}
