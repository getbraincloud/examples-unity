using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Canvas coverage % + win-ranking (BCLOUD-14472/14490) — the ALGORITHM is ported verbatim from
/// the Godot C# RelayTestApp's Coverage.cs (itself a pure port of cpp's coverage.cpp / react's
/// coverage.js). The CONSTANTS below are Unity-specific on purpose (see CanvasW/CanvasH/
/// SplotchRadius) — coverage is never compared numerically across hosts, so each port derives
/// its canvas/radius from its own real board geometry rather than copying another port's numbers.
///
/// Algorithm: rasterize a GridCellSize-resolution ownership grid over the canvas by stamping
/// every splotch, in paint order, as a filled circle of radius SplotchRadius centered on it —
/// so a later splotch always overwrites an earlier one wherever they overlap. This is exactly
/// what ends up rendered on screen (paint order = draw order = "last one wins").
///
/// CoveragePct is each player's owned-cell-count * cell-area / canvas-area (clamped to 100) —
/// ABSOLUTE canvas-area coverage, NOT a share of painted area, so a solo match doesn't
/// trivially score 100%.
///
/// Attribution: attribute by the sender's cxId first, falling back to a colorIndex match
/// against a live member only when unattributed (a splotch synced in before its owning netId
/// could be resolved, or a legacy sender). Two players sharing the same colour would otherwise
/// have their splotches merged into whichever member matches that colour first. A splotch whose
/// colour matches no current member still overwrites the grid (correctly obscuring whatever was
/// under it) but credits no one.
/// </summary>
public static class Coverage
{
    // GameArea's sizeDelta is 600x800, but it's rotated 270° — on-screen footprint is 800x600.
    // Splat sprite is 64x64 (Generic-Paint-Image.prefab), matching the cross-client standard.
    public const float CanvasW = 800f;
    public const float CanvasH = 600f;
    public const float SplotchRadius = 32f; // half the 64px splat sprite
    public const float GridCellSize = 2f;

    private static readonly int GridW = (int)(CanvasW / GridCellSize);
    private static readonly int GridH = (int)(CanvasH / GridCellSize);
    private static readonly int CellRadius = Mathf.CeilToInt(SplotchRadius / GridCellSize);
    private static readonly float R2 = SplotchRadius * SplotchRadius;

    // Reused across calls (this runs once per match, host-only, right when the timer elapses).
    private static readonly int[] OwnerGrid = new int[GridW * GridH];

    public class Entry
    {
        public string CxId;
        public int ColourIndex;
        public int VisibleCount;
        public float CoveragePct;
        public int Rank = 1;
        public int Beaten;
    }

    // x/y are normalized 0-1 (matches StateManager.SplotchRecord.Position — the wire/local
    // splotch format every CursorParty port shares).
    public readonly struct SplotchLike
    {
        public readonly float X, Y;
        public readonly int ColorIndex;
        public readonly string CxId;
        public SplotchLike(float x, float y, int colorIndex, string cxId = null) { X = x; Y = y; ColorIndex = colorIndex; CxId = cxId; }
    }

    public readonly struct MemberLike
    {
        public readonly string CxId;
        public readonly int ColourIndex;
        public MemberLike(string cxId, int colourIndex) { CxId = cxId; ColourIndex = colourIndex; }
    }

    public static List<Entry> Compute(IReadOnlyList<SplotchLike> splotches, IReadOnlyList<MemberLike> members)
    {
        var result = new List<Entry>(members.Count);
        var indexByCxId = new Dictionary<string, int>();
        foreach (var m in members)
        {
            indexByCxId[m.CxId] = result.Count;
            result.Add(new Entry { CxId = m.CxId, ColourIndex = m.ColourIndex });
        }

        int n = splotches.Count;
        if (n > 0)
        {
            for (int i = 0; i < OwnerGrid.Length; i++) OwnerGrid[i] = -1;

            for (int i = 0; i < n; i++)
            {
                var s = splotches[i];
                float px = s.X * CanvasW;
                float py = s.Y * CanvasH;

                int idx = -1;
                if (!string.IsNullOrEmpty(s.CxId) && indexByCxId.TryGetValue(s.CxId, out int foundIdx))
                {
                    idx = foundIdx;
                }
                else
                {
                    for (int k = 0; k < result.Count; k++)
                    {
                        if (result[k].ColourIndex == s.ColorIndex) { idx = k; break; }
                    }
                }

                int cx = (int)(px / GridCellSize);
                int cy = (int)(py / GridCellSize);
                for (int dy = -CellRadius; dy <= CellRadius; dy++)
                {
                    int gy = cy + dy;
                    if (gy < 0 || gy >= GridH) continue;
                    float cellPy = (gy + 0.5f) * GridCellSize;
                    float ddy = cellPy - py;
                    int rowBase = gy * GridW;
                    for (int dx = -CellRadius; dx <= CellRadius; dx++)
                    {
                        int gx = cx + dx;
                        if (gx < 0 || gx >= GridW) continue;
                        float cellPx = (gx + 0.5f) * GridCellSize;
                        float ddx = cellPx - px;
                        if (ddx * ddx + ddy * ddy <= R2)
                            OwnerGrid[rowBase + gx] = idx; // may be -1 — still overwrites, credits no one
                    }
                }
            }

            for (int c = 0; c < OwnerGrid.Length; c++)
            {
                int idx = OwnerGrid[c];
                if (idx >= 0) result[idx].VisibleCount++;
            }
        }

        float cellArea = GridCellSize * GridCellSize;
        float canvasArea = CanvasW * CanvasH;
        foreach (var e in result)
            e.CoveragePct = Mathf.Min(100f, e.VisibleCount * cellArea / canvasArea * 100f);

        result.Sort((a, b) =>
        {
            if (a.CoveragePct != b.CoveragePct) return b.CoveragePct.CompareTo(a.CoveragePct);
            if (a.VisibleCount != b.VisibleCount) return b.VisibleCount.CompareTo(a.VisibleCount);
            return string.CompareOrdinal(a.CxId, b.CxId);
        });

        for (int i = 0; i < result.Count; i++)
        {
            result[i].Rank = (i > 0 &&
                result[i].CoveragePct == result[i - 1].CoveragePct &&
                result[i].VisibleCount == result[i - 1].VisibleCount)
                ? result[i - 1].Rank // tie shares rank
                : i + 1;
        }
        foreach (var e in result)
            e.Beaten = result.Count(other => other.Rank > e.Rank);

        return result;
    }
}
