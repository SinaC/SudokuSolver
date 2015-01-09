using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BruteForceSudokuSolver
{
    public class SudokuTile
    {
        internal static SudokuProgress CombineSolvedState(SudokuProgress a, SudokuProgress b)
        {
            if (a == SudokuProgress.Failed)
                return a;
            if (a == SudokuProgress.NoProgress)
                return b;
            if (a == SudokuProgress.Progress)
                return b == SudokuProgress.Failed ? b : a;
            throw new InvalidOperationException("Invalid value for a");
        }

        public const int Cleared = 0;
        private readonly int _maxValue;
        private int _value;
        private readonly int _x;
        private readonly int _y;
        private ISet<int> _possibleValues;
        private bool _blocked;

        public SudokuTile(int x, int y, int maxValue)
        {
            _x = x;
            _y = y;
            _blocked = false;
            _maxValue = maxValue;
            _possibleValues = new HashSet<int>();
            _value = 0;
        }

        public int Value
        {
            get { return _value; }
            set
            {
                if (value > _maxValue)
                    throw new ArgumentOutOfRangeException("SudokuTile Value cannot be greater than " + _maxValue + ". Was " + value);
                if (value < Cleared)
                    throw new ArgumentOutOfRangeException("SudokuTile Value cannot be zero or smaller. Was " + value);
                _value = value;
            }
        }

        public bool HasValue
        {
            get { return Value != Cleared; }
        }

        public string ToStringSimple()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return String.Format("Value {0} at pos {1}, {2}. #:{3} ", Value, _x, _y, _possibleValues.Count);
        }

        internal void ResetPossibles()
        {
            _possibleValues.Clear();
            foreach (int i in Enumerable.Range(1, _maxValue))
            {
                if (!HasValue || Value == i)
                    _possibleValues.Add(i);
            }
        }

        public void Block()
        {
            _blocked = true;
        }

        internal void Fix(int value, string reason)
        {
            System.Diagnostics.Debug.WriteLine("Fixing {0} on pos {1}, {2}: {3}", value, _x, _y, reason);
            Value = value;
            ResetPossibles();
        }

        internal SudokuProgress RemovePossibles(IEnumerable<int> existingNumbers)
        {
            if (_blocked)
                return SudokuProgress.NoProgress;
            // Takes the current possible values and removes the ones existing in `existingNumbers`
            _possibleValues = new HashSet<int>(_possibleValues.Where(x => !existingNumbers.Contains(x)));
            SudokuProgress result = SudokuProgress.NoProgress;
            if (_possibleValues.Count == 1)
            {
                Fix(_possibleValues.First(), "Only one possibility");
                result = SudokuProgress.Progress;
            }
            if (_possibleValues.Count == 0)
                return SudokuProgress.Failed;
            return result;
        }

        public bool IsValuePossible(int i)
        {
            return _possibleValues.Contains(i);
        }

        public int X
        {
            get { return _x; }
        }

        public int Y
        {
            get { return _y; }
        }

        public bool IsBlocked
        {
            get { return _blocked; }
        } 
        
        // A blocked field can not contain a value -- used for creating 'holes' in the map
        public int PossibleCount
        {
            get { return IsBlocked ? 1 : _possibleValues.Count; }
        }
    }
}
