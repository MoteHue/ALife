using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Genetic_Algorithm {
    class Program {

        // Initialise the GA population
        // Fill an empty population array with N=pop_size random individuals
        // Each individual is represented by a Dictionary with two elements: "solution" as the key and "fitness" as the value
        // each "fitness" is initialised to 0 as the associated solution has not yet been assessed
        // each "solution" is initialised to a string of random symbols from the alphabet
        // either each "solution" is the same random string (converged=True) or
        // each "solution" is a different random string (converged=False)
        // the function as provided doesn't implement the converged=True functionality
        static Dictionary<string, float> Initialise(int popSize, int genomeLength, string geneticAlphabet, bool converged) {
            Dictionary<string, float> pop = new Dictionary<string, float>();
            Random rand = new Random();
            if (converged) {
                string solution = "";
                for(int i = 0; i < genomeLength; i++) {
                    solution += geneticAlphabet[rand.Next(genomeLength)];
                }
                while (pop.Count < popSize) {
                    pop.Add(solution, 0);
                }
            } else {
                while (pop.Count < popSize) {
                    string solution = "";
                    for (int i = 0; i < genomeLength; i++) {
                        solution += geneticAlphabet[rand.Next(geneticAlphabet.Length)];
                    }
                    pop.Add(solution, 0);
                }
            }

            return pop;
        }

        // Count the number of locations for which two strings of the same length match.
        // E.g, matches of "red", "rod" should be 2.
        static int Matches(string str1, string str2) {
            int count = 0;
            for (int i = 0; i < str1.Length; i++) {
                if (str1[i] == str2[i]) count++;
            }
            return count;
        }

        // Assess the fitness of each individual in the current population
        // For each individual, count the number of symbols in the solution that match the target string
        // Store this as the fitness of the individual (normalised by the target string length)
        // Maximum fitness is thus 1 (all symbols match); minimum fitness is 0 (no matches).
        // IGNORE BELOW, C# DICTIONARY ORDER IS NONDETERMINISTIC
        // Sort the population by fitness with the best solution at the top of the list
        //     * this last step is important because it helps us track the best solution and
        // will also be useful when we implement elitism...
        static Dictionary<string, float> Assess(Dictionary<string, float> pop, string target) {
            float length = target.Length;
            Dictionary<string, float> tempPopStore = new Dictionary<string, float>();
            foreach(KeyValuePair<string, float> entry in pop) {
                tempPopStore.Add(entry.Key, Matches(entry.Key, target) / length);
            }
            return tempPopStore;
        }

        static KeyValuePair<string, float> GetHighestInduvidual(Dictionary<string, float> pop) {
            KeyValuePair<string, float> winner = pop.ElementAt(0);
            float currentHighestFitness = winner.Value;
            foreach (KeyValuePair<string, float> entry in pop) {
                if (entry.Value > winner.Value) winner = entry;
            }

            return winner;
        }

        // Run tournament selection to pick a parent solution
        // Consider a sample of tournament_size unique indivduals from the current population
        // Return the solution belonging to the winner (the individual with the highest fitness)
        static string Tournament(Dictionary<string, float> pop, int tournamentSize) {
            Dictionary<string, float> competitors = new Dictionary<string, float>();
            Dictionary<string, float> selectionPool = new Dictionary<string, float>(pop);
            Random rand = new Random();

            for (int i = 0; i < tournamentSize; i++) {
                string randomKey = selectionPool.ElementAt(rand.Next(0, selectionPool.Count)).Key;
                competitors.Add(randomKey, selectionPool[randomKey]);
                selectionPool.Remove(randomKey);
            }

            KeyValuePair<string, float> winner = GetHighestInduvidual(competitors);
            return winner.Key;
        }

        // Breed a new generation of solutions from the existing population
        // Generate N offspring solutions from a population of N individuals
        // Choose parents with a bias towards those with higher fitness
        // We can do this in a few different ways: here we use tournament selection
        // We can opt to employ 'elitism' which means the current best individual
        // always gets copied into the next generation at least once
        // We can opt to use 'crossover' (uniform or single point) which combines
        // two parent genotypes into one offspring
        static Dictionary<string, float> Breed(Dictionary<string, float> pop, int tournamentSize, float crossover, bool uniform, bool elitism) {

            Random rand = new Random();
            Dictionary<string, float> offspring_pop = new Dictionary<string, float>();

            if (elitism) offspring_pop.Add(GetHighestInduvidual(pop).Key, 0);

            while (offspring_pop.Count < pop.Count) {
                string mum = Tournament(pop, tournamentSize);
                if (rand.NextDouble() < (double) crossover) {
                    string dad = Tournament(pop, tournamentSize);
                    if (uniform) offspring_pop.Add(UniformCross(mum, dad), 0);
                    else offspring_pop.Add(Cross(mum, dad), 0);
                } else {
                    offspring_pop.Add(mum, 0);
                }
            }

            return offspring_pop;
        }

        // Apply mutation to the population of new offspring
        // Each symbol in each solution may be replaced by a randomly chosen symbol from the alphabet
        // For each symbol in each solution the chance of this happening is set by the mutation parameter
        static Dictionary<string, float> Mutate(Dictionary<string, float> pop, float mutation, string alphabet, bool elitism) {
            Random rand = new Random();
            Dictionary<string, float> tempPopCopy = new Dictionary<string, float>(pop);
            Dictionary<string, float> mutatedPop = new Dictionary<string, float>();

            KeyValuePair<string, float> elite = GetHighestInduvidual(tempPopCopy);
            if (elitism) {
                tempPopCopy.Remove(elite.Key);
                mutatedPop.Add(elite.Key, elite.Value);
            }

            foreach(KeyValuePair<string, float> entry in tempPopCopy) {
                string newSolution = "";
                foreach (char c in entry.Key) {
                    if (rand.NextDouble() < mutation) {
                        newSolution += alphabet[rand.Next(alphabet.Length)];
                    } else {
                        newSolution += c;
                    }
                }
                mutatedPop.Add(newSolution, entry.Value);
            }

            return mutatedPop;
        }

        // Crossover the solution string of two parents to make an offspring
        //   (This code implements 'one-point crossover')
        // Pick a random point in the solution string,
        // use the mum's string up to this point and the dad's string after it
        static string Cross(string mum, string dad) {
            Random rand = new Random();
            int point = rand.Next(0, mum.Length);
            return mum.Substring(0, point) + dad.Substring(point);
        }

        // Uniform crossover of two parent solution strings to make an offspring
        // pick each offspring solution symbol from the mum or dad with equal probability
        static string UniformCross(string mum, string dad) {
            Random rand = new Random();
            string returnString = "";
            for(int i = 0; i < mum.Length; i++) {
                if (rand.NextDouble() < 0.5) returnString += mum[i];
                else returnString += dad[i];
            }
            return returnString;
        }

        // Write a line of summary stats for population pop at generation gen
        // if File is "" we write to the standard out, otherwise we write to the File
        // (In addition to writing out the max, min, and mean fitness for the pop, we
        // now write out a measure of population "covergence", i.e., std dev of fitness,
        // and the match() between the best solutionand the median solution in the pop#
        // but that's not implemented here yet.)
        static void WriteFitness(Dictionary<string, float> pop, int gen, string fileName) {

            List<float> fitness = new List<float>();
            foreach (KeyValuePair<string, float> entry in pop) {
                fitness.Add(entry.Value);
            }

            string line = $"{gen}: max:{GetHighestInduvidual(pop).Value}\n";

            if (fileName != "") {
                using StreamWriter file = new StreamWriter(fileName, append: true);
                file.WriteLine(line);
            } else {
                Console.WriteLine(line);
            }
        }

        // The main function for the GA
        // The function takes a number of arguments specifying various parameters and options
        // each argument has a default value which can be overloaded in the function call..
        // Seed the pseudo-random number generator (using the system clock)
        // so no two runs will have the same sequence of pseudo-random numbers
        // Set the length of the solution strings to be the length of the target string
        // Set the mutation rate to be equivalent to "on average 1 mutation per offspring"
        // Initialise a population of individuals
        // Assess each member of the initial population using the fitness function
        // Run a maximum of max_gen generations of evolution
        //     (stopping early if we find the perfect solution)
        // Each generation of evolution comprises:
        // increment the generation counter
        // breed a new population of offspring
        // mutate the new offspring
        // assess each member of the new population using the fitness function and sort pop by fitness
        // track the best (highest fitness) solution in the current population (the 0th item in the list)
        // if we are writing stats and we want to write stats this generation:
        // write out some stats
        // Return the final generation count and the best individual from the final population
        static (int, KeyValuePair<string, float>) DoTheGA(int popSize, int tournamentSize, float crossover, bool uniform, bool elitism, int maxGen, bool converged, int writeEvery, string fileName, string target, float m, string alphabet) {
            Random rand = new Random();

            float length = target.Length;
            float mutation = m / length;

            Dictionary<string, float> pop = Initialise(popSize, (int)length, alphabet, converged);
            pop = Assess(pop, target);

            int generation = 0;
            KeyValuePair<string, float> best = GetHighestInduvidual(pop);
            while (generation < maxGen && best.Value < 1) {
                generation++;
                pop = Breed(pop, tournamentSize, crossover, uniform, elitism);
                pop = Mutate(pop, mutation, alphabet, elitism);
                pop = Assess(pop, target);
                best = GetHighestInduvidual(pop);
                if (writeEvery > 0 && generation % writeEvery == 0) {
                    WriteFitness(pop, generation, fileName);
                }
            }

            return (generation, best);
        }

        static void Main(string[] args) {
            int popSize = 100;
            int tournamentSize = 2;
            float crossover = 1;
            bool uniform = true;
            bool elitism = false;
            int maxGen = 1000;
            bool converged = false;
            int writeEvery = 1;
            string fileName = "results.txt";
            string target = "methinks it is like a weasel";
            float m = 1;
            string alphabet = "abcdefghijklmnopqrstuvwxyz ";

            (int, KeyValuePair<string, float>) results = DoTheGA(popSize, tournamentSize, crossover, uniform, elitism, maxGen, converged, writeEvery, fileName, target, m, alphabet);

            Console.WriteLine($"{results.Item1} generations yielded: '{results.Item2.Key}' ({results.Item2.Value})");




            /*foreach (KeyValuePair<string, float> entry in pop) {
                Console.WriteLine($"{entry.Key} : {entry.Value}");
            }*/

            /*Dictionary<string, float> testPop = Mutate(pop, 0.5f, alphabet, false);
            Console.WriteLine($"{pop.ElementAt(0).Key} : {pop.ElementAt(0).Value}");
            Console.WriteLine($"{testPop.ElementAt(0).Key} : {testPop.ElementAt(0).Value}");*/

            Console.ReadLine();
        }
    }
}
