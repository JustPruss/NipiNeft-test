using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
    public class MinesweeperException : Exception {}

    public enum CellStatus
    {
        Covered,
        Uncovered,
        Marked
    }

    public struct MineCell
    {
        public void New(CellStatus status, int val, bool bomb, bool marked)
        {
            status = status;
            isBomb = bomb;
            Value = val;
        }

        public CellStatus Status;
        public int Value;
        public bool isBomb;
    }

    public class MinesweeperMgr
    {
        private int mRows;
        private int mCols;
        private MineCell[,] mMineArray;
        private int mNumMines;
        private int mNumCellsUncovered;

        public int NumCellsUncovered
        {
            get
            {
                return mNumCellsUncovered;
            }
        }

        private int IncrementNumCellsUncovered()
        {
            mNumCellsUncovered = mNumCellsUncovered + 1;
            return mNumCellsUncovered;
        }

        public int MinefieldRows
        {
            get 
            {
                return mRows; 
            }

            set
            {
                if (value > 0)
                {
                    mRows = value;
                    if (NumMines > mRows * MinefieldColumns)
                    {
                        NumMines = mRows * MinefieldColumns;
                    }
                    else
                    {
                        throw new MinesweeperException();
                    }
                }
            }
        }

        public int MinefieldColumns
        {
            get
            {
                return mCols;
            }
            set
            {
                if (value > 0)
                {
                    mCols = value;
                    if (NumMines > MinefieldRows * mCols)
                    {
                        NumMines = MinefieldRows * mCols;
                    }
                    else
                    {
                        throw new MinesweeperException();
                    }
                }
            }
        }

        public int NumMines
        {
            get
            {
                return mNumMines;
            }
            set
            {
                if(mNumMines <= (mCols * mRows))
                {
                    mNumMines = value;
                }
                else
                {
                    mNumMines = mCols * mRows;
                }
            }
        }

        public MineCell[,] MineArray
        {
            get
            {
                return mMineArray;
            }
        }

        public MineCell GetCell(int row, int col)
        {
            if (row >= 0 && row < MinefieldRows && col >= 0 && col < MinefieldColumns)
            {
                return mMineArray[row, col];
            }
            else
            {
                throw new MinesweeperException();
            }
        }

        public bool SetCell(int row, int col, MineCell value)
        {
            if (row >= 0 && row < MinefieldRows && col >= 0 && col < MinefieldColumns)
            {
                mMineArray[row, col] = value;
                return true;
            }
            else
            {
                throw new MinesweeperException();
            }
        }

        public MineCell UncoverCell(int row, int col)
        {
            MineCell curCellVal = MineArray[row, col];
            if (curCellVal.Status != CellStatus.Uncovered)
            {
                MineArray[row, col].Status = CellStatus.Uncovered;
                IncrementNumCellsUncovered();
            }

            return curCellVal;
        }

        public MineCell MarkCell(int row, int col)
        {
            MineCell curCellVal = MineArray[row, col];
            if (curCellVal.Status == CellStatus.Covered)
            {
                MineArray[row, col].Status = CellStatus.Marked;
            }
            else if (curCellVal.Status == CellStatus.Marked)
            {
                MineArray[row, col].Status = CellStatus.Covered;
            }
            return curCellVal;
        }

        public bool CellIsUnCovered(int row, int col)
        {
            MineCell curCellVal = MineArray[row, col];
            if (MineArray[row, col].Status == CellStatus.Uncovered)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AllEmptyCellsUncovered()
        {
            return NumCellsUncovered == MinefieldColumns * MinefieldRows - NumMines;
        }

        public void InitMinefield()
        {
            InitMinefield(MinefieldRows, MinefieldColumns, NumMines);
        }

        private void ResetNumCellsUncovered()
        {
            mNumCellsUncovered = 0;
        }

        public void InitMinefield(int rows, int cols, int num)
        {
            if (rows < 1 || cols < 1 || num < 1)
            {
                throw new MinesweeperException();
            }

            MinefieldRows = rows;
            MinefieldColumns = cols;

            if (num > rows * cols)
            {
                NumMines = rows * cols;
            }
            else
            {
                NumMines = num;
            }

            ResetNumCellsUncovered();
            mMineArray = new MineCell[rows - 1, cols - 1];
            Random rand = new Random();
            int i = 0;

            do
            {
                int rndRow = rand.Next(MinefieldRows - 1);
                int rndCol = rand.Next(MinefieldColumns - 1);
                if (mMineArray[rndRow, rndCol].isBomb == false)
                {
                    mMineArray[rndRow, rndCol].Value = -1;
                    mMineArray[rndRow, rndCol].isBomb = true;
                    mMineArray[rndRow, rndCol].Status = CellStatus.Covered;

                    i = i + 1;
                }
            } while (i < num);

            for (int i = 0; i < MinefieldRows; i++)
            {
                for (int j = 0; j < MinefieldColumns; j++)
                {
                    if (mMineArray[i, j].isBomb == true)
                    {
                        continue;
                    }

                    int mineCounter = 0;
                    for (int k = -1; k < 1; k++)
                    {
                        for (int l = -1; l < 1; l++)
                        {
                            if (i + k < 0 || i + k > MinefieldRows - 1 || j + l < 0 || j + l > MinefieldColumns - 1)
                            {
                                continue;
                            }

                            if (k == 0 && l == 0)
                            {
                                continue;
                            }

                            if (mMineArray[i + k, j + l].isBomb == true)
                            {
                                mineCounter = mineCounter + 1;
                            }
                        }
                    }
                    mMineArray[i, j].Value = mineCounter;
                    mMineArray[i, j].Status = CellStatus.Covered;
                }
            }
        }
    }
}
