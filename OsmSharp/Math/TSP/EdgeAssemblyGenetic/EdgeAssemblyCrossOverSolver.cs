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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OsmSharp.Math.AI.Genetic.Operations;
using OsmSharp.Math.TSP.Problems;
using OsmSharp.Math.VRP.Core.Routes;
using OsmSharp.Math.AI.Genetic;
using OsmSharp.Math.TSP.Genetic.Solver;
using OsmSharp.Math.AI.Genetic.Solvers;
using OsmSharp.Math.VRP.Core.Routes.ASymmetric;

namespace OsmSharp.Math.TSP.EdgeAssemblyGenetic
{
    /// <summary>
    /// Implements a best-placement solver.
    /// </summary>
    public class EdgeAssemblyCrossOverSolver : SolverBase
    {
        ///// <summary>
        ///// Keeps the stopped flag.
        ///// </summary>
        //private bool _stopped = false;

        /// <summary>
        /// Keeps an orginal list of customers.
        /// </summary>
        //private IList<int> _customers;

        private int _population_size;

        /// <summary>
        /// Holds the stagnation count.
        /// </summary>
        private int _stagnation;

        /// <summary>
        /// Holds a generation operation.
        /// </summary>
        private IGenerationOperation<List<int>, GeneticProblem, Fitness> _generation_operation;

        /// <summary>
        /// Holds a generation operation.
        /// </summary>
        private ICrossOverOperation<List<int>, GeneticProblem, Fitness> _cross_over_operation;

        /// <summary>
        /// Creates a new solver.
        /// </summary>
        /// <param name="population_size"></param>
        /// <param name="stagnation"></param>
        /// <param name="generation_operation"></param>
        /// <param name="cross_over_operation"></param>
        public EdgeAssemblyCrossOverSolver(int population_size, int stagnation,
            IGenerationOperation<List<int>, GeneticProblem, Fitness> generation_operation,
            ICrossOverOperation<List<int>, GeneticProblem, Fitness> cross_over_operation)
        {
            //_stopped = false;
            _stagnation = stagnation;
            _population_size = population_size;

            _generation_operation = generation_operation;
            _cross_over_operation = cross_over_operation;
        }

        /// <summary>
        /// Creates a new solver.
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="customers"></param>
        public EdgeAssemblyCrossOverSolver(OsmSharp.Math.TSP.Problems.IProblem problem, IList<int> customers)
        {
            //_stopped = false;
            //_customers = customers;
        }

        /// <summary>
        /// Retuns the name of this solver.
        /// </summary>
        public override string Name
        {
            get
            {
                return string.Format("EAX{0}_{1}_{2}", _population_size,
                    _generation_operation.Name,
                    _cross_over_operation.Name);
            }
        }

        /// <summary>
        /// Returns a solution found using best-placement.
        /// </summary>
        /// <returns></returns>
        protected override IRoute DoSolve(OsmSharp.Math.TSP.Problems.IProblem problem)
        {
            // create the settings.
            SolverSettings settings = new SolverSettings(
                -1,
                -1,
                1000000000,
                -1,
                -1,
                -1);

            Solver<List<int>, GeneticProblem, Fitness> solver =
                new Solver<List<int>, GeneticProblem, Fitness>(
                new GeneticProblem(problem),
                settings,
                null,
                null,
                null,
                _generation_operation,
                new FitnessCalculator(),
                true, false);

            Population<List<int>, GeneticProblem, Fitness> population =
                new Population<List<int>, GeneticProblem, Fitness>(true);
            while (population.Count < _population_size)
            {
                // generate new.
                Individual<List<int>, GeneticProblem, Fitness> new_individual =
                    _generation_operation.Generate(solver);

                // add to population.
                population.Add(new_individual);
            }

            // select each individual once.
            Population<List<int>, GeneticProblem, Fitness> new_population =
                new Population<List<int>, GeneticProblem, Fitness>(true);
            Individual<List<int>, GeneticProblem, Fitness> best = null;
            int stagnation = 0;
            while (stagnation < _stagnation)
            {
                while (new_population.Count < _population_size)
                {
                    // select an individual and the next one.
                    int idx = OsmSharp.Math.Random.StaticRandomGenerator.Get().Generate(population.Count);
                    Individual<List<int>, GeneticProblem, Fitness> individual1 = population[idx];
                    Individual<List<int>, GeneticProblem, Fitness> individual2 = null;
                    if (idx == population.Count - 1)
                    {
                        individual2 = population[0];
                    }
                    else
                    {
                        individual2 = population[idx + 1];
                    }
                    population.RemoveAt(idx);

                    Individual<List<int>, GeneticProblem, Fitness> new_individual = _cross_over_operation.CrossOver(solver,
                        individual1, individual2);

                    new_individual.CalculateFitness(solver.Problem, solver.FitnessCalculator);
                    if (new_individual.Fitness.CompareTo(
                        individual1.Fitness) < 0)
                    {
                        new_population.Add(new_individual);
                    }
                    else
                    {
                        new_population.Add(individual1);
                    }
                }

                population = new_population;
                population.Sort(solver, solver.FitnessCalculator);

                new_population = new Population<List<int>, GeneticProblem, Fitness>(true);

                if (best == null ||
                    best.Fitness.CompareTo(population[0].Fitness) > 0)
                {
                    stagnation = 0;
                    best = population[0];
                }
                else
                {
                    stagnation++;
                }

                //// report progress.
                //OsmSharp.IO.Output.OutputStreamHost.ReportProgress(stagnation,_stagnation,
                //    "OsmSharp.Math.TSP.EdgeAssemblyGenetic.EdgeAssemblyCrossOverSolver",
                //    "Solving using EAX...");
            }

            List<int> result = new List<int>(best.Genomes);
            result.Insert(0, 0);
            //return new SimpleAsymmetricRoute(result, true);
            // Parameter problem.Symmetric inserted otherwise Tour is still round
            return DynamicAsymmetricRoute.CreateFrom(result, problem.Symmetric); 
        }

        /// <summary>
        /// Stops executiong.
        /// </summary>
        public override void Stop()
        {
            //_stopped = true;
        }
    }
}
