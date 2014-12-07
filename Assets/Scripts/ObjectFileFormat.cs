using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class ObjectFileFormat
{
    const string PrefixNormal = "N";
    const string PrefixColor = "C";
    const string PrefixTextureCoordinate = "ST";
    const string PrefixNDimension = "n";
    const string PrefixHomogeneousCoordinate = "4";

    public static Mesh OffToMesh(TextReader off)
    {
        var mesh = new Mesh();
        var tokens = getTokensOfNonEmptyLines(off);
        var parser = parseOff(mesh);
        while (parser.MoveNext())
        {
            if (tokens.MoveNext())
                parser.Current(tokens.Current);
            else
                throw new Exception("Parse error.");
        }
        off.Close();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static void MeshToOff(Mesh mesh, TextWriter off)
    {
        if (mesh.uv.Length != 0)
            off.Write(PrefixTextureCoordinate);
        if (mesh.colors.Length != 0)
            off.Write(PrefixColor);
        if (mesh.normals.Length != 0)
            off.Write(PrefixNormal);
        off.WriteLine("OFF");

        var verts = mesh.vertices;
        var norms = mesh.normals;
        var colors = mesh.colors;
        var uvs = mesh.uv;
        uvs = new Vector2[0];
        colors = new Color[0];
        var tris = mesh.triangles;
        var faceCount = tris.Length / 3;

        off.WriteLine(string.Format("{0} {1} {2}", verts.Length, faceCount, 0));

        for (int i = 0; i < verts.Length; i++)
        {
            off.Write(verts[i].x);
            off.Write(" ");
            off.Write(verts[i].y);
            off.Write(" ");
            off.Write(verts[i].z);
            if (norms.Length != 0)
            {
                off.Write(" ");
                off.Write(norms[i].x);
                off.Write(" ");
                off.Write(norms[i].y);
                off.Write(" ");
                off.Write(norms[i].z);
            }
            if (colors.Length != 0)
            {
                off.Write(" ");
                off.Write(colors[i].r);
                off.Write(" ");
                off.Write(colors[i].g);
                off.Write(" ");
                off.Write(colors[i].b);
                off.Write(" ");
                off.Write(colors[i].a);
            }
            if (uvs.Length != 0)
            {
                off.Write(" ");
                off.Write(uvs[i].x);
                off.Write(" ");
                off.Write(uvs[i].y);
            }
            off.WriteLine();
        }

        for(int i = 0; i < faceCount; i++)
        {
            off.Write("3 ");
            off.Write(tris[i * 3]);
            off.Write(" ");
            off.Write(tris[i * 3 + 1]);
            off.Write(" ");
            off.WriteLine(tris[i * 3 + 2]);
        }
        off.Close();
    }

    static IEnumerator<string[]> getTokensOfNonEmptyLines(TextReader off)
    {
        var re = new Regex(@"\s+");
        while (off.Peek() > 0)
        {
            var line = off.ReadLine();
            var sharp = line.IndexOf("#");
            if (sharp >= 0)
            {
                line = line.Substring(0, sharp);
            }
            line = line.Trim(" \t\n\r".ToCharArray());
            yield return re.Split(line);
        }
    }

    static IEnumerator<Action<string[]>> parseOff(Mesh mesh)
    {
        var hasNormal = false;
        var hasColor = false;
        var hasUv = false;
        var hasHomo = false;
        var hasDim = false;
        var dim = 3;

        var vertexCount = 0;
        var faceCount = 0;

        // Parse Header
        yield return tokens =>
        {
            if (tokens.Length != 1)
                throw new Exception("Invalid OFF header: ");
            var re = new Regex("(?<ST>ST)?(?<C>C)?(?<N>N)?(?<4>4)?(?<n>n)?OFF");
            var match = re.Match(tokens[0]);
            if (!match.Success)
                throw new Exception("Invalid OFF header.");

            hasNormal = match.Groups[PrefixNormal].Value == PrefixNormal;
            hasColor = match.Groups[PrefixColor].Value == PrefixColor;
            hasUv = match.Groups[PrefixTextureCoordinate].Value == PrefixTextureCoordinate;
            hasHomo = match.Groups[PrefixHomogeneousCoordinate].Value == PrefixHomogeneousCoordinate;
            hasDim = match.Groups[PrefixNDimension].Value == PrefixNDimension;
        };

        // Dimension
        if (hasDim)
        {
            yield return tokens =>
            {
                if (tokens.Length != 1
                    || !int.TryParse(tokens[0], out dim)
                    || dim > 3)
                {
                    throw new Exception("Dimension should not be more than 3.");
                }
            };
        }

        // Counts
        yield return tokens =>
        {
            if (!int.TryParse(tokens[0], out vertexCount)
                || !int.TryParse(tokens[1], out faceCount))
                throw new Exception("Invalid vertex or face count.");
        };

        // Vertex
        var verts = new Vector3[vertexCount];
        var normals = hasNormal ? new Vector3[vertexCount] : null;
        var colors = hasColor ? new Color[vertexCount] : null;
        var uvs = hasUv ? new Vector2[vertexCount] : null;
        var normOff = hasHomo ? dim + 1 : dim;
        var colOff = hasNormal ? normOff + dim : normOff;
        var uvOff = hasColor ? colOff + 4 : colOff;
        int i = 0;
        Action<string[]> parseVert = tokens =>
        {
            var w = 1f;
            if ((dim > 0 && !float.TryParse(tokens[0], out verts[i].x)) ||
                (dim > 1 && !float.TryParse(tokens[1], out verts[i].y)) ||
                (dim > 2 && !float.TryParse(tokens[2], out verts[i].z)) ||
                (hasHomo && !float.TryParse(tokens[dim], out w)) ||
                (hasNormal && !(
                    float.TryParse(tokens[normOff + 0], out normals[i].x) &&
                    float.TryParse(tokens[normOff + 1], out normals[i].y) &&
                    float.TryParse(tokens[normOff + 2], out normals[i].z))) ||
                (hasColor && !(
                    float.TryParse(tokens[colOff + 0], out colors[i].r) &&
                    float.TryParse(tokens[colOff + 1], out colors[i].g) &&
                    float.TryParse(tokens[colOff + 2], out colors[i].b) &&
                    float.TryParse(tokens[colOff + 3], out colors[i].a))) ||
                (hasUv && !(
                    float.TryParse(tokens[uvOff + 0], out uvs[i].x) &&
                    float.TryParse(tokens[uvOff + 1], out uvs[i].y))))
            {
                throw new Exception("Vertex Parse error: " + i + ".");
            }
            if (hasHomo)
                verts[i] /= w;
        };
        for (i = 0; i < vertexCount; i++)
        {
            yield return parseVert;
        }

        // Indexes
        var tris = new int[faceCount * 3];
        Action<string[]> parseFace = tokens =>
        {
            if (tokens[0] != "3" ||
                !int.TryParse(tokens[1], out tris[i * 3 + 0]) ||
                !int.TryParse(tokens[2], out tris[i * 3 + 1]) ||
                !int.TryParse(tokens[3], out tris[i * 3 + 2]))
            {
                throw new Exception("Face Parse error.");
            }
        };
        for (i = 0; i < faceCount; i++)
        {
            yield return parseFace;
        }

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = tris;
    }
}
