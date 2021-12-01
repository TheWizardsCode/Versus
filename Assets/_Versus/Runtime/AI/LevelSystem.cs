using System;

namespace WizardsCode.Versus.AI
{
    public class LevelSystem
    {
        public event EventHandler OnExperienceChanged;
        public event EventHandler OnLevelChanged;

        private long m_CurrentExperience;
        private int m_CurrentLevel;

        // TODO maybe this should be set somewhere else or configurable
        private const int MaxLevel = 100;

        public int GetMaxLevel()
        {
            return MaxLevel;
        }
        
        /// <summary>
        /// Calculate the experience needed for the next level
        /// </summary>
        /// <remarks>
        /// Could use an AnimationCurve instead so each faction could have independent growth set visually
        /// </remarks>
        /// <param name="level"></param>
        /// <returns></returns>
        public long GetExperienceNeeded(int level)
        {
            // This is a gentle increase of xp for each level which is almost the opposite of AD&D
            var xp = 0.04f * Math.Pow(level + 1, 3) + 0.8f * Math.Pow(level + 1, 2) + 2 * level + 1;
            return (long) xp;
        }
        
        public void AddExperience(long amount)
        {
            m_CurrentExperience += amount;
            if (OnExperienceChanged != null)
            {
                OnExperienceChanged(this, EventArgs.Empty);
            }
            var experienceNeeded = GetExperienceNeeded(m_CurrentLevel);
            if (m_CurrentExperience > experienceNeeded)
            {
                m_CurrentLevel++;
                // TODO add some cool message that the animal reached a new level
                if (OnLevelChanged != null)
                {
                    OnLevelChanged(this, EventArgs.Empty);
                }
            }
        }
        
        public int GetLevel()
        {
            return m_CurrentLevel;
        }

        public long GetExperience()
        {
            return m_CurrentExperience;
        }
    }
}