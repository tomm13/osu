﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    public class Colour : Skill
    {
        private const int mono_history_max_length = 5;

        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 0.4;

        private HitType? previousHitType;

        /// <summary>
        /// Length of the current mono pattern.
        /// </summary>
        private int currentMonoLength = 1;

        /// <summary>
        /// List of the last <see cref="mono_history_max_length"/> most recent mono patterns, with the most recent at the end of the list.
        /// </summary>
        private readonly List<int> monoHistory = new List<int>();

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (!(current.LastObject is Hit && current.BaseObject is Hit && current.DeltaTime < 1000))
            {
                previousHitType = null;
                return 0.0;
            }

            var taikoCurrent = (TaikoDifficultyHitObject)current;

            double objectStrain = 0.0;

            if (taikoCurrent.HitType != null && previousHitType != null && taikoCurrent.HitType != previousHitType)
            {
                // The colour has changed.
                objectStrain = 1.0;

                if (monoHistory.Count < 2)
                {
                    // There needs to be at least two streaks to determine a strain.
                    objectStrain = 0.0;
                }
                else if ((monoHistory[^1] + currentMonoLength) % 2 == 0)
                {
                    // The last streak in the history is guaranteed to be a different type to the current streak.
                    // If the total number of notes in the two streaks is even, apply a penalty.
                    objectStrain *= sameParityPenalty();
                }

                objectStrain *= repetitionPenalties();
                currentMonoLength = 1;
            }
            else
            {
                currentMonoLength += 1;
            }

            previousHitType = taikoCurrent.HitType;
            return objectStrain;
        }

        /// <summary>
        /// The penalty to apply when the total number of notes in the two most recent colour streaks is even.
        /// </summary>
        private double sameParityPenalty() => 0.0;

        /// <summary>
        /// The penalty to apply due to the length of repetition in colour streaks.
        /// </summary>
        private double repetitionPenalties()
        {
            double penalty = 1.0;

            monoHistory.Add(currentMonoLength);

            if (monoHistory.Count > mono_history_max_length)
                monoHistory.RemoveAt(0);

            for (int l = 2; l <= mono_history_max_length / 2; l++)
            {
                for (int start = monoHistory.Count - l - 1; start >= 0; start--)
                {
                    bool samePattern = true;

                    for (int i = 0; i < l; i++)
                    {
                        if (monoHistory[start + i] != monoHistory[monoHistory.Count - l + i])
                        {
                            samePattern = false;
                        }
                    }

                    if (samePattern) // Repetition found!
                    {
                        int notesSince = 0;
                        for (int i = start; i < monoHistory.Count; i++) notesSince += monoHistory[i];
                        penalty *= repetitionPenalty(notesSince);
                        break;
                    }
                }
            }

            return penalty;
        }

        private double repetitionPenalty(int notesSince) => Math.Min(1.0, 0.032 * notesSince);
    }
}
