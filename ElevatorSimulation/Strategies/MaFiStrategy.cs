using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorSimulation.Strategies;

internal class MaFiStrategy : IElevatorStrategy
{
    // M Prefix = multiply
    // A Prefix = add
    // AM Prefix = add/multiply (setting to 0 means no effect)

    public const double MPickUpBias = 2.0;
    public const double MDropOffBias = 1.0;
    public const double MOpenDoorBias = 5000000.0;

    public const double AMHeatMapBias = 0.8;
    public const double MPrioritizeCurrentDirectionBias = 3;


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

    private static Dictionary<int, double> GetScores(ElevatorSystem elevator)
    {
        var floorMoveScores = new Dictionary<int, double>();

        SetBaseRiderScores(elevator, floorMoveScores);
        AddHeatMapScores(elevator, floorMoveScores);
        AddSameDirectionScore(elevator, floorMoveScores);

        return floorMoveScores;
    }

    private static void SetBaseRiderScores(ElevatorSystem elevator, Dictionary<int, double> floorMoveScores)
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

            // If we are on the active floor, give a bonus to opening the door
            if (floor == elevator.CurrentElevatorFloor)
            {
                score *= MOpenDoorBias;
            }

            floorMoveScores[floor] = score;
        }
    }

    private static void AddHeatMapScores(ElevatorSystem elevator, Dictionary<int, double> floorMoveScores)
    {
        // Take each floor and add the score of floors around it (1 above + 1 below)
        var heatmapScores = new Dictionary<int, double>(floorMoveScores);

        for (int floor = elevator.Building.MinFloor; floor <= elevator.Building.MaxFloor; floor++)
        {
            double heatmapScore = 0d;

            // Add score from the floor itself (copied from floorMoveScores)
            heatmapScore += heatmapScores[floor];
            
            // Add score from the floor above
            if (floor < elevator.Building.MaxFloor)
            {
                heatmapScore += heatmapScores[floor + 1];
            }
            // Add score from the floor below
            if (floor > elevator.Building.MinFloor)
            {
                heatmapScore += heatmapScores[floor - 1];
            }

            heatmapScore /= 3.0; // Average score

            // Apply heatmap bias
            heatmapScore *= AMHeatMapBias;
            floorMoveScores[floor] += heatmapScore;
        }
    }

    private static void AddSameDirectionScore(ElevatorSystem elevator , Dictionary<int, double> floorMoveScores)
    {
        // If the elevator is moving up, prioritize floors above
        // If the elevator is moving down, prioritize floors below
        if (elevator.CurrentElevatorDirection == Direction.Up)
        {
            for (int floor = elevator.CurrentElevatorFloor + 1; floor <= elevator.Building.MaxFloor; floor++)
            {
                floorMoveScores[floor] *= MPrioritizeCurrentDirectionBias;
            }
        }
        else if (elevator.CurrentElevatorDirection == Direction.Down)
        {
            for (int floor = elevator.Building.MinFloor; floor < elevator.CurrentElevatorFloor; floor++)
            {
                floorMoveScores[floor] *= MPrioritizeCurrentDirectionBias;
            }
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
