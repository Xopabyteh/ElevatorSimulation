namespace ElevatorSimulation.Strategies;

internal class MaFiStrategy : IElevatorStrategy
{
    // M Prefix = multiply
    // A Prefix = add
    // AM Prefix = add/multiply (setting to 0 means no effect)

    public double MPickUpBias = 1.5;
    public double MDropOffBias = 0.9;
    public double MOpenDoorBias = 7.8;
    public double AMHeatMapBias = 0.98;
    public double MPrioritizeCurrentDirectionBias = 1.8;


    public MoveResult DecideNextMove(ElevatorSystem elevator)
    {
        // The resulting score is the cummulative time of all passengers waiting + travelling
        // so we want to minimize that

        // If no riders and no requests, go towards the middle
        if (elevator.PendingRequests.Count == 0 && elevator.ActiveRiders.Count == 0)
        {
            var middle = elevator.Building.MinFloor + (elevator.Building.MaxFloor - elevator.Building.MinFloor) / 2;

            return MoveTowardsFloor(elevator, middle);
        }

        var floorMoveScores = GetScores(elevator);

        // Find the floor with the highest score, and move towards it
        // if it's the current floor, open doors
        var bestFloor = floorMoveScores.MaxBy(kv => kv.Value).Key;
        if (bestFloor == elevator.CurrentElevatorFloor)
        {
            return MoveResult.OpenDoors;
        }
        else
        {
            return MoveTowardsFloor(elevator, bestFloor);
        }
    }

    private Dictionary<int, double> GetScores(ElevatorSystem elevator)
    {
        var floorMoveScores = new Dictionary<int, double>();

        SetBaseRiderScores(elevator, floorMoveScores);
        AddHeatMapScores(elevator, floorMoveScores);    
        AddSameDirectionScore(elevator, floorMoveScores);

        AddOpenDoorScore(elevator, floorMoveScores); // Always at the end...

        return floorMoveScores;
    }

    private void SetBaseRiderScores(ElevatorSystem elevator, Dictionary<int, double> floorMoveScores)
    {
        for (int floor = elevator.Building.MinFloor; floor <= elevator.Building.MaxFloor; floor++)
        {
            double score = 0d;

            var waitingRiders = elevator.PendingRequests.Where(r => r.From == floor).ToArray();
            var activeRiders = elevator.ActiveRiders.Where(r => r.To == floor).ToArray();

            // Add score for pickup and dropoff
            // Prioritize spaces with most riders waiting + most riders to drop off
            score += waitingRiders.Length * MPickUpBias;
            score += activeRiders.Length * MDropOffBias;

            floorMoveScores[floor] = score;
        }
    }

    private void AddHeatMapScores(ElevatorSystem elevator, Dictionary<int, double> floorMoveScores)
    {
        // Take each floor and add the score of floors around it (1 above + 1 below)
        var heatmapScores = new Dictionary<int, double>(floorMoveScores.Count);

        for (int floor = elevator.Building.MinFloor; floor <= elevator.Building.MaxFloor; floor++)
        {
            double heatmapScore = 0d;

            // Add score from the floor itself
            heatmapScore += floorMoveScores[floor];
            
            // Add score from the floor above
            if (floor < elevator.Building.MaxFloor)
            {
                heatmapScore += floorMoveScores[floor + 1];
            }
            // Add score from the floor below
            if (floor > elevator.Building.MinFloor)
            {
                heatmapScore += floorMoveScores[floor - 1];
            }

            heatmapScore /= 3.0; // Average score

            // Apply heatmap bias
            heatmapScore *= AMHeatMapBias;
            floorMoveScores[floor] += heatmapScore;
        }
    }

    private void AddSameDirectionScore(ElevatorSystem elevator , Dictionary<int, double> floorMoveScores)
    {
        // If the elevator is moving up, prioritize floors above
        // If the elevator is moving down, prioritize floors below

        // Only do so if there are active riders or pending requests in that direction
        if (elevator.CurrentElevatorDirection == Direction.Up)
        {
            bool hasUpRequests = elevator.PendingRequests.Any(r => r.From > elevator.CurrentElevatorFloor) ||
                                 elevator.ActiveRiders.Any(r => r.To > elevator.CurrentElevatorFloor);
            if (hasUpRequests)
            {
                for (int floor = elevator.CurrentElevatorFloor + 1; floor <= elevator.Building.MaxFloor; floor++)
                {
                    floorMoveScores[floor] *= MPrioritizeCurrentDirectionBias;
                }
            }
        }
        else if (elevator.CurrentElevatorDirection == Direction.Down)
        {
            bool hasDownRequests = elevator.PendingRequests.Any(r => r.From < elevator.CurrentElevatorFloor) ||
                                   elevator.ActiveRiders.Any(r => r.To < elevator.CurrentElevatorFloor);
            if (hasDownRequests)
            {
                for (int floor = elevator.Building.MinFloor; floor < elevator.CurrentElevatorFloor; floor++)
                {
                    floorMoveScores[floor] *= MPrioritizeCurrentDirectionBias;
                }
            }
        }
    }

    private void AddOpenDoorScore(ElevatorSystem elevator, Dictionary<int, double> floorMoveScores)
    {
        var currentFloor = elevator.CurrentElevatorFloor;
        if (elevator.PendingRequests.Any(r => r.From == currentFloor) ||
            elevator.ActiveRiders.Any(r => r.To == currentFloor))
        {
            floorMoveScores[currentFloor] *= MOpenDoorBias;
        }
    }

    private MoveResult MoveTowardsFloor(ElevatorSystem elevator, int targetFloor)
    {
        if (elevator.CurrentElevatorFloor < targetFloor)
            return MoveResult.MoveUp;
        else if (elevator.CurrentElevatorFloor > targetFloor)
            return MoveResult.MoveDown;
        else
            return MoveResult.OpenDoors;
    }
}
