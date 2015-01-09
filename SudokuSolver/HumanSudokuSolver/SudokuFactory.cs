using System;
using System.Collections.Generic;
using System.Linq;

namespace NaturalSudokuSolver
{
    public class SudokuFactory
    {
        public static IEnumerable<Tuple<int, int>> Square(int size)
        {
            return Enumerable.Range(0, size)
                .SelectMany(
                    _ => Enumerable.Range(0, size),
                    (x, y) => new Tuple<int, int>(y, x));
        }
    }
}
