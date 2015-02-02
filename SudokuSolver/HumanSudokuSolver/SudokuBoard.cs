using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaturalSudokuSolver
{
    public class SudokuBoard
    {
        private const int Size = 9;
        private const int BlockSize = 3;
        private readonly int _maxValue;
        private readonly SudokuCell[,] _cells;

        public SudokuBoard(SudokuBoard copy)
        {
            _maxValue = copy._maxValue;
            _cells = new SudokuCell[Size,Size];
            CreateCells();
            // Copy the cell values
            foreach (Tuple<int, int> pos in SudokuFactory.Square(Size))
                _cells[pos.Item1, pos.Item2] = new SudokuCell(pos.Item1, pos.Item2, _maxValue)
                    {
                        Value = copy._cells[pos.Item1, pos.Item2].Value
                    };
        }

        public SudokuBoard()
        {
            _maxValue = 9;
            _cells = new SudokuCell[Size,Size];
            CreateCells();
        }

        public bool Solved
        {
            get { return Cells.All(c => c.HasValue); }
        }

        public void Display()
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    SudokuCell cell = _cells[x, y];
                    if (cell.Value == 0)
                        Console.Write(" ");
                    else
                        Console.Write(cell.Value);
                    if (x != Size - 1 && ((x + 1)%BlockSize == 0))
                        Console.Write("|");
                }
                Console.WriteLine();
                if (y != Size - 1 && ((y + 1)%BlockSize == 0))
                {
                    for (int x = 0; x < Size; x++)
                    {
                        Console.Write("-");
                        if (x != Size - 1 && ((x + 1)%BlockSize == 0))
                            Console.Write(" ");
                    }
                    Console.WriteLine();
                }
            }
        }

        public void Dump()
        {
            foreach (SudokuCell cell in Cells)
                cell.Dump();
        }

        public void InitializeGrid(string grid)
        {
            if (grid.Length < Size*Size)
                throw new ArgumentException(String.Format("Cannot initialize grid with a string containing less than {0} characters", Size*Size), "grid");
            for (int i = 0; i < Size; i++)
                InitializeRow(i, grid.Substring(i*Size, Size));
        }

        public void InitializeRow(int rowId, string row)
        {
            if (row.Length != Size)
                throw new ArgumentException(String.Format("Cannot initialize row with a string containing less than {0} characters", Size), "row");
            // Method for initializing a board from string
            for (int i = 0; i < row.Length; i++)
            {
                SudokuCell cell = _cells[i, rowId];
                int value = row[i] == '.' ? 0 : (int) Char.GetNumericValue(row[i]);
                cell.Value = value;
            }
        }

        public IEnumerable<SudokuBoard> Solve()
        {
            ResetSolutions();

            while (true)
            {
                SudokuProgress progress = SudokuProgress.NoProgress;

                // Naked singles
                progress = CombineSolvedState(progress, SolveNakedSingles());
                if (progress == SudokuProgress.Progress) // at least one modification
                    continue;
                if (progress == SudokuProgress.Failed) // Failed, stop with this board
                    yield break;

                //// Hidden singles
                //progress = CombineSolvedState(progress, SolveHiddenSingles());
                //if (progress == SudokuProgress.Progress) // at least one modification
                //    continue;
                //if (progress == SudokuProgress.Failed) // Failed, stop with this board
                //    yield break;

                //// Locked candidates
                //progress = CombineSolvedState(progress, SolveLockedCandidates());
                //if (progress == SudokuProgress.Progress) // at least one modification
                //    continue;
                //if (progress == SudokuProgress.Failed) // Failed, stop with this board
                //    yield break;

                //// Hidden groups
                //progress = SolveHiddenGroups();
                //if (progress == SudokuProgress.Progress) // at least one modification
                //    continue;
                //if (progress == SudokuProgress.Failed) // Failed, stop with this board
                //    yield break;

                //// Naked groups
                //progress = SolveNakedGroups();
                //if (progress == SudokuProgress.Progress) // at least one modification
                //    continue;
                //if (progress == SudokuProgress.Failed) // Failed, stop with this board
                //    yield break;

                // Fishes
                progress = SolveFishes();
                if (progress == SudokuProgress.Progress) // at least one modification
                    continue;
                if (progress == SudokuProgress.Failed) // Failed, stop with this board
                    yield break;

                //

                if (progress == SudokuProgress.Failed)
                    yield break;

                if (progress == SudokuProgress.NoProgress)
                    break;
            }

            yield return this;
        }

        #region Singles

        public SudokuProgress SolveNakedSingles()
        {
            // Foreach unsolved cells
            return Cells
                .Where(c => !c.HasValue)
                .Select(SolveNakedSingle)
                .Aggregate(SudokuProgress.NoProgress, CombineSolvedState);
        }

        private SudokuProgress SolveNakedSingle(SudokuCell cell)
        {
            // Build values in neighbour row/column/block
            ISet<int> values = new HashSet<int>();
            foreach (SudokuCell rowNeighbour in Row(cell.Y).Where(c => c.HasValue)) // no need to test on outer loop cell because HasValue is false
                values.Add(rowNeighbour.Value);
            foreach (SudokuCell columnNeighbour in Column(cell.X).Where(c => c.HasValue)) // no need to test on outer loop cell because HasValue is false
                values.Add(columnNeighbour.Value);
            foreach (SudokuCell blockNeighbour in Block(cell.X, cell.Y).Where(c => c.HasValue)) // no need to test on outer loop cell because HasValue is false
                values.Add(blockNeighbour.Value);
            // Remove values from candidates
            if (cell.HasCommonCandidate(values))
                return cell.RemoveCandidates(values, "Naked single");
            return SudokuProgress.NoProgress;
        }

        public SudokuProgress SolveHiddenSingles()
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            // Check rows
            progress = Rows
                .Select(row => SolveHiddenSingles(row.Where(c => !c.HasValue && c.CandidateCount > 0)))
                .Aggregate(progress, CombineSolvedState);
            if (progress != SudokuProgress.NoProgress)
                return progress;
            
            // Check columns
            progress = Columns
                .Select(column => SolveHiddenSingles(column.Where(c => !c.HasValue && c.CandidateCount > 0)))
                .Aggregate(progress, CombineSolvedState);
            if (progress != SudokuProgress.NoProgress)
                return progress;
            
            // Check blocks
            progress = Blocks
                .Select(block => SolveHiddenSingles(block.Where(c => !c.HasValue && c.CandidateCount > 0)))
                .Aggregate(progress, CombineSolvedState);
            if (progress != SudokuProgress.NoProgress)
                return progress;

            return progress;
        }

        private SudokuProgress SolveHiddenSingles(IEnumerable<SudokuCell> cells)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            List<SudokuCell>[] count = new List<SudokuCell>[Size + 1]; // store a list of cell for each value

            // for each cell in group, check if a number has only one place available
            foreach (SudokuCell cell in cells)
                foreach (int candidate in cell.Candidates)
                {
                    count[candidate] = count[candidate] ?? new List<SudokuCell>();
                    count[candidate].Add(cell);
                }
            for (int c = 1; c < Size + 1; c++) // 0 is not a valid value
                if (count[c] != null && count[c].Count == 1)
                {
                    //count[c].First().Value = c;
                    count[c].First().Fix(c, "Hidden single");
                    progress = CombineSolvedState(progress, SudokuProgress.Progress);
                }

            return progress;
        }

        #endregion

        #region Locked candidates

        private SudokuProgress SolveLockedCandidates()
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            //SolveLockedCandidatesRowColumn(Row(8).Where(c => !c.HasValue && c.CandidateCount > 0), "row");

            // Check group/row and group/column
            progress = Blocks.Select(block => SolveLockedCandidatesBlock(block.Where(c => !c.HasValue && c.CandidateCount > 0)))
                .Aggregate(progress, CombineSolvedState);
            if (progress != SudokuProgress.NoProgress)
                return progress;
            
            // Check row/group
            progress = Rows.Select(row => SolveLockedCandidatesRowColumn(row.Where(c => !c.HasValue && c.CandidateCount > 0), "row"))
                .Aggregate(progress, CombineSolvedState);
            if (progress != SudokuProgress.NoProgress)
                return progress;

            // Check column/group
            progress = Columns.Select(column => SolveLockedCandidatesRowColumn(column.Where(c => !c.HasValue && c.CandidateCount > 0), "column"))
                .Aggregate(progress, CombineSolvedState);
            if (progress != SudokuProgress.NoProgress)
                return progress;

            return progress;
        }

        // Sometimes a candidate within a box is restricted to one row or column. Since one of these cells must contain that specific candidate, the candidate can safely be excluded from the remaining cells in that row or column outside of the box.
        private SudokuProgress SolveLockedCandidatesBlock(IEnumerable<SudokuCell> block)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            // project each value in block on horizontal and vertical axis
            List<SudokuCell>[] horizontalProjection = new List<SudokuCell>[Size + 1];
            List<SudokuCell>[] verticalProjection = new List<SudokuCell>[Size + 1];
            foreach (SudokuCell cell in block)
            {
                foreach (int candidate in cell.Candidates)
                {
                    horizontalProjection[candidate] = horizontalProjection[candidate] ?? new List<SudokuCell>();
                    horizontalProjection[candidate].Add(cell);
                    verticalProjection[candidate] = verticalProjection[candidate] ?? new List<SudokuCell>();
                    verticalProjection[candidate].Add(cell);
                }
            }

            // for each candidate, check if horizontal/vertical projections have the same x/y
            for (int candidate = 1; candidate < Size + 1; candidate++) // 0 is not a valid value
            {
                if (horizontalProjection[candidate] != null && horizontalProjection[candidate].Count >= 2) // at least 2 cell with this candidate
                {
                    // must have same X
                    int x = horizontalProjection[candidate][0].X;
                    if (horizontalProjection[candidate].All(cell => cell.X == x))
                    {
                        // remove candidate from every other cell in same column
                        foreach (SudokuCell cell in Column(x).Except(horizontalProjection[candidate]))
                            if (!cell.HasValue && cell.IsCandidate(candidate))
                            {
                                cell.RemoveCandidate(candidate, "Locked candidates block/column");
                                progress = CombineSolvedState(progress, SudokuProgress.Progress);
                            }
                    }
                }
                if (verticalProjection[candidate] != null && verticalProjection[candidate].Count >= 2) // at least 2 cell with this candidate
                {
                    // must have same Y
                    int y = verticalProjection[candidate][0].Y;
                    if (verticalProjection[candidate].All(cell => cell.Y == y))
                    {
                        // remove candidate from every other cell in same row
                        foreach (SudokuCell cell in Row(y).Except(verticalProjection[candidate]))
                            if (!cell.HasValue && cell.IsCandidate(candidate))
                            {
                                cell.RemoveCandidate(candidate, "Locked candidates block/row");
                                progress = CombineSolvedState(progress, SudokuProgress.Progress);
                            }
                    }
                }
            }
            return progress;
        }

        // Sometimes a candidate within a row or column is restricted to one box. Since one of these cells must contain that specific candidate, the candidate can safely be excluded from the remaining cells in the box.
        private SudokuProgress SolveLockedCandidatesRowColumn(IEnumerable<SudokuCell> cells, string rowOrColumn)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            // project each value in row/column on 'block axis'
            List<SudokuCell>[] blockProjection = new List<SudokuCell>[Size + 1];
            foreach (SudokuCell cell in cells)
            {
                foreach (int candidate in cell.Candidates)
                {
                    blockProjection[candidate] = blockProjection[candidate] ?? new List<SudokuCell>();
                    blockProjection[candidate].Add(cell);
                }
            }

            // for each candidate, check if block projections have the same blockId
            for (int candidate = 1; candidate < Size + 1; candidate++) // 0 is not a valid value
            {
                if (blockProjection[candidate] != null && blockProjection[candidate].Count >= 2) // at least 2 cell with this candidate
                {
                    // must have same blockId
                    int blockId = BlockId(blockProjection[candidate][0]);
                    if (blockProjection[candidate].All(cell => BlockId(cell) == blockId))
                    {
                        // remove candidate from every other cell in same block
                        foreach (SudokuCell cell in Block(blockId).Except(blockProjection[candidate]))
                            if (!cell.HasValue && cell.IsCandidate(candidate))
                            {
                                cell.RemoveCandidate(candidate, String.Format("Locked candidates {0}/block", rowOrColumn));
                                progress = CombineSolvedState(progress, SudokuProgress.Progress);
                            }
                    }
                }
            }

            return progress;
        }

        #endregion

        #region Hidden pair/triple/quadruple

        //If you can find two cells within a house such as that two candidates appear nowhere outside those cells in that house, those two candidates must be placed in the two cells. All other candidates can therefore be eliminated.

        private SudokuProgress SolveHiddenGroups()
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            //progress = CombineSolvedState(progress, SolveHiddenGroup(Column(5).Where(c => !c.HasValue && c.CandidateCount > 0), 3) ); // board11
            //progress = CombineSolvedState(progress, SolveHiddenGroup(Row(0).Where(c => !c.HasValue && c.CandidateCount > 0), 2)); // board12
            //progress = CombineSolvedState(progress, SolveHiddenGroup(Block(6).Where(c => !c.HasValue && c.CandidateCount > 0), 3)); // board13
            //progress = CombineSolvedState(progress, SolveHiddenGroup(Block(7).Where(c => !c.HasValue && c.CandidateCount > 0), 4)); // board11

            for (int groupSize = 2; groupSize <= 4; groupSize++) // TODO: max groupSize depends on Size
            {
                // Check rows
                progress = Rows
                    .Select(row => SolveHiddenGroup(row.Where(t => !t.HasValue && t.CandidateCount > 0), groupSize))
                    .Aggregate(progress, CombineSolvedState);
                if (progress != SudokuProgress.NoProgress)
                    return progress;

                // Check columns
                progress = Columns
                    .Select(column => SolveHiddenGroup(column.Where(t => !t.HasValue && t.CandidateCount > 0), groupSize))
                    .Aggregate(progress, CombineSolvedState);
                if (progress != SudokuProgress.NoProgress)
                    return progress;

                // Check blocks
                progress = Blocks
                    .Select(block => SolveHiddenGroup(block.Where(t => !t.HasValue && t.CandidateCount > 0), groupSize))
                    .Aggregate(progress, CombineSolvedState);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
            }

            return progress;
        }

        private SudokuProgress SolveHiddenGroup(IEnumerable<SudokuCell> cells, int groupSize)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            List<SudokuCell>[] count = new List<SudokuCell>[Size + 1]; // store a list of cell for each candidates

            // create a list of cells for each candidates
            foreach (SudokuCell cell in cells)
                foreach (int candidate in cell.Candidates)
                {
                    count[candidate] = count[candidate] ?? new List<SudokuCell>();
                    count[candidate].Add(cell);
                }

            // create a list of candidates with no more than groupSize elements
            IEnumerable<int> validCandidates = Enumerable.Range(1, 9).Where(c => count[c] != null && count[c].Count <= groupSize);
            // create every combinations of valid candidates
            IEnumerable<int[]> combinations = Combinations(validCandidates, groupSize);

            // foreach permutation, check if union of cells in this permutation has exactly groupSize elements, remove every other candidate from cells in permutation
            foreach(int[] combination in combinations)
            {
                HashSet<SudokuCell> union = new HashSet<SudokuCell>();
                foreach(int candidate in combination)
                {
                    union.UnionWith(count[candidate]);
                    if (union.Count > groupSize)
                        break; // no need to continue, too many elements
                }
                if (union.Count == groupSize)
                {
                    foreach (SudokuCell cell in union)
                    {
                        List<int> candidates = cell.Candidates.Except(combination).ToList();
                        if (candidates.Count > 0)
                        {
                            cell.RemoveCandidates(candidates, "Hidden groups");
                            progress = CombineSolvedState(progress, SudokuProgress.Progress);
                        }
                    }
                }
            }

            return progress;
        }

        #endregion

        #region Naked pair/triple/quadruple

        // If two cells in a group contain an identical pair of candidates and only those two candidates, then no other cells in that group could be those values.
        [Obsolete]
        public SudokuProgress SolveNakedPairs()
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            for (int i = 0; i < Size; i++)
            {
                // Check rows
                SudokuProgress rowsProgress = SolveNakedPairs(Row(i).Where(t => !t.HasValue && t.CandidateCount > 0));
                progress = CombineSolvedState(progress, rowsProgress);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
                // Check columns
                SudokuProgress columnsProgress = SolveNakedPairs(Column(i).Where(t => !t.HasValue && t.CandidateCount > 0));
                progress = CombineSolvedState(progress, columnsProgress);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
                // Check blocks
                SudokuProgress blocksProgress = SolveNakedPairs(Block(i).Where(t => !t.HasValue && t.CandidateCount > 0));
                progress = CombineSolvedState(progress, blocksProgress);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
            }

            return progress;
        }

        [Obsolete]
        private SudokuProgress SolveNakedPairs(IEnumerable<SudokuCell> cells)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            // Get cells with only 2 candidates
            var cellsWith2Candidates = cells
                .Where(t => t.CandidateCount == 2)
                .Select(t => new
                    {
                        cell = t,
                        sum2 = t.Candidates.Aggregate((i, i1) => i + i1*i1) // unique value for a pair of distinct digit
                    })
                .ToList(); // Avoid multiple enumerations

            // Foreach pairs of cells with only 2 candidates, remove candidate from other cells
            foreach (var cellWith2Candidates in cellsWith2Candidates)
            {
                var countIdentical = cellsWith2Candidates
                    .Where(t => t.sum2 == cellWith2Candidates.sum2)
                    .ToList(); // Avoid multiple enumerations
                if (countIdentical.Count == 2) // TODO: this loop will be executed for each pairs of cell with same candidates and should be executed only once
                    foreach (SudokuCell cell in cells.Except(countIdentical.Select(x => x.cell))) // remove candidate from other cells
                    {
                        if (cell.HasCommonCandidate(cellWith2Candidates.cell))
                        {
                            cell.RemoveCandidates(cellWith2Candidates.cell.Candidates, "Naked pairs");
                            progress = CombineSolvedState(progress, SudokuProgress.Progress);
                        }
                    }
            }

            return progress;
        }

        // If two cells in a group contain an identical pair of candidates and only those two candidates, then no other cells in that group could be those values.
        // A Naked Triple occurs when three cells in a group contain no candidates other that the same three candidates. The cells which make up a Naked Triple don't have to contain every candidate of the triple. If these candidates are found in other cells in the group they can be excluded.
        // A Naked Quad occurs when four cells in a group contain no candidates other that the same four candidates.

        public SudokuProgress SolveNakedGroups()
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            for (int groupSize = 2; groupSize <= 4; groupSize++) // TODO: max groupSize depends on Size
            {
                // Check rows
                progress = Rows
                    .Select(row => SolveNakedGroups(row.Where(t => !t.HasValue && t.CandidateCount > 0), groupSize))
                    .Aggregate(progress, CombineSolvedState);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
                
                // Check columns
                progress = Columns
                    .Select(column => SolveNakedGroups(column.Where(t => !t.HasValue && t.CandidateCount > 0), groupSize))
                    .Aggregate(progress, CombineSolvedState);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
                
                // Check blocks
                progress = Blocks
                    .Select(block => SolveNakedGroups(block.Where(t => !t.HasValue && t.CandidateCount > 0), groupSize))
                    .Aggregate(progress, CombineSolvedState);
                if (progress != SudokuProgress.NoProgress)
                    return progress;
            }

            return progress;
        }

        private SudokuProgress SolveNakedGroups(IEnumerable<SudokuCell> cells, int groupSize)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            // Make every combinations of x cells containing no more than x candidates
            IEnumerable<SudokuCell[]> combinations = Combinations(cells.Where(t => t.CandidateCount <= groupSize), groupSize);

            // For each combination, check if union of candidates contains exactly x elements
            foreach (SudokuCell[] combination in combinations)
            {
                HashSet<int> candidates = new HashSet<int>();
                foreach (SudokuCell cell in combination)
                {
                    candidates.UnionWith(cell.Candidates);
                    if (candidates.Count > groupSize)
                        break; // no need to continue, too many elements
                }
                if (candidates.Count == groupSize) // group candidates can be safely removed from other cells
                {
                    //foreach (SudokuCell cell in cells.Where(cell => combination.All(c => c != cell)))
                    foreach (SudokuCell cell in cells.Except(combination))
                        if (cell.HasCommonCandidate(candidates))
                        {
                            cell.RemoveCandidates(candidates, "Naked groups");
                            progress = CombineSolvedState(progress, SudokuProgress.Progress);
                        }
                }
            }

            return progress;
        }

        #endregion

        #region Fish

        //  Look for a certain number of non overlapping houses. Those houses are called the base sets (set is synonymous for house here), the candidates contained within them are the base candidates. Non overlapping means, that any base candidate is contained only in one base set, the sets themselves can overlap. Now look for an equal number of different non overlapping houses that cover all base candidates. These new sets are the cover sets containing the cover candidates. If such a combination exists, all cover candidates that are not base candidates can be eliminated.

        public SudokuProgress SolveFishes()
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            SolveFish(3);

            return progress;
        }

        private SudokuProgress SolveFish(int size)
        {
            SudokuProgress progress = SudokuProgress.NoProgress;

            // foreach candidate
            //  count on horizontal/vertical axis how many cells contain this candidate -> 2 maps: List<SudokuCell>[candidate,X/Y]
            //  if there is size number of rows/columns containing no more than size elements in a row/column
            //      we can remove this candidate from other axis

            List<SudokuCell>[,] horizontalProjection = new List<SudokuCell>[Size+1, Size];
            List<SudokuCell>[,] verticalProjection = new List<SudokuCell>[Size+1, Size];

            foreach(SudokuCell cell in Cells.Where(c => !c.HasValue && c.CandidateCount > 0))
            {
                foreach(int candidate in cell.Candidates)
                {
                    horizontalProjection[candidate, cell.X] = horizontalProjection[candidate, cell.X] ?? new List<SudokuCell>();
                    horizontalProjection[candidate, cell.X].Add(cell);
                    verticalProjection[candidate, cell.Y] = verticalProjection[candidate, cell.Y] ?? new List<SudokuCell>();
                    verticalProjection[candidate, cell.Y].Add(cell);
                }
            }

            for (int candidate = 1; candidate <= 9; candidate++)
            {
                List<SudokuCell>[] admissableColumns = new List<SudokuCell>[Size];
                List<SudokuCell>[] admissableRows = new List<SudokuCell>[Size];

                // Check horizontal/vertical projection with no more than size elements
                for (int axis = 0; axis < Size; axis++)
                {
                    List<SudokuCell> horizontalCells = horizontalProjection[candidate, axis];
                    if (horizontalCells != null && horizontalCells.Count >= 2 && horizontalCells.Count <= size)
                    {
                        System.Diagnostics.Debug.WriteLine("candidate: {0} X: {1} => {2}", candidate, axis, horizontalCells.Select(c => String.Format("[{0}, {1}]", c.X, c.Y)).Aggregate((s, s1) => s + ";" + s1));
                        admissableColumns[axis] = horizontalCells;
                    }

                    List<SudokuCell> verticalCells = verticalProjection[candidate, axis];
                    if (verticalCells != null && verticalCells.Count >= 2 && verticalCells.Count <= size)
                    {
                        System.Diagnostics.Debug.WriteLine("candidate: {0} Y: {1} => {2}", candidate, axis, verticalCells.Select(c => String.Format("[{0}, {1}]", c.X, c.Y)).Aggregate((s, s1) => s + ";" + s1));
                        admissableRows[axis] = verticalCells;
                    }
                }

                // Horizontal
                IEnumerable<int[]> columnCombinations = Combinations(Enumerable.Range(0, Size).Where(y => admissableColumns[y] != null), size);
                foreach (int[] combination in columnCombinations)
                {
                    System.Diagnostics.Debug.WriteLine("testing column combination {0} candidate {1}", combination.Select(c => c.ToString(CultureInfo.InvariantCulture)).Aggregate((s, s1) => s + "," + s1), candidate);
                    // in combination, count(unique Y) must be equal to size
                    ISet<int> found = new HashSet<int>();
                    foreach (SudokuCell cell in combination.SelectMany(x => admissableColumns[x]))
                        found.Add(cell.Y);
                    //
                    if (found.Count == size)
                    {
                        System.Diagnostics.Debug.WriteLine("columns {0} candidate {1} is OK", combination.Select(c => c.ToString(CultureInfo.InvariantCulture)).Aggregate((s, s1) => s + "," + s1), candidate);
                        // TODO: remove candidate from every unique Y except in admissable columns
                    }
                }

                // Vertical
                IEnumerable<int[]> rowCombinations = Combinations(Enumerable.Range(0, Size).Where(y => admissableRows[y] != null), size);
                foreach (int[] combination in rowCombinations)
                {
                    System.Diagnostics.Debug.WriteLine("testing row combination {0} candidate {1}", combination.Select(c => c.ToString(CultureInfo.InvariantCulture)).Aggregate((s, s1) => s + "," + s1), candidate);
                    // in combination, count(unique x) must be equal to size
                    ISet<int> found = new HashSet<int>();
                    foreach (SudokuCell cell in combination.SelectMany(y => admissableRows[y]))
                        found.Add(cell.X);
                    //
                    if (found.Count == size)
                    {
                        System.Diagnostics.Debug.WriteLine("rows {0} candidate {1} is OK", combination.Select(c => c.ToString(CultureInfo.InvariantCulture)).Aggregate((s, s1) => s + "," + s1), candidate);
                        // TODO: remove candidate from every unique X except in admissable rows
                    }
                }
            }

            return progress;
        }

        #endregion

        private void ResetSolutions()
        {
            foreach (SudokuCell cell in _cells)
                cell.ResetCandidates();
        }

        private IEnumerable<SudokuCell> Cells
        {
            get { return SudokuFactory.Square(Size).Select(pos => _cells[pos.Item1, pos.Item2]); }
        }

        private IEnumerable<SudokuCell> Row(int rowId)
        {
            for (int i = 0; i < Size; i++)
                yield return _cells[i, rowId];
        }

        private IEnumerable<SudokuCell> Column(int columnId)
        {
            for (int i = 0; i < Size; i++)
                yield return _cells[columnId, i];
        }

        private IEnumerable<SudokuCell> Block(int blockId)
        {
            // 0 1 2
            // 3 4 5
            // 6 7 8
            return Block((blockId%BlockSize)*BlockSize, (blockId/BlockSize)*BlockSize);
        }

        private IEnumerable<SudokuCell> Block(int rowId, int columnId)
        {
            int lowerCornerX = (rowId/BlockSize)*BlockSize;
            int lowerCornerY = (columnId/BlockSize)*BlockSize;

            for (int y = lowerCornerY; y < lowerCornerY + BlockSize; y++)
                for (int x = lowerCornerX; x < lowerCornerX + BlockSize; x++)
                    yield return _cells[x, y];
        }

        private IEnumerable<IEnumerable<SudokuCell>> Rows
        {
            get
            {
                for (int i = 0; i < Size; i++)
                    yield return Row(i);
            }
        }

        private IEnumerable<IEnumerable<SudokuCell>> Columns
        {
            get
            {
                for (int i = 0; i < Size; i++)
                    yield return Column(i);
            }
        }

        private IEnumerable<IEnumerable<SudokuCell>> Blocks
        {
            get
            {
                for (int i = 0; i < Size; i++)
                    yield return Block(i);
            }
        }

        private void CreateCells()
        {
            foreach (Tuple<int, int> pos in SudokuFactory.Square(Size))
                _cells[pos.Item1, pos.Item2] = new SudokuCell(pos.Item1, pos.Item2, _maxValue);
        }

        private static int BlockId(SudokuCell cell)
        {
            return (cell.X/3) + ((cell.Y)/3)*3;
        }

        private static SudokuProgress CombineSolvedState(SudokuProgress a, SudokuProgress b)
        {
            if (a == SudokuProgress.Failed)
                return a;
            if (a == SudokuProgress.NoProgress)
                return b;
            if (a == SudokuProgress.Progress)
                return b == SudokuProgress.Failed ? b : a;
            throw new InvalidOperationException("Invalid value for a");
        }

        private static IEnumerable<T[]> Combinations<T>(IEnumerable<T> enumerable, int nrepeats)
        {
            List<T> items = enumerable.ToList();
            T[] ret = new T[nrepeats];
            int[] indices = new int[nrepeats];
            int current = 0;

            while (true)
            {
                if (indices[current] < items.Count)
                {
                    ret[current] = items[indices[current]++];
                    if (current == nrepeats - 1)
                        yield return ret.Clone() as T[];
                    else
                        indices[++current] = indices[current - 1];
                }
                else
                {
                    if (current == 0)
                        break;
                    current--;
                }
            }
        }
    }
}
