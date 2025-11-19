# MaFi Strategy Training System

## Overview
This is a simple "machine learning" framework that trains the MaFi elevator strategy by testing random bias configurations and saving the best results.

## How It Works

1. **Generate Random Configurations**: The trainer generates random values for each bias parameter within reasonable ranges:
   - `MPickUpBias`: 0.5 to 5.0
   - `MDropOffBias`: 0.5 to 5.0
   - `MOpenDoorBias`: 1,000 to 10,000,000 (logarithmic scale)
   - `AMHeatMapBias`: 0.0 to 2.0
   - `MPrioritizeCurrentDirectionBias`: 1.0 to 5.0

2. **Evaluate**: Each configuration is tested across all tournament seeds (5 different scenarios) and scored based on average total passenger time (lower is better).

3. **Save Best**: The best configuration found so far is saved to `C:\Result.txt` every N iterations (default: 10).

## Usage

### Enable Training Mode
In `Program.cs`, set:
```csharp
public const bool TrainingMode = true;
public const int TrainingIterations = 100; // Adjust as needed
public const int SaveEveryNIterations = 10;
```

### Run the Training
Simply run the program:
```bash
dotnet run
```

### Monitor Progress
The console will show:
- Current iteration number
- Score for each tested configuration
- "?? NEW BEST SCORE!" when a better configuration is found
- "?? Saved best configuration" when results are saved

### Check Results
Open `C:\Result.txt` to see:
- The best configuration found
- The score (average total time)
- Complete bias parameters with high precision
- Ready-to-use C# code to paste into `MaFiStrategy.cs`

## Example Output (C:\Result.txt)

```
MaFi Strategy Training Results
===============================
Generated: 2024-01-15 14:30:22
Progress: 100/100 iterations

BEST CONFIGURATION FOUND:
-------------------------
Score (Avg Total Time): 15.2341

Bias Parameters:
  MPickUpBias:                      2.3456
  MDropOffBias:                     1.2345
  MOpenDoorBias:                    6789012.3456
  AMHeatMapBias:                    0.8765
  MPrioritizeCurrentDirectionBias:  2.9876

C# CODE TO USE:
---------------
public const double MPickUpBias = 2.3;
public const double MDropOffBias = 1.2;
public const double MOpenDoorBias = 6789012.3;
public const double AMHeatMapBias = 0.88;
public const double MPrioritizeCurrentDirectionBias = 3.0;
```

## Configuration Options

### Training Iterations
Increase for more thorough search:
```csharp
public const int TrainingIterations = 500; // More iterations = more configurations tested
```

### Save Frequency
Adjust how often results are saved:
```csharp
public const int SaveEveryNIterations = 5; // Save more frequently
```

### Result Path
Change where results are saved:
```csharp
public const string TrainingResultPath = @"D:\MyResults\training.txt";
```

## Tips for Best Results

1. **Start Small**: Begin with 50-100 iterations to get a feel for training time
2. **Increase Gradually**: If you see improvement, run more iterations (500-1000)
3. **Multiple Runs**: Run training multiple times - you might find different good configurations
4. **Validate Results**: After training, switch to `TournamentMode = true` to compare your trained strategy against others

## Implementation Details

### Files Created
- `MaFiTrainer.cs` - Main training framework
  - `MaFiTrainer` - Orchestrates the training process
  - `MaFiBiasConfig` - Holds bias parameter configurations
  - `TrainableMaFiStrategy` - A version of MaFiStrategy that uses configurable parameters

### Algorithm
This is a simple random search algorithm:
1. Start with default configuration
2. For each iteration:
   - Generate random bias values
   - Evaluate across all test seeds
   - Keep if better than current best
3. Save best configuration periodically

This approach is simple but effective for finding good parameter combinations. More advanced techniques (genetic algorithms, gradient descent, etc.) could be added later.
