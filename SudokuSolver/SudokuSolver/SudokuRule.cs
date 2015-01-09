using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BruteForceSudokuSolver
{
    public class SudokuRule : IEnumerable<SudokuTile>
    {
        internal SudokuRule(IEnumerable<SudokuTile> tiles, string description)
        {
            _tiles = new HashSet<SudokuTile>(tiles);
            _description = description;
        }

        private readonly ISet<SudokuTile> _tiles;
        private readonly string _description;

        public bool CheckValid()
        {
            return _tiles
                .Where(tile => tile.HasValue)
                .GroupBy(tile => tile.Value)
                .All(group => group.Count() == 1);
        }

        public bool CheckComplete()
        {
            return _tiles.All(tile => tile.HasValue) && CheckValid();
        }

        internal SudokuProgress RemovePossibles()
        {
            // Tiles that has a number already
            IEnumerable<SudokuTile> withNumber = _tiles.Where(tile => tile.HasValue);

            // Tiles without a number
            IEnumerable<SudokuTile> withoutNumber = _tiles.Where(tile => !tile.HasValue);

            // The existing numbers in this rule
            IEnumerable<int> existingNumbers = new HashSet<int>(withNumber
                                                                    .Select(tile => tile.Value)
                                                                    .Distinct()
                                                                    .ToList());
            return withoutNumber.Aggregate(
                SudokuProgress.NoProgress,
                (result, tile) => SudokuTile.CombineSolvedState(result, tile.RemovePossibles(existingNumbers)));
        }

        internal SudokuProgress CheckForOnlyOnePossibility()
        {
            // Check if there is only one number within this rule that can have a specific value
            IList<int> existingNumbers = _tiles
                .Select(tile => tile.Value)
                .Distinct()
                .ToList();
            SudokuProgress result = SudokuProgress.NoProgress;

            foreach (int value in Enumerable.Range(1, _tiles.Count))
            {
                if (existingNumbers.Contains(value)) // this rule already has the value, skip checking for it
                    continue;
                var possibles = _tiles
                    .Where(tile => !tile.HasValue && tile.IsValuePossible(value))
                    .ToList();
                if (possibles.Count == 0)
                    return SudokuProgress.Failed;
                if (possibles.Count == 1)
                {
                    possibles.First().Fix(value, "Only possible in rule " + ToString());
                    result = SudokuProgress.Progress;
                }
            }
            return result;
        }

        internal SudokuProgress Solve()
        {
            // If both are null, return null (indicating no change). If one is null, return the other. Else return result1 && result2
            SudokuProgress result1 = RemovePossibles();
            SudokuProgress result2 = CheckForOnlyOnePossibility();
            return SudokuTile.CombineSolvedState(result1, result2);
        }

        public override string ToString()
        {
            return _description;
        }

        public IEnumerator<SudokuTile> GetEnumerator()
        {
            return _tiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Description
        {
            get { return _description; }
        }
    }
}
