using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using TransactionManager = Autodesk.AutoCAD.ApplicationServices.TransactionManager;


namespace AutoCAD_CSharp_plug_in1
{
    public class Commands
    {
        private const string mXdataName = "ADSK_ATTRIBUTE_ZERO_OVERRULE";
        private static KeepStraightOverrule myOverrule;

        [CommandMethod("DontKeepStraight")]
        public static void RemoveXdata()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptEntityOptions opts = new PromptEntityOptions("\nSelect a block reference: ");
            opts.SetRejectMessage("\nMust be block reference...");
            opts.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult res = ed.GetEntity(opts);
            if (res.Status != PromptStatus.OK)
            {
                return;
            }

            Database db = doc.Database;
            AddRegAppId(db);
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = trans.GetObject(res.ObjectId, OpenMode.ForRead) as BlockReference;
                AttributeCollection attRefColl = blkRef.AttributeCollection;
                foreach (ObjectId objId in attRefColl)
                {
                    AttributeReference attRef = trans.GetObject(objId, OpenMode.ForWrite) as AttributeReference;
                    if(attRef != null)
                    {
                        using (ResultBuffer resBuf =
                               new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, mXdataName)))
                        {
                            attRef.XData = resBuf;
                        }
                    }
                }
                trans.Commit();
            }
        }

        [CommandMethod("keepstrainght")]
        public static void ImplementOverrule()
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptEntityOptions opts = new PromptEntityOptions("\nSelect a block reference:");
            opts.SetRejectMessage("\nMust be block reference...");
            opts.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult res = ed.GetEntity(opts);
            if (res.Status != PromptStatus.OK)
            {
                return;
            }

            ObjectId[] objIds;
            Database db = doc.Database;
            AddRegAppId(db);
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = (BlockReference)trans.GetObject(res.ObjectId, OpenMode.ForRead);
                AttributeCollection attRefColl = blkRef.AttributeCollection;
                objIds = new ObjectId[attRefColl.Count + 1];
                attRefColl.CopyTo(objIds, 0);
                Array.Resize(ref objIds, attRefColl.Count);

                foreach (ObjectId objId in attRefColl)
                {
                    AttributeReference attRef = trans.GetObject(objId, OpenMode.ForWrite) as AttributeReference;
                    if (attRef != null)
                    {
                        using (ResultBuffer resBuf =
                               new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, mXdataName)))
                        {
                            attRef.XData = resBuf;
                        }

                    }
                }
                trans.Commit();
            }

            //

             if (myOverrule == null)
             {
                 myOverrule = new KeepStraightOverrule();
                 Overrule.AddOverrule(
                     RXClass.GetClass(typeof(AttributeReference)),
                     myOverrule, false);
             }

            myOverrule.SetIdFilter(objIds);
            Overrule.Overruling = true;
        }

        private static void AddRegAppId(Database db)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                RegAppTable appTbl = trans.GetObject(db.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!appTbl.Has(mXdataName))
                {
                    RegAppTableRecord appTblRec = new RegAppTableRecord();
                    appTbl.UpgradeOpen();
                    appTblRec.Name = mXdataName;
                    appTbl.Add(appTblRec);
                    trans.AddNewlyCreatedDBObject(appTblRec, true);
                }
                trans.Commit();
            }
        }

        [CommandMethod("ActiveOverrule")]
        public static void ActivateOverrule()
        {
            if (myOverrule == null)
            {
                myOverrule = new KeepStraightOverrule();
                Overrule.AddOverrule(RXClass.GetClass(typeof(AttributeReference)),
                    myOverrule, false);
            }
            myOverrule.SetXDataFilter(mXdataName);
            Overrule.Overruling = true;
        }
    }

    public class KeepStraightOverrule : TransformOverrule
    {
        public override void TransformBy(Entity entity, Matrix3d transform)
        {
            base.TransformBy(entity, transform); 
            AttributeReference attRef = (AttributeReference)entity;
            attRef.Rotation = 0.0;
        }
    }
}
