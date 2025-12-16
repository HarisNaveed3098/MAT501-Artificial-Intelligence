using System;
using System.Collections.Generic;
using System.Linq;


/**
 * GeneticAglorithm manages the population and core steps of the GA
 *  * Note: the <T> notation is for Generic methods - see https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generic-methods 
 * It is similar to C++ Templates
 * In this Project, the data type for the DNA is <int> , as that is the data used within the Genes
 * 
 */
public class GeneticAglorithm<T>
{

    // the popualation for the GA
    public List<DNA<T>> Population { get; private set; }

    // tracking variables
    public int Generation { get; private set; }
    public float BestFitness { get; private set; }
    public T[] BestGenes { get; private set; }


    // implementation variables
    public float MutationRate;


    // Random access 
    private System.Random random;

    /**
     * Create the GeneticAglorithm based on the provided values
     * 
     */
    public GeneticAglorithm(int populationSize, int dnaSize, System.Random random, Func<T> getRandomGene, Func<int, float> fitnessFunction, float mutationRate = 0.01f)
    {
        Generation = 1;
        MutationRate = mutationRate;
        //create the new population list with the provided data type
        Population = new List<DNA<T>>();
        this.random = random;

        BestGenes = new T[dnaSize];

        // create teh starting random population
        for (int i = 0; i < populationSize; i++)
        {
            Population.Add(new DNA<T>(dnaSize, random, getRandomGene, fitnessFunction, shouldInitGenes: true));
        }
    }


    /**
     * NewGeneration will creata the DNA for the next generation for the required population count
     * Should be called when the Simulation step is complete
     */
    public void NewGeneration()
    {
        if (Population.Count <= 0)
        {
            return;
        }

        // Evaluate - calculate the fitness of each solution based on their performance in the Simulation
        CalculateAllFitness();


        // crate the new generation
        List<DNA<T>> newPopulation = new List<DNA<T>>();

        for (int i = 0; i < Population.Count; i++)
        {
            // Selection
            DNA<T> parent1 = ChooseParent();
            DNA<T> parent2 = ChooseParent();

            // Crossover
            DNA<T> child = parent1.Crossover(parent2);

            // Mutation
            child.Mutate(MutationRate);

            newPopulation.Add(child);
        }

        // set the new population
        Population = newPopulation;

        Generation++;
    }


    /**
     * CalculateAllFitness will calcualte the fitness for each solution and re-order the Population by ascending Fitness
     */
    public void CalculateAllFitness()
    {
        // track the best solution
        DNA<T> best = Population[0];

        for (int i = 0; i < Population.Count; i++)
        {
            Population[i].CalculateFitness(i);

            if (Population[i].Fitness < best.Fitness)
            {
                best = Population[i];
            }
        }

        BestFitness = best.Fitness;
        best.Genes.CopyTo(BestGenes, 0);

        // order by Fitness so it is easy to select the fittest
        Population = Population.OrderBy(ch => ch.Fitness).ToList();
    }



    /**
     * ChooseParent should return the DNA of a parent to be used in Crossover (Reproduction)
     */

    private DNA<T> ChooseParent()
    {

        int halfPopulation = Population.Count / 2;
        int randomIndex = random.Next(halfPopulation);

        return Population[randomIndex];


    }
}