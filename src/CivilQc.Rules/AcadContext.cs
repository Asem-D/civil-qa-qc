// ──────────────────────────────────────────────────────────────────────────────
// AcadContext.cs — Typed helper for accessing AutoCAD objects from DrawingContext.
//
// DrawingContext stores AutoCAD handles as `object?` to keep the Engine project
// free of AutoCAD references. Rules that need the API cast through this helper.
//
// All methods throw InvalidOperationException if the context is not populated
// (e.g., running outside accoreconsole).
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

/// <summary>
/// Provides typed access to the AutoCAD objects wrapped by <see cref="DrawingContext"/>.
/// </summary>
internal static class AcadContext
{
    /// <summary>
    /// Returns the active drawing's <see cref="Database"/>.
    /// </summary>
    public static Database GetDatabase(DrawingContext ctx)
    {
        if (ctx.Database is not Database db)
            throw new InvalidOperationException(
                "DrawingContext.Database is not populated. " +
                "Rules must run inside accoreconsole with a loaded drawing.");
        return db;
    }

    /// <summary>
    /// Returns the active drawing's <see cref="Document"/>.
    /// </summary>
    public static Document GetDocument(DrawingContext ctx)
    {
        if (ctx.Document is not Document doc)
            throw new InvalidOperationException(
                "DrawingContext.Document is not populated.");
        return doc;
    }

    /// <summary>
    /// Returns the active drawing's Civil 3D <c>DatabaseServices.Document</c>.
    /// Casts the raw object; throws if not available.
    /// </summary>
    public static Autodesk.AutoCAD.ApplicationServices.Document GetCivilDocument(DrawingContext ctx)
    {
        // CivilDocument is the same Document object in most cases.
        // This exists for future Civil 3D-specific rules that may need
        // AeccDocument wrappers.
        return GetDocument(ctx);
    }
}
