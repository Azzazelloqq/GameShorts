using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;


namespace Code.Games
{
    internal static class WeightedPickerUnity
    {
        private static readonly Dictionary<int, int> _weights = new()
        {
            {  2, 108 }, {  4, 32 }, {  8,  4 }, { 16,  2 }, { 32,  1 }, { 64,  1 },
        };

        internal struct Table
        {
            public int[] values;
            public int[] cum;
            public int total;
            public bool IsValid => values != null && cum != null && total > 0;
        }

        internal static Table BuildTable(IEnumerable<int> values)
        {
            var vals = new List<int>();
            var cum  = new List<int>();
            int total = 0;

            foreach (var v in values)
            {
                if (_weights.TryGetValue(v, out var w) && w > 0)
                {
                    total += w;
                    vals.Add(v);
                    cum.Add(total);
                }
            }
            if (total <= 0) throw new System.InvalidOperationException("Нет допустимых значений.");
            return new Table { values = vals.ToArray(), cum = cum.ToArray(), total = total };
        }

        internal static int Pick(ref Table table)
        {
            if (!table.IsValid) throw new System.ArgumentException("Таблица невалидна.", nameof(table));

            int r = Random.Range(0, table.total); // [0, total)
            int idx = System.Array.BinarySearch(table.cum, r);
            if (idx < 0) idx = ~idx;
            return table.values[idx];
        }
    }
}