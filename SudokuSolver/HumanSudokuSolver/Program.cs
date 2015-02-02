using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//http://angusj.com/sudoku/hints.php
//http://fr.wikipedia.org/wiki/Sudoku#M.C3.A9thodes_de_r.C3.A9solution_utilis.C3.A9es_par_les_joueurs
//http://hodoku.sourceforge.net/en/techniques.php

namespace NaturalSudokuSolver
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SudokuBoard board = new SudokuBoard();

            string board1 = // (cannot be solved with singleton and naked/hidden pair/triple/quadruple)
                "...84...9" +
                "..1.....5" +
                "8...2.46." + // "8...2146."+ <-- unique solution
                "7.8....9." +
                "........." +
                ".5....3.1" +
                ".2491...7" +
                "9.....5.." +
                "3...84...";

            string board2 = // (cannot be solved with singleton and naked/hidden pair/triple/quadruple)
                "8........" +
                "..36....." +
                ".7..9.2.." +
                ".5...7..." +
                "....457.." +
                "...1...3." +
                "..1....68" +
                "..85...1." +
                ".9....4..";

            // No solution in brute-force (cannot be solved with singleton and naked pair/triple/quadruple)
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

            // Solved with singletons
            string board4 =
                ".6.3..8.4" +
                "537.9...." +
                ".4...63.7" +
                ".9..51238" +
                "........." +
                "71362..4." +
                "3.64...1." +
                "....6.523" +
                "1.2..9.8.";

            // Naked pairs in column 4 (cannot be solved with singleton and naked pair/triple/quadruple)
            string board5 =
                "..5......" +
                "..8.3.6.." +
                ".1.5.9.4." +
                ".4.....1." +
                "5...2...8" +
                ".7.....3." +
                ".5.1.4.9." +
                "1.3.6.7.4" +
                "..4......";

            // Naked triple in column 1 (solved with singleton and naked pair/triple/quadruple)
            string board6=
                "...29438." +
                "...17864." +
                "48.3561.." +
                "..48375.1" +
                "...4157.." +
                "5..629834" +
                "953782416" +
                "126543978" +
                ".4.961253";

            // Naked triple in block 1 (solved with singleton and naked pair/triple/quadruple)
            string board7 =
                "39....7.." +
                "......65." +
                "5.7...349" +
                ".4938.5.6" +
                "6.1.54983" +
                "853...4.." +
                "9..8..134" +
                "..294.865" +
                "4.....297";


            // Naked quadruple in row 7 (solved with singleton and naked pair/triple/quadruple)
            string board8 =
                ".1.72.563" +
                ".56.3.247" +
                "732546189" +
                "693287415" +
                "247615938" +
                "581394..." +
                ".....2..." +
                "........1" +
                "..587....";

            // Locked candidates in block 0, row 2 (solved with singleton and locked candidates)
            string board9 =
                "984......" +
                "..25...4." +
                "..19.4..2" +
                "..6.9723." +
                "..36.2..." +
                "2.9.3561." +
                "195768423" +
                "427351896" +
                "638..9751";

            // Locked candidates in block 7, row 6 (solved with singleton and locked candidates)
            string board10 =
                "34...6.7." +
                ".8....93." +
                "..2.3..6." +
                "....1...." +
                ".9736485." +
                ".....2..." +
                "........." +
                "...6.8.9." +
                "...923785";

            // Hidden triple in column 5 (first solution step is this hidden triple or a naked quadruple)
            string board11 =
                "5..62..37"+
                "..489...." +
                "....5...." +
                "93......." +
                ".2....6.5" +
                "7.......3" +
                ".....9..." +
                "......7.." +
                "68.57...2";

            // Hidden pair in row 0/block 0
            string board12 =
                "....6...." +
                "....42736" +
                "..673..4." +
                ".94....68" +
                "....964.7" +
                "6.7.5.923" +
                "1......85" +
                ".6..8.271" +
                "..5.1..94";

            // Hidden triple in block 6
            string board13 =
                "28....473" +
                "534827196" +
                ".71.34.8." +
                "3..5...4." +
                "...34..6." +
                "46.79.31." +
                ".9.2.3654" +
                "..3..9821" +
                "....8.937";

            // Hidden quadruple in block 7
            string board14 =
                "816573294" +
                "392......" +
                "4572.9..6" +
                "941...568" +
                "785496123" +
                "6238...4." +
                "279.....1" +
                "138....7." +
                "564....82";

            // X-Wing in row 1, 4 column 4, 7 candidate 5
            string board15 =
                ".41729.3." +
                "769..34.2" +
                ".3264.719" +
                "4.39..17." +
                "6.7..49.3" +
                "19537..24" +
                "214567398" +
                "376.9.541" +
                "958431267";

            // X-Wing in column 0, 4 row 1, 4 candidate 1
            string board16 =
    "98..62753" +
    ".65..3..." +
    "327.5...6" +
    "79..3.5.." +
    ".5...9..." +
    "832.45..9" +
    "673591428" +
    "249.87..5" +
    "518.2...7";

            // Swordfish in row 1, 2, 8 column 0, 4, 7
            string board17 =
    "16.543.7." +
    ".786.1435" +
    "4358.76.1" +
    "72.458.69" +
    "6..912.57" +
    "...376..4" +
    ".16.3..4." +
    "3...8..16" +
    "..71645.3";

            board.InitializeGrid(board17);

            IEnumerable<SudokuBoard> solutions = board.Solve();
            solutions.ToList().ForEach(x =>
                {
                    x.Display();
                    if (!x.Solved)
                        x.Dump();
                });
            //solutions.ToList().ForEach(x => x.Display());
        }
    }
}
