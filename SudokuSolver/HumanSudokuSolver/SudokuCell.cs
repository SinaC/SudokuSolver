using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NaturalSudokuSolver
{
    public class SudokuCell
    {
        private const int Cleared = 0;

        private readonly int _maxValue;
        private readonly int _x;
        private readonly int _y;

        private int _value;
        private ISet<int> _candidates; // [1, MaxValue]

        public SudokuCell(int x, int y, int maxValue)
        {
            _x = x;
            _y = y;
            _maxValue = maxValue;
            _candidates = new HashSet<int>();
            _value = Cleared;
        }

        public int Value
        {
            get { return _value; }
            set
            {
                if (value > _maxValue)
                    throw new ArgumentOutOfRangeException("SudokuCell Value cannot be greater than " + _maxValue + ". Was " + value);
                if (value < Cleared)
                    throw new ArgumentOutOfRangeException("SudokuCell Value cannot be zero or smaller. Was " + value);
                _value = value;
            }
        }

        public bool HasValue
        {
            get { return Value != Cleared; }
        }

        public bool IsCandidate(int i)
        {
            return _candidates.Contains(i);
        }

        public bool HasCommonCandidate(IEnumerable<int> candidates)
        {
            return _candidates.Any(candidates.Contains);
        }

        public bool HasCommonCandidate(SudokuCell cell)
        {
            return HasCommonCandidate(cell.Candidates);
        }

        public int X
        {
            get { return _x; }
        }

        public int Y
        {
            get { return _y; }
        }

        public int CandidateCount
        {
            get { return _candidates.Count; }
        }

        public ISet<int> Candidates
        {
            get { return _candidates; }
        }

        public void ResetCandidates()
        {
            _candidates.Clear();
            foreach (int i in Enumerable.Range(1, _maxValue))
            {
                if (!HasValue || Value == i)
                    _candidates.Add(i);
            }
        }

        public void Fix(int value, string reason)
        {
            System.Diagnostics.Debug.WriteLine("Fixing {0} on Cell[{1},{2}]: {3}", value, _x, _y, reason);
            Value = value;
            ResetCandidates();
        }

        public SudokuProgress RemoveCandidate(int candidate, string reason)
        {
            System.Diagnostics.Debug.WriteLine("Removing {0} from Cell[{1},{2}]: {3}", candidate, X, Y, reason);

            _candidates.Remove(candidate);

            SudokuProgress result = SudokuProgress.NoProgress;
            if (_candidates.Count == 1) // one candidate value
            {
                Fix(_candidates.First(), "Only one possibility");
                result = SudokuProgress.Progress;
            }
            else if (_candidates.Count == 0) // no candidate value
                return SudokuProgress.Failed;
            return result;
        }

        public SudokuProgress RemoveCandidates(IEnumerable<int> candidates, string reason)
        {
            System.Diagnostics.Debug.WriteLine("Removing {0} from Cell[{1},{2}]: {3}", candidates.Select(x => x.ToString(CultureInfo.InvariantCulture)).Aggregate((s, s1) => s + "," + s1), X, Y, reason);

            // Takes the current candate values and removes the ones existing in `existingNumbers`
            _candidates = new HashSet<int>(_candidates.Except(candidates));
            SudokuProgress result = SudokuProgress.NoProgress;
            if (_candidates.Count == 1) // one candidate value
            {
                Fix(_candidates.First(), "Only one possibility");
                result = SudokuProgress.Progress;
            }
            else if (_candidates.Count == 0) // no candidate value
                return SudokuProgress.Failed;
            return result;
        }

        public void Dump()
        {
            Console.WriteLine("Cell[{0},{1}]:{2} {3}", X, Y, Value, CandidateCount == 0 ? "/" : _candidates.Select(x => x.ToString(CultureInfo.InvariantCulture)).Aggregate((s, s1) => s+","+s1));
        }
    }
}
