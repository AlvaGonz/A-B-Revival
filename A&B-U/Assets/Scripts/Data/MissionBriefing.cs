namespace ABRevival.Core.Data
{
    /// <summary>
    /// Port of the frozen MissionData.missionWords array
    /// (src/as3/scripts/f4data/sdata/MissionData.as). The map-loading page prints the
    /// campaign's objective summary before combat; talkBefore and the opening movie play
    /// separately through StoryDirector.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class MissionBriefing
    {
        private static readonly string[] StoryWords =
        {
            "Wang is fighting. \n\nMichael is waiting.",
            "1.Find Officer XA. [DON'T FIGHT!!!]\n 2.Entice him to the cannon's range.\n 3.Get the JUMP fuel.",
            "Protect the ship for 1 minute.",
            "Protect the ship, fight against unknown enemies I.",
            "To find out, who are they I.",
            "Protect the ship, fight against unknown enemies II.",
            "To find out, who are they II.",
            "Wang, protect the ship.",
            "To find out, who are they III.\nYou'd better run.",
            "What is Color Defense? Tom is fighting.",
            "Protect the ship and save Tom.",
            "Protect the ship, fight against unknown enemies III.",
            "Fight! Unknown enemies and old enemies!",
            "Fight! Unknown friends and old enemies!",
            "Fight! AI and old enemies!",
            "Protect the ship, fight against old enemies I.",
            "Protect the ship, fight against old enemies II.",
            "Protect the ship, fight against old enemies III.",
            "Protect the ship, fight against old enemies IV.",
            "Protect the ship, fight against old enemies V.",
            "Protect the ship, fight against old enemies VI.",
            "Protect the ship, fight against old enemies VII.",
            "Protect the ship, fight against old enemies VIII.",
            "Protect the ship, fight against old enemies IX.",
            "Protect the ship, fight against old enemies X.",
            "Protect the ship, fight against new enemies I.",
            "Protect the ship, fight against new enemies II.",
            "Protect the ship, fight against new enemies III.",
            "Protect the ship, fight against new enemies IV.",
            "Protect the ship, fight against new enemies with the monkey I.",
            "Protect the ship, fight against new enemies with the monkey II.",
            "Protect the ship, fight against new enemies with the monkey III.",
            "Protect the ship, fight against new enemies with the monkey IV.",
            "Protect the ship, fight against new enemies with the monkey V.",
            "Protect the ship, as you can."
        };

        /// <summary>Frozen objective text for a STORY index, or an empty string when none exists.</summary>
        public static string Words(int storyIndex)
        {
            if (storyIndex >= 0 && storyIndex < StoryWords.Length) return StoryWords[storyIndex];

            return storyIndex switch
            {
                49 => "Well, life is not that bad. But it should be better.",
                99 => "BATTLES Never end...",
                _ => string.Empty
            };
        }
    }
}
