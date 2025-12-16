using System;
using UnityEngine;



/**
 * 
 * DNA stores the Gene and other data for any GA Solution
 * Note: the <T> notation is for Generic methods - see https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generic-methods 
 * It is similar to C++ Templates
 * In this Project, the data type for the DNA is <int> , as that is the data used within the Genes
 */
public class DNA<T>
{
    // the Gene data - here an array of integers
    public T[] Genes { get; private set; }

    // the Fitness for this Solution
    public float Fitness { get; private set; }

    // Functions for the various methods
    private System.Random random;
    private Func<T> getRandomGene;
    private Func<int, float> fitnessFunction;


    public DNA(int size, System.Random random, Func<T> getRandomGene, Func<int, float> fitnessFunction, bool shouldInitGenes = true)
    {
        Genes = new T[size];
        this.random = random;
        this.getRandomGene = getRandomGene;
        this.fitnessFunction = fitnessFunction;

        // if we need to create new Gene data, use the random funciton to pick random values
        if (shouldInitGenes)
        {
            for (int i = 0; i < Genes.Length; i++)
            {
                Genes[i] = getRandomGene();
            }
        }
    }

    /**
     * CalculateFitness will call the stored Fitness Function and set the Fitness value with the value returned
     */
    public float CalculateFitness(int index)
    {

        Fitness = fitnessFunction(index);

        return Fitness;
    }



    /**
	 * Crossover function for the Genetic Algorihm
	 * Currently, this will select random data from each parent
	 */
    public DNA<T> Crossover(DNA<T> otherParent)
    {
        // create a new instance of DNA that will be the child's DNA
        DNA<T> child = new DNA<T>(Genes.Length, random, getRandomGene, fitnessFunction, shouldInitGenes: false);


        // Single-point crossover: select a random crossover point
        // Child inherits genes from parent1 up to crossover point, then from parent2
        int crossoverPoint = random.Next(Genes.Length);

        for (int i = 0; i < Genes.Length; i++)
        {
            if (i < crossoverPoint)
            {
                child.Genes[i] = Genes[i];  // From first parent
            }
            else
            {
                child.Genes[i] = otherParent.Genes[i];  // From second parent
            }
        }






        // return the child DNA
        return child;
    }


    /**
	 * Mutate will apply mutation at the rate given by mutationRate
	 * 
	 */
    public void Mutate(float mutationRate)
    {



        // For each gene, check if it should mutate based on mutation rate
        for (int i = 0; i < Genes.Length; i++)
        {
            if (random.NextDouble() < mutationRate)
            {
                // Mutate this gene by replacing it with a random value
                Genes[i] = getRandomGene();
            }
        }





    }
}