﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using OsmSharp.Math.Automata;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Geo.Meta;
using OsmSharp.Math.StateMachines;
using OsmSharp.Routing.ArcAggregation.Output;

namespace OsmSharp.Routing.Instructions.MicroPlanning.Machines
{
    /// <summary>
    /// Machine to detect significant turns.
    /// </summary>
    internal class TurnMachine : MicroPlannerMachine
    {
        /// <summary>
        /// Creates a new turn machine.
        /// </summary>
        /// <param name="planner"></param>
        public TurnMachine(MicroPlanner planner)
            : base(TurnMachine.Initialize(), planner, 100)
        {

        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <returns></returns>
        private static FiniteStateMachineState<MicroPlannerMessage> Initialize()
        {
            // generate states.
            List<FiniteStateMachineState<MicroPlannerMessage>> states = FiniteStateMachineState<MicroPlannerMessage>.Generate(3);

            // state 2 is final.
            states[2].Final = true;

            // 0
            FiniteStateMachineTransition<MicroPlannerMessage>.Generate(states, 0, 0, typeof(MicroPlannerMessagePoint),
                new FiniteStateMachineTransitionCondition<MicroPlannerMessage>.FiniteStateMachineTransitionConditionDelegate(TestNonSignificantTurn));
            FiniteStateMachineTransition<MicroPlannerMessage>.Generate(states, 0, 1, typeof(MicroPlannerMessageArc));

            // 1
            FiniteStateMachineTransition<MicroPlannerMessage>.Generate(states, 1, 0, typeof(MicroPlannerMessagePoint),
                new FiniteStateMachineTransitionCondition<MicroPlannerMessage>.FiniteStateMachineTransitionConditionDelegate(TestNonSignificantTurn));
            FiniteStateMachineTransition<MicroPlannerMessage>.Generate(states, 1, 2, typeof(MicroPlannerMessagePoint),
                new FiniteStateMachineTransitionCondition<MicroPlannerMessage>.FiniteStateMachineTransitionConditionDelegate(TestSignificantTurn));

            // return the start automata with intial state.
            return states[0];
        }

        /// <summary>
        /// Tests if the given turn is significant.
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        private static bool TestNonSignificantTurn(FiniteStateMachine<MicroPlannerMessage> machine, object test)
        {
            if (!TurnMachine.TestSignificantTurn(machine, test))
            { // it is no signficant turn.
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tests if the given turn is significant.
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        private static bool TestSignificantTurn(FiniteStateMachine<MicroPlannerMessage> machine, object test)
        {
            if (test is MicroPlannerMessagePoint)
            {
                MicroPlannerMessagePoint point = (test as MicroPlannerMessagePoint);
                if (point.Point.Angle != null)
                {
                    if (point.Point.ArcsNotTaken == null || point.Point.ArcsNotTaken.Count == 0)
                    {
                        return false;
                    }
                    switch (point.Point.Angle.Direction)
                    {
                        case RelativeDirectionEnum.SlightlyLeft:
                        case RelativeDirectionEnum.SlightlyRight:
                            // test to see if is needed to generate instruction.
                            // if there is no other straight on
                            int straight_count = MicroPlannerHelper.GetStraightOn(point, (machine as MicroPlannerMachine).Planner.Interpreter);
                            if (straight_count > 0)
                            {
                                return true;
                            }
                            return false;
                        case OsmSharp.Math.Geo.Meta.RelativeDirectionEnum.StraightOn:
                            // test to see if this is cross road or anything.
                            int left_count = MicroPlannerHelper.GetLeft(point, (machine as MicroPlannerMachine).Planner.Interpreter);
                            int right_count = MicroPlannerHelper.GetRight(point, (machine as MicroPlannerMachine).Planner.Interpreter);
                            if (left_count > 0 && right_count > 0)
                            { // this straight-on is important.
                                return true;
                            }
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public override void Succes()
        {
            // get the last arc and the last point.
            var latestArc = (this.FinalMessages[this.FinalMessages.Count - 2] as MicroPlannerMessageArc).Arc;
            var latestPoint = (this.FinalMessages[this.FinalMessages.Count - 1] as MicroPlannerMessagePoint).Point;

            // count the number of streets in the same turning direction as the turn
            // that was found.
            int count = 0;
            if (MicroPlannerHelper.IsLeft(latestPoint.Angle.Direction, this.Planner.Interpreter))
            {
                count = MicroPlannerHelper.GetLeft(this.FinalMessages, this.Planner.Interpreter);
            }
            else if (MicroPlannerHelper.IsRight(latestPoint.Angle.Direction, this.Planner.Interpreter))
            {
                count = MicroPlannerHelper.GetRight(this.FinalMessages, this.Planner.Interpreter);
            }

            // construct the box indicating the location of the resulting find by this machine.
            var point1 = latestPoint.Location;
            var box = new GeoCoordinateBox(
                new GeoCoordinate(point1.Latitude - 0.001f, point1.Longitude - 0.001f),
                new GeoCoordinate(point1.Latitude + 0.001f, point1.Longitude + 0.001f));

            // descide what type of instruction to request be generated.  
            var metaData = new Dictionary<string, object>();         
            var streetFrom = latestArc.Tags;
            var streetTo = latestPoint.Next.Tags;
            var streetCountTurn = 0;
            var streetCountBeforeTurn = count;
            var direction = latestPoint.Angle;
            metaData["count_before"] = streetCountBeforeTurn;
            metaData["direction"] = direction;
            if (streetFrom == streetTo)
            {
                if (streetCountTurn == 0)
                {// there are no other streets between the one being turned into and the street coming from in the same
                    // direction as the turn.
                    metaData["type"] = "direct_follow_turn";
                }
                else
                { // there is another street; this is tricky to explain.
                    metaData["type"] = "indirect_follow_turn";
                }
            }
            else
            {
                if (streetCountTurn == 0)
                { // there are no other streets between the one being turned into and the street coming from in the same
                    // direction as the turn.
                    metaData["type"] = "direct_turn";
                }
                else
                { // there is another street; this is tricky to explain.
                    metaData["type"] = "indirect_turn";
                }
            }

            // let the scentence planner generate the correct information.
            metaData["street"] = streetTo;
            metaData["pois"] = latestPoint.Points;
            this.Planner.SentencePlanner.GenerateInstruction(metaData, latestPoint.EntryIdx, box, latestPoint.Points);
        }

        public override bool Equals(object obj)
        {
            if (obj is TurnMachine)
            { // if the machine can be used more than once 
                // this comparision will have to be updated.
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {// if the machine can be used more than once 
            // this hashcode will have to be updated.
            return this.GetType().GetHashCode();
        }
    }
}
