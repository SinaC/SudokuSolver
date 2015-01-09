using System;
using System.Text;

namespace DancingLinksSudokuSolver
{
    //http://en.wikipedia.org/wiki/Sudoku_algorithms
    public static class SudokuSolver
    {
        public static void Solve(int[][] puzzle)
        {
            int size = puzzle.Length;
            int subsize = (int)Math.Sqrt(size);

            if (size != puzzle[0].Length || subsize * subsize != size)
                throw new ArgumentException("invalid sudoku size");

            var matrix = new int[size * size * size][];

            for (int id = 0; id < size * size * size; id++)
            {
                int r = id / size / size, c = id / size % size, v = id % size;

                matrix[id] = new int[size * size * 4];

                // Fixed-Value Contrains
                if (puzzle[r][c] != 0 && puzzle[r][c] != v + 1)
                    continue;

                // Row-Column Constrains
                matrix[id][r * size + c] = 1;

                // Row-Number Constrains
                matrix[id][size * size + r * size + v] = 1;

                // Column-Number Constrains
                matrix[id][size * size * 2 + c * size + v] = 1;

                // Box-Number Constrains
                matrix[id][size * size * 3 + ((r / subsize) * subsize + (c / subsize)) * size + v] = 1;
            }

            var solution = ExactCover.GetSingleSolution(matrix);

            if (solution.Count != size * size)
                throw new InvalidOperationException("unsolvable puzzle");

            foreach (var id in solution)
            {
                int r = id / size / size, c = id / size % size, v = id % size;

                puzzle[r][c] = v + 1;
            }
        }

        public static string Solve(string s)
        {
            int size = s.Length;
            int subsize = (int)Math.Sqrt(size);

            if (subsize * subsize != size)
                throw new ArgumentException("invalid sudoku size");

            int[][] puzzle = new int[subsize][];
            for(int y = 0; y < subsize; y++)
            {
                puzzle[y] = new int[subsize];
                for (int x = 0; x < subsize; x++)
                {
                    int value = 0;
                    
                    char c = s[x + y*subsize];
                    if (c != ' ' && c != '.')
                        value = (int) Char.GetNumericValue(c);
                    puzzle[y][x] = value;
                }
            }

            Solve(puzzle);

            StringBuilder sb = new StringBuilder(size);
            for (int y = 0; y < subsize; y++)
                for (int x = 0; x < subsize; x++)
                    sb.Append(puzzle[y][x]);
            return sb.ToString();
        }
    }
}
