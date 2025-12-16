using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;



namespace Assets.Scripts
{
    /**
     * DataLogger - Enhanced version with asteroid kill tracking per ship
     * Tracks which ships destroyed asteroids and their shooting efficiency
     */
    public class DataLogger
    {
        private string logFilePath;
        private bool isLoggingEnabled = true;
        private StreamWriter writer;

        // Statistics tracking
        private List<float> averageCollisionsHistory = new List<float>();
        private List<float> bestFitnessHistory = new List<float>();
        private List<float> worstFitnessHistory = new List<float>();

        // Enhanced shooting statistics tracking
        private List<int> totalAsteroidsDestroyedHistory = new List<int>();
        private List<float> averageAccuracyHistory = new List<float>();

        /**
         * Constructor - Initialize the logger with a file path
         */
        public DataLogger(string fileName = "GA_Log.txt")
        {
            logFilePath = Path.Combine(Application.persistentDataPath, fileName);
            Debug.Log("Log file will be saved to: " + logFilePath);
        }

        /**
         * StartNewLog - Creates a new log file and writes the header
         */
        public void StartNewLog(int populationSize, float mutationRate, int maxGenerations, int asteroidCount, int maxAmmo)
        {
            try
            {
                writer = new StreamWriter(logFilePath, false);

                writer.WriteLine("=================================================================");
                writer.WriteLine("GENETIC ALGORITHM - ASTEROID AVOIDANCE & SHOOTING SIMULATION LOG");
                writer.WriteLine("=================================================================");
                writer.WriteLine("Log Started: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine();
                writer.WriteLine("CONFIGURATION:");
                writer.WriteLine("  Population Size: " + populationSize);
                writer.WriteLine("  Mutation Rate: " + mutationRate);
                writer.WriteLine("  Max Generations: " + maxGenerations);
                writer.WriteLine("  Asteroids per Level: " + asteroidCount);
                writer.WriteLine("  Ammo per Ship: " + maxAmmo);
                writer.WriteLine();
                writer.WriteLine("=================================================================");
                writer.WriteLine();

                // Write column headers with shooting stats
                writer.WriteLine(string.Format("{0,-12} {1,-15} {2,-15} {3,-15} {4,-15} {5,-15} {6,-15} {7,-15}",
                    "Generation", "Best Fitness", "Worst Fitness", "Avg Fitness", "Median Fit", "Std Dev", "Kills", "Avg Acc%"));
                writer.WriteLine(new string('-', 125));

                writer.Flush();

                Debug.Log("Data logging started successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to create log file: " + e.Message);
                isLoggingEnabled = false;
            }
        }

        /**
         * LogGeneration - Enhanced version with shooting statistics
         */
        public void LogGeneration(int generation, List<DNA<int>> population, float averageCollisions, List<Ship> ships)
        {
            if (!isLoggingEnabled || writer == null)
                return;

            try
            {
                // Calculate fitness statistics
                float bestFitness = float.MaxValue;
                float worstFitness = float.MinValue;
                float totalFitness = 0;
                List<float> allFitness = new List<float>();

                foreach (DNA<int> dna in population)
                {
                    float fitness = dna.Fitness;
                    allFitness.Add(fitness);
                    totalFitness += fitness;

                    if (fitness < bestFitness)
                        bestFitness = fitness;
                    if (fitness > worstFitness)
                        worstFitness = fitness;
                }

                float avgFitness = totalFitness / population.Count;
                float medianFitness = CalculateMedian(allFitness);
                float stdDeviation = CalculateStandardDeviation(allFitness, avgFitness);

                // NEW: Calculate shooting statistics
                int totalKills = 0;
                int totalShots = 0;
                foreach (Ship ship in ships)
                {
                    totalKills += ship.GetAsteroidsDestroyed();
                    totalShots += ship.GetShotsFired();
                }
                float avgAccuracy = totalShots > 0 ? (totalKills * 100f / totalShots) : 0;

                // Store for historical analysis
                averageCollisionsHistory.Add(averageCollisions);
                bestFitnessHistory.Add(bestFitness);
                worstFitnessHistory.Add(worstFitness);
                totalAsteroidsDestroyedHistory.Add(totalKills);
                averageAccuracyHistory.Add(avgAccuracy);

                // Write generation summary with shooting stats
                writer.WriteLine(string.Format("{0,-12} {1,-15:F2} {2,-15:F2} {3,-15:F2} {4,-15:F2} {5,-15:F2} {6,-15} {7,-15:F1}",
                    generation, bestFitness, worstFitness, avgFitness, medianFitness, stdDeviation, totalKills, avgAccuracy));

                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to log generation data: " + e.Message);
            }
        }

        /**
         * LogDetailedGeneration - Enhanced with per-ship shooting statistics
         */
        public void LogDetailedGeneration(int generation, List<DNA<int>> population, float averageCollisions,
                                          int asteroidCount, float elapsedTime, List<Ship> ships)
        {
            if (!isLoggingEnabled || writer == null)
                return;

            try
            {
                writer.WriteLine();
                writer.WriteLine("=================================================================");
                writer.WriteLine("--- GENERATION " + generation + " DETAILED REPORT ---");
                writer.WriteLine("=================================================================");
                writer.WriteLine("Timestamp: " + DateTime.Now.ToString("HH:mm:ss"));
                writer.WriteLine("Elapsed Time: " + elapsedTime.ToString("F2") + " seconds");
                writer.WriteLine();

                // Calculate fitness statistics
                float bestFitness = float.MaxValue;
                float worstFitness = float.MinValue;
                float totalFitness = 0;
                List<float> allFitness = new List<float>();

                foreach (DNA<int> dna in population)
                {
                    float fitness = dna.Fitness;
                    allFitness.Add(fitness);
                    totalFitness += fitness;

                    if (fitness < bestFitness)
                        bestFitness = fitness;
                    if (fitness > worstFitness)
                        worstFitness = fitness;
                }

                float avgFitness = totalFitness / population.Count;

                // Write basic statistics
                writer.WriteLine("PERFORMANCE METRICS:");
                writer.WriteLine("  Best Fitness: " + bestFitness.ToString("F2"));
                writer.WriteLine("  Worst Fitness: " + worstFitness.ToString("F2"));
                writer.WriteLine("  Average Fitness: " + avgFitness.ToString("F2"));
                writer.WriteLine("  Average Collisions: " + averageCollisions.ToString("F2"));
                writer.WriteLine("  Total Asteroids: " + asteroidCount);
                writer.WriteLine();

                // Calculate and display shooting statistics
                int totalKills = 0;
                int totalShots = 0;
                int totalAmmoUsed = 0;
                int shipsWithKills = 0;

                foreach (Ship ship in ships)
                {
                    totalKills += ship.GetAsteroidsDestroyed();
                    totalShots += ship.GetShotsFired();
                    totalAmmoUsed += (ship.GetMaxAmmo() - ship.GetCurrentAmmo());
                    if (ship.GetAsteroidsDestroyed() > 0)
                        shipsWithKills++;
                }

                float avgAccuracy = totalShots > 0 ? (totalKills * 100f / totalShots) : 0;
                float avgKillsPerShip = totalKills / (float)ships.Count;

                writer.WriteLine("SHOOTING STATISTICS:");
                writer.WriteLine("  Total Asteroids Destroyed: " + totalKills);
                writer.WriteLine("  Total Shots Fired: " + totalShots);
                writer.WriteLine("  Overall Accuracy: " + avgAccuracy.ToString("F1") + "%");
                writer.WriteLine("  Average Kills per Ship: " + avgKillsPerShip.ToString("F2"));
                writer.WriteLine("  Ships with Kills: " + shipsWithKills + "/" + ships.Count);
                writer.WriteLine("  Total Ammo Expended: " + totalAmmoUsed + "/" + (ships.Count * ships[0].GetMaxAmmo()));
                writer.WriteLine();

                // Calculate improvement
                if (generation > 1 && bestFitnessHistory.Count > 1)
                {
                    float improvement = bestFitnessHistory[bestFitnessHistory.Count - 2] - bestFitness;
                    float improvementPercent = (improvement / bestFitnessHistory[bestFitnessHistory.Count - 2]) * 100;
                    writer.WriteLine("IMPROVEMENT:");
                    writer.WriteLine("  Fitness Improvement: " + improvement.ToString("F2") +
                                   " (" + improvementPercent.ToString("F1") + "%)");

                    if (totalAsteroidsDestroyedHistory.Count > 1)
                    {
                        int killsImprovement = totalKills - totalAsteroidsDestroyedHistory[totalAsteroidsDestroyedHistory.Count - 2];
                        writer.WriteLine("  Kills Improvement: " + killsImprovement);
                    }
                    writer.WriteLine();
                }

                // Top 5 shooters with detailed stats
                writer.WriteLine("TOP 5 SHOOTERS:");
                List<Ship> sortedByKills = ships.OrderByDescending(s => s.GetAsteroidsDestroyed()).ToList();
                for (int i = 0; i < Mathf.Min(5, sortedByKills.Count); i++)
                {
                    Ship ship = sortedByKills[i];
                    float accuracy = ship.GetShotsFired() > 0 ? (ship.GetAsteroidsDestroyed() * 100f / ship.GetShotsFired()) : 0;
                    writer.WriteLine(string.Format("  #{0}: {1} kills, {2} shots, {3:F1}% accuracy, {4} collisions",
                        i + 1,
                        ship.GetAsteroidsDestroyed(),
                        ship.GetShotsFired(),
                        accuracy,
                        ship.GetCollisions()));
                }
                writer.WriteLine();

                // Top 5 performers by fitness
                writer.WriteLine("TOP 5 BY FITNESS:");
                for (int i = 0; i < Mathf.Min(5, population.Count); i++)
                {
                    Ship ship = ships[i];
                    writer.WriteLine(string.Format("  #{0}: Fitness {1:F2}, {2} collisions, {3} kills, {4} shots",
                        i + 1,
                        population[i].Fitness,
                        ship.GetCollisions(),
                        ship.GetAsteroidsDestroyed(),
                        ship.GetShotsFired()));
                }
                writer.WriteLine();

                // Distribution analysis
                writer.WriteLine("PERFORMANCE DISTRIBUTION:");
                int zeroColl = ships.Count(s => s.GetCollisions() == 0);
                int lowColl = ships.Count(s => s.GetCollisions() >= 1 && s.GetCollisions() <= 5);
                int medColl = ships.Count(s => s.GetCollisions() >= 6 && s.GetCollisions() <= 15);
                int highColl = ships.Count(s => s.GetCollisions() > 15);

                writer.WriteLine("  Perfect (0 collisions): " + zeroColl);
                writer.WriteLine("  Good (1-5 collisions): " + lowColl);
                writer.WriteLine("  Medium (6-15 collisions): " + medColl);
                writer.WriteLine("  Poor (15+ collisions): " + highColl);
                writer.WriteLine();

                writer.WriteLine(new string('=', 65));
                writer.WriteLine();

                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to log detailed generation data: " + e.Message);
            }
        }

        /**
         * LogFinalSummary - Enhanced with shooting statistics across all generations
         */
        public void LogFinalSummary(int totalGenerations, float bestOverallFitness, int[] bestGenes, List<Ship> finalShips)
        {
            if (!isLoggingEnabled || writer == null)
                return;

            try
            {
                writer.WriteLine();
                writer.WriteLine("=================================================================");
                writer.WriteLine("FINAL SUMMARY");
                writer.WriteLine("=================================================================");
                writer.WriteLine("Completed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine("Total Generations: " + totalGenerations);
                writer.WriteLine();

                writer.WriteLine("BEST OVERALL PERFORMANCE:");
                writer.WriteLine("  Best Fitness Achieved: " + bestOverallFitness.ToString("F2"));

                if (bestFitnessHistory.Count > 0)
                {
                    float initialBest = bestFitnessHistory[0];
                    float finalBest = bestFitnessHistory[bestFitnessHistory.Count - 1];
                    float totalImprovement = initialBest - finalBest;
                    float improvementPercent = (totalImprovement / initialBest) * 100;

                    writer.WriteLine("  Initial Best Fitness: " + initialBest.ToString("F2"));
                    writer.WriteLine("  Final Best Fitness: " + finalBest.ToString("F2"));
                    writer.WriteLine("  Total Improvement: " + totalImprovement.ToString("F2") +
                                   " (" + improvementPercent.ToString("F1") + "%)");
                }
                writer.WriteLine();

                // Shooting evolution statistics
                writer.WriteLine("SHOOTING EVOLUTION:");
                if (totalAsteroidsDestroyedHistory.Count > 0)
                {
                    int initialKills = totalAsteroidsDestroyedHistory[0];
                    int finalKills = totalAsteroidsDestroyedHistory[totalAsteroidsDestroyedHistory.Count - 1];
                    int maxKills = totalAsteroidsDestroyedHistory.Max();
                    float avgKills = (float)totalAsteroidsDestroyedHistory.Average();

                    writer.WriteLine("  First Generation Kills: " + initialKills);
                    writer.WriteLine("  Final Generation Kills: " + finalKills);
                    writer.WriteLine("  Best Generation Kills: " + maxKills);
                    writer.WriteLine("  Average Kills per Gen: " + avgKills.ToString("F1"));
                    writer.WriteLine("  Kill Improvement: " + (finalKills - initialKills));
                }

                if (averageAccuracyHistory.Count > 0)
                {
                    float initialAcc = averageAccuracyHistory[0];
                    float finalAcc = averageAccuracyHistory[averageAccuracyHistory.Count - 1];
                    float maxAcc = averageAccuracyHistory.Max();
                    float avgAcc = averageAccuracyHistory.Average();

                    writer.WriteLine();
                    writer.WriteLine("  First Generation Accuracy: " + initialAcc.ToString("F1") + "%");
                    writer.WriteLine("  Final Generation Accuracy: " + finalAcc.ToString("F1") + "%");
                    writer.WriteLine("  Best Generation Accuracy: " + maxAcc.ToString("F1") + "%");
                    writer.WriteLine("  Average Accuracy: " + avgAcc.ToString("F1") + "%");
                    writer.WriteLine("  Accuracy Improvement: " + (finalAcc - initialAcc).ToString("F1") + "%");
                }
                writer.WriteLine();

                // Best ship analysis from final generation
                if (finalShips != null && finalShips.Count > 0)
                {
                    Ship bestShip = finalShips.OrderBy(s => s.GetCollisions()).First();
                    Ship bestShooter = finalShips.OrderByDescending(s => s.GetAsteroidsDestroyed()).First();

                    writer.WriteLine("FINAL GENERATION CHAMPIONS:");
                    writer.WriteLine();
                    writer.WriteLine("Best Overall Ship:");
                    writer.WriteLine("  Collisions: " + bestShip.GetCollisions());
                    writer.WriteLine("  Kills: " + bestShip.GetAsteroidsDestroyed());
                    writer.WriteLine("  Shots: " + bestShip.GetShotsFired());
                    writer.WriteLine("  Accuracy: " + (bestShip.GetShotsFired() > 0 ?
                        (bestShip.GetAsteroidsDestroyed() * 100f / bestShip.GetShotsFired()).ToString("F1") : "0") + "%");
                    writer.WriteLine("  Distance Traveled: " + bestShip.GetDistanceTraveled().ToString("F1"));
                    writer.WriteLine();

                    writer.WriteLine("Best Shooter:");
                    writer.WriteLine("  Kills: " + bestShooter.GetAsteroidsDestroyed());
                    writer.WriteLine("  Shots: " + bestShooter.GetShotsFired());
                    writer.WriteLine("  Accuracy: " + (bestShooter.GetShotsFired() > 0 ?
                        (bestShooter.GetAsteroidsDestroyed() * 100f / bestShooter.GetShotsFired()).ToString("F1") : "0") + "%");
                    writer.WriteLine("  Collisions: " + bestShooter.GetCollisions());
                    writer.WriteLine();
                }

                writer.WriteLine("PERFORMANCE TRENDS:");
                if (averageCollisionsHistory.Count > 0)
                {
                    float avgFirst5 = CalculateAverage(averageCollisionsHistory, 0, Mathf.Min(5, averageCollisionsHistory.Count));
                    float avgLast5 = CalculateAverage(averageCollisionsHistory,
                        Mathf.Max(0, averageCollisionsHistory.Count - 5), averageCollisionsHistory.Count);

                    writer.WriteLine("  Avg Collisions (First 5 Gens): " + avgFirst5.ToString("F2"));
                    writer.WriteLine("  Avg Collisions (Last 5 Gens): " + avgLast5.ToString("F2"));
                    writer.WriteLine("  Improvement: " + (avgFirst5 - avgLast5).ToString("F2"));
                }

                writer.WriteLine();
                writer.WriteLine("=================================================================");
                writer.WriteLine();

                writer.Flush();

                Debug.Log("Final summary written to log file: " + logFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to write final summary: " + e.Message);
            }
        }

        /**
         * Close  Closes the log file
         */
        public void Close()
        {
            if (writer != null)
            {
                try
                {
                    writer.WriteLine();
                    writer.WriteLine("Log file closed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.Close();
                    writer = null;
                    Debug.Log("Log file closed successfully.");
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to close log file: " + e.Message);
                }
            }
        }

        /**
         * GetLogFilePath - Returns the full path to the log file
         */
        public string GetLogFilePath()
        {
            return logFilePath;
        }


        private float CalculateMedian(List<float> values)
        {
            if (values.Count == 0)
                return 0;

            List<float> sorted = new List<float>(values);
            sorted.Sort();

            int mid = sorted.Count / 2;
            if (sorted.Count % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2.0f;
            else
                return sorted[mid];
        }

        private float CalculateStandardDeviation(List<float> values, float mean)
        {
            if (values.Count == 0)
                return 0;

            float sumSquaredDiff = 0;
            foreach (float value in values)
            {
                float diff = value - mean;
                sumSquaredDiff += diff * diff;
            }

            return Mathf.Sqrt(sumSquaredDiff / values.Count);
        }

        private float CalculateAverage(List<float> values, int startIndex, int endIndex)
        {
            if (startIndex >= endIndex || values.Count == 0)
                return 0;

            float sum = 0;
            int count = 0;

            for (int i = startIndex; i < endIndex && i < values.Count; i++)
            {
                sum += values[i];
                count++;
            }

            return count > 0 ? sum / count : 0;
        }
    }
}