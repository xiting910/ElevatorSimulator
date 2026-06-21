using System;
using System.Collections.Generic;

namespace ElevatorSimulator.Server.Core.Controllers;

// 呼叫预测 — PredictTimeToServeExternalCall
public sealed partial class ElevatorController
{
    /// <summary> 一次完整停靠门周期的时长 </summary>
    private const double FullDoorCycleSec = Constants.DoorOpenCloseTimeSec + Constants.DoorOpenWaitTimeSec + Constants.DoorOpenCloseTimeSec;

    /// <summary> 模拟预测的最大步数, 防止意外死循环 </summary>
    private const int MaxSteps = (Constants.MaxFloor - Constants.MinFloor) * 4;

    /// <inheritdoc/>
    public double PredictTimeToServeExternalCall(int floor, Direction direction)
    {
        SortedSet<int> internalCopy, upExternalCopy, downExternalCopy;
        Direction currentDir;

        lock (_tasksLock)
        {
            internalCopy = [.. _internalTasks];
            upExternalCopy = [.. _upExternalTasks];
            downExternalCopy = [.. _downExternalTasks];
            currentDir = _currentDirection;
        }

        int passedSinceFloor = _passedIntervalsSinceLastFloor, passedSinceDoor = _passedIntervalsSinceDoorOpened;

        if (direction is Direction.Up)
        {
            _ = upExternalCopy.Add(floor);
        }
        else if (direction is Direction.Down)
        {
            _ = downExternalCopy.Add(floor);
        }

        var curFloor = State.CurrentFloor;
        var physDir = State.MovingDirection;
        var door = State.Door;
        var ratio = State.DoorOpenRatio;

        if (physDir is Direction.Up && HasAnyInRange(upExternalCopy, curFloor + 1, Constants.MaxFloor - 1))
        {
            _ = internalCopy.Add(Constants.MaxFloor);
        }
        else if (physDir is Direction.Down && HasAnyInRange(downExternalCopy, Constants.MinFloor + 1, curFloor - 1))
        {
            _ = internalCopy.Add(Constants.MinFloor);
        }

        var totalSec = 0.0;

        // 第一步: 结算当前物理状态
        if (physDir is Direction.Up or Direction.Down)
        {
            if (currentDir != physDir)
            {
                throw new InvalidOperationException("Physical direction does not match logical direction, possible bug in state management");
            }

            totalSec += (IntervalsPerFloor - passedSinceFloor) * Constants.UpdateInterval / 1000.0;
            curFloor += physDir is Direction.Up ? 1 : -1;
        }
        else
        {
            switch (door)
            {
                case DoorState.Opening:
                    totalSec += (1.0 - ratio) * Constants.DoorOpenCloseTimeSec;
                    _ = internalCopy.Remove(curFloor);
                    if (currentDir is Direction.Up) { _ = upExternalCopy.Remove(curFloor); }
                    else if (currentDir is Direction.Down) { _ = downExternalCopy.Remove(curFloor); }
                    totalSec += Constants.DoorOpenWaitTimeSec + Constants.DoorOpenCloseTimeSec;
                    break;

                case DoorState.Open:
                    totalSec += (IntervalsDoorOpen - passedSinceDoor) * Constants.UpdateInterval / 1000.0;
                    totalSec += Constants.DoorOpenCloseTimeSec;
                    break;

                case DoorState.Closing:
                    totalSec += ratio * Constants.DoorOpenCloseTimeSec;
                    break;

                case DoorState.Closed:
                    break;
            }

            if (curFloor == floor && (currentDir == direction || currentDir is Direction.None))
            {
                if (door is DoorState.Closed or DoorState.Closing)
                {
                    totalSec += Constants.DoorOpenCloseTimeSec;
                }
                return totalSec;
            }
        }

        // 第二步: 模拟 Look 路径
        if (currentDir is Direction.None)
        {
            currentDir = floor > curFloor ? Direction.Up : Direction.Down;
        }

        var step = 0;
        while (step < MaxSteps)
        {
            step++;

            var nextStop = currentDir is Direction.Up ? GetNextStopUp(internalCopy, upExternalCopy, downExternalCopy, curFloor) : GetNextStopDown(internalCopy, upExternalCopy, downExternalCopy, curFloor);

            if (nextStop is null)
            {
                currentDir = currentDir is Direction.Up ? Direction.Down : Direction.Up;
                continue;
            }

            totalSec += Math.Abs(nextStop.Value - curFloor) * Constants.FloorTravelTimeSec;
            curFloor = nextStop.Value;

            _ = internalCopy.Remove(curFloor);
            _ = currentDir is Direction.Up ? upExternalCopy.Remove(curFloor) : downExternalCopy.Remove(curFloor);

            if (curFloor == floor)
            {
                if (currentDir == direction)
                {
                    totalSec += Constants.DoorOpenCloseTimeSec;
                    return totalSec;
                }

                if (currentDir is Direction.Up && !HasAnyInRange(internalCopy, curFloor + 1, Constants.MaxFloor) && !HasAnyInRange(upExternalCopy, curFloor + 1, Constants.MaxFloor - 1) && !HasAnyInRange(downExternalCopy, curFloor + 1, Constants.MaxFloor))
                {
                    totalSec += Constants.DoorOpenCloseTimeSec;
                    return totalSec;
                }
                else if (currentDir is Direction.Down && !HasAnyInRange(internalCopy, Constants.MinFloor, curFloor - 1) && !HasAnyInRange(upExternalCopy, Constants.MinFloor, curFloor - 1) && !HasAnyInRange(downExternalCopy, Constants.MinFloor + 1, curFloor - 1))
                {
                    totalSec += Constants.DoorOpenCloseTimeSec;
                    return totalSec;
                }
            }

            totalSec += FullDoorCycleSec;
        }

        return step >= MaxSteps ? throw new InvalidOperationException("Simulation exceeded maximum steps, possible bug in Look algorithm implementation") : totalSec;
    }
}
