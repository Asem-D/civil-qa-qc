// AutoCADStubs.cs — Minimal type stubs for compiling Rules without AutoCAD.
// Only compiled when NO_AUTOCAD is defined (CI builds without Civil 3D).
// These stubs satisfy the compiler but throw at runtime — rules require
// real Civil 3D to function.
#if NO_AUTOCAD
using System.Collections;
using System.Collections.Generic;

namespace Autodesk.AutoCAD.ApplicationServices
{
    internal static class Application
    {
        public static object GetSystemVariable(string name) =>
            throw new System.NotSupportedException("AutoCAD not available in this environment.");
    }

    internal class Document { }
}

namespace Autodesk.AutoCAD.DatabaseServices
{
    internal enum OpenMode { ForRead, ForWrite }

    internal struct ObjectId
    {
        public static readonly ObjectId Null = default;
    }

    internal class ResultBuffer : System.IDisposable
    {
        public void Dispose() { }
    }

    internal class TransactionManager
    {
        public Transaction StartTransaction() =>
            throw new System.NotSupportedException("AutoCAD not available.");
    }

    internal class Transaction : System.IDisposable
    {
        public DBObject GetObject(ObjectId id, OpenMode mode) =>
            throw new System.NotSupportedException("AutoCAD not available.");
        public void Commit() { }
        public void Dispose() { }
    }

    internal class DBObject
    {
        public virtual ResultBuffer? GetXDataForApplication(string regappName) => null;
    }

    internal class Database
    {
        public TransactionManager TransactionManager { get; } = new();
        public ObjectId BlockTableId { get; }
    }

    internal class SymbolTable : DBObject, IEnumerable<ObjectId>
    {
        public ObjectId this[ObjectId id] => ObjectId.Null;
        public IEnumerator<ObjectId> GetEnumerator() => Enumerable.Empty<ObjectId>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class BlockTable : SymbolTable { }

    internal class BlockTableRecord : DBObject, IEnumerable<ObjectId>
    {
        public const string ModelSpace = "*Model_Space";
        public bool IsFromExternalReference { get; }
        public bool IsLayout { get; }
        public bool IsUnloaded { get; }
        public string PathName { get; } = string.Empty;
        public new string Name { get; } = string.Empty;
        public IEnumerator<ObjectId> GetEnumerator() => Enumerable.Empty<ObjectId>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class Entity : DBObject
    {
        public string Layer { get; } = string.Empty;
        public Handle Handle { get; }
    }

    internal class Handle
    {
        public override string ToString() => string.Empty;
    }

    internal class ProxyEntity : Entity { }

    internal class TextStyleTable : SymbolTable { }

    internal class TextStyleTableRecord : DBObject
    {
        public new string Name { get; } = string.Empty;
        public string FileName { get; } = string.Empty;
    }
}
#endif
