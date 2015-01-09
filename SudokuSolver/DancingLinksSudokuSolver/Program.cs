using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DancingLinksSudokuSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            string board3 =
                "358.4...." +
                "412.6...." +
                "769.2...." +
                "....8...." +
                "123456789" +
                "....3...." +
                "....1...." +
                "....9...." +
                "....7....";

            DisplayBoard(board3);

            string solution = SudokuSolver.Solve(board3);

            Console.WriteLine();

            DisplayBoard(solution);
        }

        static void DisplayBoard(string board)
        {
            int size = board.Length;
            int subsize = (int)Math.Sqrt(size);
            int subsubsize = (int)Math.Sqrt(subsize);

            int index = 0;
            for (int y = 0; y < subsize; y++)
            {
                for (int x = 0; x < subsize; x++)
                {
                    char c = board[index++];
                    Console.Write(c);
                    if (x != subsize - 1 && ((x + 1) % subsubsize == 0))
                        Console.Write("|");
                }
                Console.WriteLine();
                if (y != subsize - 1 && ((y + 1) % subsubsize == 0))
                {
                    for (int x = 0; x < subsize; x++)
                    {
                        Console.Write("-");
                        if (x != subsize - 1 && ((x + 1) % subsubsize == 0))
                            Console.Write(" ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
