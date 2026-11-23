using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastGrounds
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
        public float time;
    }

    /// <summary>
    /// Top-10 table per game mode, saved in PlayerPrefs as JSON.
    /// Replaces GameMaker's single global highscore_add / draw_highscore table.
    /// </summary>
    public static class Leaderboard
    {
        public const int Capacity = 10;

        [Serializable]
        class Table
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }

        static string Key(string modeId)
        {
            return "leaderboard." + modeId;
        }

        public static List<LeaderboardEntry> Load(string modeId)
        {
            string json = PlayerPrefs.GetString(Key(modeId), string.Empty);
            if (string.IsNullOrEmpty(json)) return new List<LeaderboardEntry>();
            try
            {
                Table table = JsonUtility.FromJson<Table>(json);
                return table != null && table.entries != null ? table.entries : new List<LeaderboardEntry>();
            }
            catch (Exception)
            {
                return new List<LeaderboardEntry>();
            }
        }

        /// <summary>Adds an entry and returns its rank (0-based), or -1 if it did not make the table.</summary>
        public static int Add(string modeId, string playerName, int score, float time)
        {
            List<LeaderboardEntry> entries = Load(modeId);
            LeaderboardEntry entry = new LeaderboardEntry { name = playerName, score = score, time = time };
            entries.Add(entry);
            // Higher score first; on a tie, whoever survived longer.
            entries.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : b.time.CompareTo(a.time));
            int rank = entries.IndexOf(entry);
            if (entries.Count > Capacity) entries.RemoveRange(Capacity, entries.Count - Capacity);
            Save(modeId, entries);
            return rank < Capacity ? rank : -1;
        }

        public static void Clear(string modeId)
        {
            PlayerPrefs.DeleteKey(Key(modeId));
            PlayerPrefs.Save();
        }

        static void Save(string modeId, List<LeaderboardEntry> entries)
        {
            Table table = new Table { entries = entries };
            PlayerPrefs.SetString(Key(modeId), JsonUtility.ToJson(table));
            PlayerPrefs.Save();
        }
    }
}
