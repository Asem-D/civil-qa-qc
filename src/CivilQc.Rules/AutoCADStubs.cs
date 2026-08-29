// AutoCADStubs.cs — Minimal type stubs for compiling without AutoCAD.
// Only compiled when NO_AUTOCAD is defined (CI builds without Civil 3D).
// These stubs satisfy the compiler but throw at runtime — rules require
// real Civil 3D to function.
#if NO_AUTOCAD
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Autodesk.AutoCAD.Runtime
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class CommandMethodAttribute : System.Attribute
    {
        public CommandMethodAttribute(string name) { }
    }
}

namespace Autodesk.AutoCAD.DatabaseServices
{
    public enum OpenMode { ForRead, ForWrite }

    public struct ObjectId
    {
        public static readonly ObjectId Null = default;
    }

    public class ResultBuffer : System.IDisposable
    {
        public void Dispose() { }
    }

    public class TransactionManager
    {
        public Transaction StartTransaction() =>
            throw new System.NotSupportedException("AutoCAD not available.");
    }

    public class Transaction : System.IDisposable
    {
        public DBObject GetObject(ObjectId id, OpenMode mode) =>
            throw new System.NotSupportedException("AutoCAD not available.");
        public void Commit() { }
        public void Dispose() { }
    }

    public class DBObject
    {
        public Handle Handle { get; } = new Handle();
        public virtual ResultBuffer? GetXDataForApplication(string regappName) => null;
    }

    public class Handle
    {
        public override string ToString() => string.Empty;
    }

    public class Database
    {
        public TransactionManager TransactionManager { get; } = new TransactionManager();
        public ObjectId BlockTableId { get; }
        public ObjectId LayerTableId { get; }
        public ObjectId TextStyleTableId { get; }
    }

    public class SymbolTable : DBObject, IEnumerable<ObjectId>
    {
        public ObjectId this[string name] => ObjectId.Null;
        public IEnumerator<ObjectId> GetEnumerator() => Enumerable.Empty<ObjectId>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class BlockTable : SymbolTable { }

    public class BlockTableRecord : DBObject, IEnumerable<ObjectId>
    {
        public const string ModelSpace = "*Model_Space";
        public string Name { get; set; } = string.Empty;
        public bool IsFromExternalReference { get; set; }
        public bool IsLayout { get; set; }
        public bool IsUnloaded { get; set; }
        public bool IsDynamicBlock { get; set; }
        public string PathName { get; set; } = string.Empty;
        public IEnumerator<ObjectId> GetEnumerator() => Enumerable.Empty<ObjectId>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Entity : DBObject
    {
        public string Layer { get; set; } = string.Empty;
    }

    public class ProxyEntity : Entity { }

    public class Point3d
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class BlockReference : Entity
    {
        public Point3d Position { get; set; } = new Point3d();
        public ObjectId BlockTableRecord { get; set; }
    }

    public class LayerTable : SymbolTable { }

    public class Color
    {
        public short ColorIndex { get; set; } = 7;
    }

    public class LayerTableRecord : DBObject
    {
        public string Name { get; set; } = string.Empty;
        public bool IsFrozen { get; set; }
        public bool IsOff { get; set; }
        public bool IsLocked { get; set; }
        public bool IsPlottable { get; set; }
        public Color Color { get; set; } = new Color();
        public ObjectId LinetypeObjectId { get; set; }
    }

    public class LinetypeTable : SymbolTable { }

    public class LinetypeTableRecord : DBObject
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TextStyleTable : SymbolTable { }

    public class TextStyleTableRecord : DBObject
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double TextSize { get; set; }
    }

    public class DynamicBlockReferenceProperty
    {
        public string PropertyName { get; set; } = string.Empty;
        public object Value { get; set; } = new object();
    }
}

namespace Autodesk.AutoCAD.ApplicationServices
{
    public static class Application
    {
        public static DocumentManager DocumentManager { get; } = new DocumentManager();
        public static object GetSystemVariable(string name) =>
            throw new System.NotSupportedException("AutoCAD not available in this environment.");
    }

    public class DocumentManager
    {
        public Document? MdiActiveDocument { get; }
    }

    public class Document
    {
        public DatabaseServices.Database Database { get; } = new DatabaseServices.Database();
    }
}
#endif
