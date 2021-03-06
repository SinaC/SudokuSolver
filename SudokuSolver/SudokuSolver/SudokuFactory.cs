﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BruteForceSudokuSolver
{
    public class SudokuFactory
    {
        private const int DefaultSize = 9;
        private const int SamuraiAreas = 7;
        private const int BoxSize = 3;
        private const int HyperMargin = 1;

        public static IEnumerable<Tuple<int, int>> Box(int sizeX, int sizeY)
        {
            //foreach (int x in Enumerable.Range(0, sizeX))
            //    foreach (int y in Enumerable.Range(0, sizeY))
            //        yield return new Tuple<int, int>(x, y);
            return Enumerable.Range(0, sizeX)
                .SelectMany(
                    x => Enumerable.Range(0, sizeY),
                    (x, y) => new Tuple<int, int>(x, y));
        }

        public static SudokuBoard Samurai()
        {
            SudokuBoard board = new SudokuBoard(SamuraiAreas*BoxSize, SamuraiAreas*BoxSize, DefaultSize);
            // Removed the empty areas where there are no tiles
            var queriesForBlocked = new List<IEnumerable<SudokuTile>>
                {
                    Box(BoxSize, BoxSize*2).Select(pos => board.Tile(pos.Item1 + DefaultSize, pos.Item2)),
                    Box(BoxSize, BoxSize*2).Select(pos => board.Tile(pos.Item1 + DefaultSize, pos.Item2 + DefaultSize*2 - BoxSize)),
                    Box(BoxSize*2, BoxSize).Select(pos => board.Tile(pos.Item1, pos.Item2 + DefaultSize)),
                    Box(BoxSize*2, BoxSize).Select(pos => board.Tile(pos.Item1 + DefaultSize*2 - BoxSize, pos.Item2 + DefaultSize))
                };
            foreach (var query in queriesForBlocked)
                foreach (var tile in query)
                    tile.Block();

            // Select the tiles in the 3 x 3 area (area.X, area.Y) and create rules for them
            foreach (var area in Box(SamuraiAreas, SamuraiAreas))
            {
                var tilesInArea = Box(BoxSize, BoxSize).Select(pos => board.Tile(area.Item1*BoxSize + pos.Item1, area.Item2*BoxSize + pos.Item2)).ToList();
                if (tilesInArea.First().IsBlocked)
                    continue;
                board.CreateRule("Area " + area.Item1 + ", " + area.Item2, tilesInArea);
            }

            // Select all rows and create columns for them
            //var cols = from pos in Box(board.Width, 1) select new { X = pos.Item1, Y = pos.Item2 };
            //var rows = from pos in Box(1, board.Height) select new { X = pos.Item1, Y = pos.Item2 };
            foreach (var posSet in Enumerable.Range(0, board.Width))
            {
                board.CreateRule("Column Upper " + posSet, Box(1, DefaultSize).Select(pos => board.Tile(posSet, pos.Item2)));
                board.CreateRule("Column Lower " + posSet, Box(1, DefaultSize).Select(pos => board.Tile(posSet, pos.Item2 + DefaultSize + BoxSize)));

                board.CreateRule("Row Left " + posSet, Box(DefaultSize, 1).Select(pos => board.Tile(pos.Item1, posSet)));
                board.CreateRule("Row Right " + posSet, Box(DefaultSize, 1).Select(pos => board.Tile(pos.Item1 + DefaultSize + BoxSize, posSet)));

                if (posSet >= BoxSize*2 && posSet < BoxSize*2 + DefaultSize)
                {
                    // Create rules for the middle sudoku
                    board.CreateRule("Column Middle " + posSet, Box(1, 9).Select(pos => board.Tile(posSet, pos.Item2 + BoxSize*2)));
                    board.CreateRule("Row Middle " + posSet, Box(9, 1).Select(pos => board.Tile(pos.Item1 + BoxSize*2, posSet)));
                }
            }
            return board;
        }

        public static SudokuBoard SizeAndBoxes(int width, int height, int boxCountX, int boxCountY)
        {
            SudokuBoard board = new SudokuBoard(width, height);
            board.AddBoxesCount(boxCountX, boxCountY);
            return board;
        }

        public static SudokuBoard ClassicWith3x3Boxes()
        {
            return SizeAndBoxes(DefaultSize, DefaultSize, DefaultSize/BoxSize, DefaultSize/BoxSize);
        }

        public static SudokuBoard ClassicWith3x3BoxesAndHyperRegions()
        {
            SudokuBoard board = ClassicWith3x3Boxes();
            const int hyperSecond = HyperMargin + BoxSize + HyperMargin;
            // Create the four extra hyper regions
            board.CreateRule("HyperA", Box(3, 3).Select(pos => board.Tile(pos.Item1 + HyperMargin, pos.Item2 + HyperMargin)));
            board.CreateRule("HyperB", Box(3, 3).Select(pos => board.Tile(pos.Item1 + hyperSecond, pos.Item2 + HyperMargin)));
            board.CreateRule("HyperC", Box(3, 3).Select(pos => board.Tile(pos.Item1 + HyperMargin, pos.Item2 + hyperSecond)));
            board.CreateRule("HyperD", Box(3, 3).Select(pos => board.Tile(pos.Item1 + hyperSecond, pos.Item2 + hyperSecond)));
            return board;
        }

        public static SudokuBoard ClassicWithSpecialBoxes(string[] areas)
        {
            int sizeX = areas[0].Length;
            int sizeY = areas.Length;
            SudokuBoard board = new SudokuBoard(sizeX, sizeY);
            var joinedString = String.Join("", areas);
            var grouped = joinedString.Distinct();

            // Loop through all the unique characters
            foreach (var ch in grouped)
            {
                // Select the rule tiles based on the index of the character
                //var ruleTiles = from i in Enumerable.Range(0, joinedString.Length)
                //                where joinedString[i] == ch // filter out any non-matching characters
                //                select board.Tile(i%sizeX, i/sizeY);
                var ruleTiles = Enumerable.Range(0, joinedString.Length)
                    .Where(i => joinedString[i] == ch)
                    .Select(i => board.Tile(i%sizeX, i/sizeY));
                board.CreateRule("Area " + ch, ruleTiles);
            }

            return board;
        }
    }
}
