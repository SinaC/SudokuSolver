﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BruteForceSudokuSolver
{
    public class SudokuBoard
    {
        private readonly int _maxValue;
        private readonly ISet<SudokuRule> _rules = new HashSet<SudokuRule>();
        private readonly SudokuTile[,] _tiles;

        public SudokuBoard(SudokuBoard copy)
        {
            _maxValue = copy._maxValue;
            _tiles = new SudokuTile[copy.Width,copy.Height];
            CreateTiles();
            // Copy the tile values
            foreach (var pos in SudokuFactory.Box(Width, Height))
                _tiles[pos.Item1, pos.Item2] = new SudokuTile(pos.Item1, pos.Item2, _maxValue)
                    {
                        Value = copy._tiles[pos.Item1, pos.Item2].Value
                    };

            // Copy the rules
            foreach (SudokuRule rule in copy._rules)
            {
                var ruleTiles = new HashSet<SudokuTile>();
                foreach (SudokuTile tile in rule)
                    ruleTiles.Add(_tiles[tile.X, tile.Y]);
                _rules.Add(new SudokuRule(ruleTiles, rule.Description));
            }
        }

        public SudokuBoard(int width, int height, int maxValue)
        {
            _maxValue = maxValue;
            _tiles = new SudokuTile[width,height];
            CreateTiles();
            if (_maxValue == width || _maxValue == height) // If maxValue is not width or height, then adding line rules would be stupid
                SetupLineRules();
        }

        public SudokuBoard(int width, int height)
            : this(width, height, Math.Max(width, height))
        {
        }

        private void CreateTiles()
        {
            foreach (var pos in SudokuFactory.Box(_tiles.GetLength(0), _tiles.GetLength(1)))
                _tiles[pos.Item1, pos.Item2] = new SudokuTile(pos.Item1, pos.Item2, _maxValue);
        }

        private void SetupLineRules()
        {
            // Create rules for rows and columns
            for (int x = 0; x < Width; x++)
            {
                IEnumerable<SudokuTile> row = GetCol(x);
                _rules.Add(new SudokuRule(row, "Row " + x.ToString(CultureInfo.InvariantCulture)));
            }
            for (int y = 0; y < Height; y++)
            {
                IEnumerable<SudokuTile> col = GetRow(y);
                _rules.Add(new SudokuRule(col, "Col " + y.ToString(CultureInfo.InvariantCulture)));
            }
        }

        internal IEnumerable<SudokuTile> TileBox(int startX, int startY, int sizeX, int sizeY)
        {
            return SudokuFactory.Box(sizeX, sizeY).Select(pos => _tiles[startX + pos.Item1, startY + pos.Item2]);
        }

        private IEnumerable<SudokuTile> GetRow(int row)
        {
            for (int i = 0; i < _tiles.GetLength(0); i++)
                yield return _tiles[i, row];
        }

        private IEnumerable<SudokuTile> GetCol(int col)
        {
            for (int i = 0; i < _tiles.GetLength(1); i++)
                yield return _tiles[col, i];
        }

        public int Width
        {
            get { return _tiles.GetLength(0); }
        }

        public int Height
        {
            get { return _tiles.GetLength(1); }
        }

        public void CreateRule(string description, params SudokuTile[] tiles)
        {
            _rules.Add(new SudokuRule(tiles, description));
        }

        public void CreateRule(string description, IEnumerable<SudokuTile> tiles)
        {
            _rules.Add(new SudokuRule(tiles, description));
        }

        public bool CheckValid()
        {
            return _rules.All(rule => rule.CheckValid());
        }

        public IEnumerable<SudokuBoard> Solve()
        {
            ResetSolutions();
            SudokuProgress simplify = SudokuProgress.Progress;
            while (simplify == SudokuProgress.Progress)
                simplify = Simplify();

            if (simplify == SudokuProgress.Failed)
                yield break;

            // Find one of the values with the least number of alternatives, but that still has at least 2 alternatives
            //var query = from rule in _rules
            //            from tile in rule
            //            where tile.PossibleCount > 1
            //            orderby tile.PossibleCount ascending
            //            select tile;
            var query = _rules.SelectMany(
                rule => rule.Where(tile => tile.PossibleCount > 1)
                            .OrderBy(tile => tile.PossibleCount),
                (rule, tile) => tile);

            SudokuTile chosen = query.FirstOrDefault();
            if (chosen == null)
            {
                // The board has been completed, we're done!
                yield return this;
                yield break;
            }

            System.Diagnostics.Debug.WriteLine("SudokuTile: " + chosen);

            foreach (var value in Enumerable.Range(1, _maxValue))
            {
                // Iterate through all the valid possibles on the chosen square and pick a number for it
                if (!chosen.IsValuePossible(value))
                    continue;
                var copy = new SudokuBoard(this);
                copy.Tile(chosen.X, chosen.Y).Fix(value, "Trial and error");
                foreach (var innerSolution in copy.Solve())
                    yield return innerSolution;
            }
            //yield break;
        }

        public void Output()
        {
            for (int y = 0; y < _tiles.GetLength(1); y++)
                for (int x = 0; x < _tiles.GetLength(0); x++)
                    Console.Write(_tiles[x, y].ToStringSimple());
            Console.WriteLine();
        }

        public SudokuTile Tile(int x, int y)
        {
            return _tiles[x, y];
        }

        private int _rowAddIndex; // Only used by AddRow

        public void AddRow(string s)
        {
            // Method for initializing a board from string
            for (int i = 0; i < s.Length; i++)
            {
                var tile = _tiles[i, _rowAddIndex];
                if (s[i] == '/')
                {
                    tile.Block();
                    continue;
                }
                int value = s[i] == '.' ? 0 : (int) Char.GetNumericValue(s[i]);
                tile.Value = value;
            }
            _rowAddIndex++;
        }

        internal void ResetSolutions()
        {
            foreach (SudokuTile tile in _tiles)
                tile.ResetPossibles();
        }

        internal SudokuProgress Simplify()
        {
            bool valid = CheckValid();
            if (!valid)
                return SudokuProgress.Failed;
            return _rules.Aggregate(
                SudokuProgress.NoProgress,
                (current, rule) => SudokuTile.CombineSolvedState(current, rule.Solve()));
        }

        internal void AddBoxesCount(int boxesX, int boxesY)
        {
            int sizeX = Width/boxesX;
            int sizeY = Height/boxesY;

            var boxes = SudokuFactory.Box(sizeX, sizeY);
            foreach (var pos in boxes)
            {
                IEnumerable<SudokuTile> boxTiles = TileBox(pos.Item1*sizeX, pos.Item2*sizeY, sizeX, sizeY);
                CreateRule("Box at (" + pos.Item1 + ", " + pos.Item2 + ")", boxTiles);
            }
        }

        internal void OutputRules()
        {
            foreach (var rule in _rules)
                Console.WriteLine(String.Join(",", rule) + " - " + rule);
        }
    }
}
