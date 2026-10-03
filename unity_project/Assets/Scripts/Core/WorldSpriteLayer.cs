using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>One batched, clipped map layer made from cells in a transparent sprite atlas.</summary>
    public sealed class WorldSpriteLayer : MaskableGraphic
    {
        public struct SpriteQuad
        {
            public Vector2 Center;
            public Vector2 Size;
            public int Cell;
            public Color Tint;
            public float Rotation;

            public SpriteQuad(Vector2 center, Vector2 size, int cell, Color tint, float rotation = 0f)
            {
                Center = center;
                Size = size;
                Cell = cell;
                Tint = tint;
                Rotation = rotation;
            }
        }

        private Texture2D atlas;
        private int columns = 1;
        private int rows = 1;
        private readonly List<SpriteQuad> sprites = new List<SpriteQuad>();

        public override Texture mainTexture => atlas != null ? atlas : s_WhiteTexture;

        public void Configure(Texture2D texture, int atlasColumns, int atlasRows, IEnumerable<SpriteQuad> quads)
        {
            atlas = texture;
            columns = Mathf.Max(1, atlasColumns);
            rows = Mathf.Max(1, atlasRows);
            sprites.Clear();
            if (quads != null) sprites.AddRange(quads);
            raycastTarget = false;
            color = Color.white;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (atlas == null || atlas.width < columns || atlas.height < rows) return;

            var padU = .75f / atlas.width;
            var padV = .75f / atlas.height;
            foreach (var sprite in sprites)
            {
                var cell = Mathf.Clamp(sprite.Cell, 0, columns * rows - 1);
                var column = cell % columns;
                var rowFromTop = cell / columns;
                var u0 = column / (float)columns + padU;
                var u1 = (column + 1f) / columns - padU;
                var v1 = 1f - rowFromTop / (float)rows - padV;
                var v0 = 1f - (rowFromTop + 1f) / rows + padV;

                var half = sprite.Size * .5f;
                var radians = sprite.Rotation * Mathf.Deg2Rad;
                var sin = Mathf.Sin(radians);
                var cos = Mathf.Cos(radians);
                Vector2 Rotate(Vector2 p) => new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);

                AddVertex(vh, sprite.Center + Rotate(new Vector2(-half.x, -half.y)), new Vector2(u0, v0), sprite.Tint);
                AddVertex(vh, sprite.Center + Rotate(new Vector2(-half.x, half.y)), new Vector2(u0, v1), sprite.Tint);
                AddVertex(vh, sprite.Center + Rotate(new Vector2(half.x, half.y)), new Vector2(u1, v1), sprite.Tint);
                AddVertex(vh, sprite.Center + Rotate(new Vector2(half.x, -half.y)), new Vector2(u1, v0), sprite.Tint);
                var first = vh.currentVertCount - 4;
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first + 2, first + 3, first);
            }
        }

        private static void AddVertex(VertexHelper vh, Vector2 position, Vector2 uv, Color tint)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.uv0 = uv;
            vertex.color = tint;
            vh.AddVert(vertex);
        }
    }
}
