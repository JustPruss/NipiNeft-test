using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;
using System.Windows.Documents.DocumentStructures;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Minesweeper.MyCommands))]

namespace Minesweeper
{
    public class MyCommands
    {
        [CommandMethod("SGP_Minesweeper", "MINESWEEPER", "MINESWEEPER", CommandFlags.Session)]
        public static void Minesweeper()
        {
            Document doc = Application.DocumentManager.Add("");
            Application.DocumentManager.MdiActiveDocument = doc;
            doc.SendStringToExecute("RUNMINESWEEPER ", true, false, false);
        }

        [CommandMethod("SGP_Minesweeper", "RUNMINESWEEPER", CommandFlags.Modal)]
        public static void RunMimesweeper()
        {
            try
            {
                DefineTextStyle();
                AcadMinesweeper cls = new AcadMinesweeper();
                cls.DoIt();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    "\nSorry - An error occurred. Game aborted." + "\n");
            }
        }

        private static void DefineTextStyle()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = db.TransactionManager;
            Transaction myT = tm.StartTransaction();
            try
            {
                TextStyleTable st = tm.GetObject(db.TextStyleTableId, OpenMode.ForWrite, false) as TextStyleTable;
                if (!st.Has("MinesweeperStyle"))
                {
                    TextStyleTableRecord str = new TextStyleTableRecord();
                    str.Name = "MinesweeperStyle";
                    st.Add(str);
                    str.FileName = "txt.shx";
                    str.TextSize = 1.0;
                    str.IsShapeFile = true;
                    tm.AddNewlyCreatedDBObject(str, true);
                }

                myT.Commit();
            }
            finally
            {
                myT.Dispose();
            }
        }
    }

    public class AcadMinesweeper
    {
        private struct MineElement
        {
            public ObjectId Id;
            public int Row;
            public int Col;
        }

        private MinesweeperMgr mMineMgr = new MinesweeperMgr();

        private MineElement[] mMinefield;

        public void DoIt()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            if (!PromptSetup())
            {
                ed.WriteMessage("\nYou cancelled setup - aborting command\n");
            }

            mMineMgr.InitMinefield();
            if (!SetupGrid())
            {
                ed.WriteMessage("\nThere was a problem setting up the minefield" + " - aborting command\n");
            }

            DateTime startTime = DateTime.Now;
            while (PromptMineAction())
            {

            }

            TimeSpan timeInterval = DateTime.Now - startTime;
            ed.WriteMessage("\nTime taken = " + timeInterval.TotalSeconds + " seconds\n");
        }

        private bool PromptMineAction()
        {
            static int bMarking;
            string strMsg = "";
            string strKeyword = "";
            switch (bMarking)
            {
                case 0:
                    strMsg = "Select a cell to uncover:";
                    strKeyword = "Mark";
                    break;

                case 1:
                    strMsg = "Select a cell to mark/unmark:";
                    strKeyword = "Uncover";
                    break;
            }

            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptEntityOptions opts = new PromptEntityOptions(Environment.NewLine + strMsg);
            opts.Keywords.Add(strKeyword);
            opts.AppendKeywordsToMessage = true;
            opts.AllowNone = true;
            PromptEntityResult res = ed.GetEntity(opts);

            if (res.Status == PromptStatus.Cancel)
            {
                ed.WriteMessage("\nYou cancelled the game. Byeee!\n");
                return false;
            }

            if (res.Status == PromptStatus.None)
            {
                return true;
            }

            if (res.Status == PromptStatus.Keyword)
            {
                switch (res.StringResult)
                {
                    case "Mark":
                        bMarking = 1;
                        break;
                    default:
                        bMarking = 0;
                        break;
                }

                return true;
            }
            else if (res.Status == PromptStatus.OK)
            {
  
                MineElement elem = FindInMinefield(res.ObjectId);
                if (elem.Id == ObjectId.Null)
                {
                    ed.WriteMessage("\nYou didn't select a cell in the minefield.\n");
                    return true;
                }

                if (mMineMgr.CellIsUnCovered(elem.Row, elem.Col))
                {
                    ed.WriteMessage("\nThis cell is already uncovered. " + "Pick another.\n");
                    return true;
                }

                if (bMarking)
                {
                    MineCell oldCellVal = mMineMgr.MarkCell(elem.Row, elem.Col);
                    if (oldCellVal.Status == CellStatus.Covered)
                    {
                        if (oldCellVal.Status == CellStatus.Marked)
                        {
                            SetText(elem.Id, "X");
                        }
                        else
                        {
                            SetText(elem.Id, "M");
                        }

                        return true;
                    }
                    else
                    {
                        MineCell oldCellVal = mMineMgr.UncoverCell(elem.Row, elem.Col);
                        if (oldCellVal.isBomb)
                        {
                            SetText(elem.Id, "*");
                            ed.WriteMessage("\nYou hit a mine. Game Over!.\n");
                            return false;
                        }
                        else
                        {
                            SetText(elem.Id, oldCellVal.Value.ToString());
                            if (mMineMgr.AllEmptyCellsUncovered)
                            {
                                ed.WriteMessage("\nCongratulations. " + "You cleared all the mines.\n");
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        private void SetText(ObjectId objId, string strText)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MText txt = tr.GetObject(objId, OpenMode.ForWrite) as MText;
                txt.Contents = strText;
                tr.Commit();
            }
        }

        private string GetText(ObjectId objId)
        {
            string strText;
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MText txt = tr.GetObject(objId, OpenMode.ForWrite) as MText;
                strText = txt.Contents;
                tr.Commit();
            }

            return strText;
        }

        private MineElement EleFindInMinefield(ObjectId objId)
        {
            foreach (MineElement elem in mMinefield)
            {
                if (elem.Id == objId)
                {
                    return elem;
                }
            }

            return new MineElement();
        }

        private MineElement FindInMinefield(ObjectId objId)
        {
            foreach (MineElement elem in mMinefield)
            {
                if (elem.Id == objId)
                {
                    return elem;
                }
            }

            return new MineElement();
        }

        private bool SetupGrid()
        {
            bool bFlag = false;
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                    ObjectId textStyleId = tst["MinesweeperStyle"];
                    BlockTableRecord btr =
                        (tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite) as
                            BlockTableRecord) as BlockTableRecord;

                    int rows = mMineMgr.MinefieldRows;
                    int cols = mMineMgr.MinefieldColumns;
                    mMinefield = new MineElement[rows * cols];

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            using (MText txt = new MText())
                            {
                                txt.SetDatabaseDefaults();
                                txt.TextStyleId = textStyleId;
                                txt.Location = new Point3d(i, j, 0);
                                txt.Width = 1.0;
                                txt.Height = 1.0;
                                txt.TextHeight = 0.8;
                                txt.Attachment = AttachmentPoint.MiddleCenter;

                                int index = i * cols + j;
                                mMinefield[index].Id = btr.AppendEntity(txt);
                                mMinefield[index].Row = i;
                                mMinefield[index].Col = j;

                                txt.Contents = "X";
                                tr.AddNewlyCreatedDBObject(txt, true);
                            }
                        }
                    }

                    tr.Commit();
                    ModelZoomExtents();
                }

                bFlag = true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                bFlag = false;
            }

            return bFlag;
        }

        private bool PromptSetup()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptIntegerOptions opts1 = new PromptIntegerOptions("Enter Minefield width:");
            opts1.LowerLimit = 1;
            opts1.UpperLimit = 100;
            opts1.DefaultValue = 10;

            PromptIntegerResult res1 = ed.GetInteger(opts1);
            if (res1.Status != PromptStatus.OK)
            {
                return false;
            }

            mMineMgr.MinefieldRows = res1.Value;
            opts1.Message = "Enter minefield height:";
            res1 = ed.GetInteger(opts1);
            if (res1.Status != PromptStatus.OK)
            {
                return false;
            }

            mMineMgr.MinefieldColumns = res1.Value;
            opts1.UpperLimit = mMineMgr.MinefieldRows * mMineMgr.MinefieldColumns;
            opts1.DefaultValue = mMineMgr.MinefieldRows * mMineMgr.MinefieldColumns / 6;
            res1 = ed.GetInteger(opts1);
            if (res1.Status != PromptStatus.OK)
            {
                return true;
            }

            mMineMgr.NumMines = res1.Value;
            return true;
        }

        public void SetViewportToExtents(Database db, ViewportTableRecord vtr)
        {
            db.UpdateExt(true);
            double scrRatio = (vtr.Width / vtr.Height);
            Matrix3d matWCS2DCS = Matrix3d.PlaneToWorld(vtr.ViewDirection);
            matWCS2DCS = Matrix3d.Displacement(vtr.Target - Point3d.Origin) * matWCS2DCS;
            matWCS2DCS =
                Matrix3d.Rotation(-vtr.ViewTwist, vtr.ViewDirection, vtr.Target) * matWCS2DCS;
            matWCS2DCS = matWCS2DCS.Inverse();

            Extents3d extents = new Extents3d(db.Extmin, db.Extmax);
            extents.TransformBy(matWCS2DCS);

            double width = (extents.MaxPoint.X - extents.MinPoint.X);
            double height = (extents.MaxPoint.Y - extents.MinPoint.Y);
            Point2d center = new Point2d((extents.MaxPoint.X + extents.MinPoint.X) * 0.5,
                (extents.MaxPoint.Y + extents.MinPoint.Y) * 0.5);

            if (width > (height * scrRatio))
            {
                height = width / scrRatio;
            }

            vtr.Height = height;
            vtr.Width = height * scrRatio;
            vtr.CenterPoint = center;
            vtr.IconEnabled = false;
        }

        public void ModelZoomExtents()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                ed.UpdateTiledViewportsInDatabase();
                ViewportTableRecord viewportTableRec =
                    (Tx.GetObject(ed.ActiveViewportId, OpenMode.ForWrite)) as ViewportTableRecord;
                SetViewportToExtents(db, viewportTableRec);
                ed.UpdateTiledViewportsFromDatabase();
                Tx.Commit();
            }

        }
    }
}


